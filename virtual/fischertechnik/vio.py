"""Enlace `vio` de un TXT con la maqueta virtual, por MQTT.

Implementa el protocolo de D006 (02-DOCS/wiki/decisiones/D006 Protocolo vio.md):

- D006.1  `vio/txtN/in` trae la imagen completa de entradas. `vio/txtN/out`
          lleva la imagen completa de salidas.
- D006.3  Si la última imagen de entradas tiene más de 0,5 s, toda lectura
          espera a una nueva. Nunca se trabaja con datos viejos.
- D006.4  Los valores viajan tal cual los da la API real.
- D006.5  `out` se envía cuando cambia alguna salida y además cada 100 ms.
- D006.7  Mientras las entradas están caducadas, `out` deja de enviarse,
          latido incluido. El latido significa "lazo de control sano".

Las lecturas se responden desde memoria (la última imagen recibida). Las
salidas las guardan los propios bornes; el enlace solo las empaqueta y las
publica.
"""

import json
import logging
import math
import threading
import time
import uuid

import paho.mqtt.client as mqtt
from paho.mqtt.enums import CallbackAPIVersion

# Reglas de D006. No son configurables a propósito: D006.6 dice que la regla
# de 0,5 s no se desactiva ni en pruebas.
CADUCIDAD_S = 0.5   # D006.3: edad máxima de la imagen de entradas
LATIDO_S = 0.1      # D006.5: periodo del latido de `out`
QOS = 0             # Sin confirmación: a 100 Hz el siguiente mensaje ya corrige
RETAIN = False      # Un `in` retenido sería un dato viejo

_INTERVALO_AVISO_DESCARTE_S = 2.0   # Como mucho un aviso de descarte cada 2 s
_PASO_VIGILANCIA_S = 0.01           # Cada cuánto mira el hilo vigilante

_config = {"host": "localhost", "puerto": 1883}

log = logging.getLogger("vio")


def configurar(host="localhost", puerto=1883):
  """Fija el broker MQTT. Hay que llamarla antes de importar `lib.controller`."""
  _config["host"] = host
  _config["puerto"] = int(puerto)


class EnlaceVio:
  """Conecta los bornes de un TXT con los topics `vio/txtN/in` y `vio/txtN/out`."""

  def __init__(self, numero_txt, host=None, puerto=None):
    self.numero_txt = numero_txt
    self.host = host if host is not None else _config["host"]
    self.puerto = puerto if puerto is not None else _config["puerto"]
    self.topic_in = "vio/txt{}/in".format(numero_txt)
    self.topic_out = "vio/txt{}/out".format(numero_txt)

    # Entradas: clave ("I1", "C1"...) -> tipo esperado (bool o int)
    self._tipos_entrada = {}
    # Salidas: clave ("M1", "O5"...) -> borne con método valor_vio()
    self._salidas = {}

    # Imagen de entradas y su hora de llegada (reloj monotónico del PC), en una
    # sola tupla (imagen, hora). Se sustituye entera, nunca se modifica: así una
    # lectura sin cerrojo nunca ve una imagen nueva con una hora vieja.
    self._cond = threading.Condition()
    self._foto = None

    # Publicación de salidas
    self._lock_out = threading.Lock()
    self._ultima_out = None
    self._salidas_bloqueadas = False   # tras parar_salidas() ya no se publica nada más

    # Avisos de imágenes descartadas
    self._descartes = 0
    self._motivo_descarte = ""
    self._t_aviso_descarte = 0.0

    self._cliente = None
    self._hilo = None
    self._parar = threading.Event()
    self._aviso_conexion_dado = False

  # --- Registro de bornes (lo llaman las fábricas al crear cada borne) ---

  def registrar_entrada(self, clave, tipo):
    if clave in self._tipos_entrada:
      raise ValueError("La entrada {} del TXT {} ya está creada".format(clave, self.numero_txt))
    self._tipos_entrada[clave] = tipo

  def registrar_salida(self, clave, borne):
    if clave in self._salidas:
      raise ValueError("La salida {} del TXT {} ya está creada".format(clave, self.numero_txt))
    self._salidas[clave] = borne

  # --- Arranque y parada ---

  def arrancar(self):
    """Conecta con el broker y lanza el hilo que vigila la imagen y envía el latido."""
    self._cliente = mqtt.Client(
      callback_api_version=CallbackAPIVersion.VERSION2,
      client_id="vio-txt{}-{}".format(self.numero_txt, uuid.uuid4().hex[:8]),
    )
    self._cliente.on_connect = self._al_conectar
    self._cliente.on_connect_fail = self._al_fallar_conexion
    self._cliente.on_disconnect = self._al_desconectar
    self._cliente.on_message = self._al_recibir
    self._cliente.reconnect_delay_set(min_delay=1, max_delay=5)
    # connect_async no bloquea: si el broker no está, paho reintenta solo.
    # Mientras tanto no hay entradas, así que las lecturas esperan (D006.3).
    self._cliente.connect_async(self.host, self.puerto, keepalive=30)
    self._cliente.loop_start()
    log.warning("TXT %d: esperando la primera imagen en %s. Hasta entonces las lecturas "
                "esperan y no se envía %s.", self.numero_txt, self.topic_in, self.topic_out)
    self._hilo = threading.Thread(target=self._vigilar, name="vio-txt{}".format(self.numero_txt),
                                  daemon=True)
    self._hilo.start()

  def cerrar(self):
    """Para el latido y se desconecta. Unity pasará a estado seguro (D006.5)."""
    self._parar.set()
    if self._hilo is not None:
      self._hilo.join(timeout=1)
    if self._cliente is not None:
      self._cliente.disconnect()
      self._cliente.loop_stop()

  # --- Callbacks de paho (API 2.x) ---

  def _al_conectar(self, cliente, userdata, flags, reason_code, properties):
    if reason_code.is_failure:
      log.warning("TXT %d: el broker %s:%d rechaza la conexión (%s).",
                  self.numero_txt, self.host, self.puerto, reason_code)
      return
    # Se suscribe en cada conexión, así también vale tras una reconexión.
    cliente.subscribe(self.topic_in, qos=QOS)
    self._aviso_conexion_dado = False
    log.info("TXT %d: conectado a %s:%d, escuchando %s.",
             self.numero_txt, self.host, self.puerto, self.topic_in)

  def _al_fallar_conexion(self, cliente, userdata):
    if not self._aviso_conexion_dado:
      log.warning("TXT %d: no se puede conectar con el broker %s:%d. Reintentando...",
                  self.numero_txt, self.host, self.puerto)
      self._aviso_conexion_dado = True

  def _al_desconectar(self, cliente, userdata, flags, reason_code, properties):
    if not self._parar.is_set():
      log.warning("TXT %d: desconectado del broker (%s). Reintentando...",
                  self.numero_txt, reason_code)

  def _al_recibir(self, cliente, userdata, mensaje):
    self.procesar_entrada(mensaje.payload)

  # --- Entradas ---

  def procesar_entrada(self, payload):
    """Valida un mensaje de `in` y, si es correcto, sustituye entera la imagen.

    Un mensaje roto, incompleto o con un tipo incorrecto se descarta entero:
    no se mezclan imágenes parciales (D006.1) y no se renueva la hora, así que
    si solo llegan mensajes malos acaba saltando la regla de 0,5 s.
    """
    try:
      datos = json.loads(payload)
    except (ValueError, UnicodeDecodeError):
      return self._descartar("no es JSON válido")
    if not isinstance(datos, dict):
      return self._descartar("no es un objeto JSON")

    imagen = {}
    for clave, tipo in self._tipos_entrada.items():
      if clave not in datos:
        return self._descartar("falta {}".format(clave))
      valor = datos[clave]
      if tipo is bool:
        if not isinstance(valor, bool):
          return self._descartar("{} debe ser true/false".format(clave))
      else:
        # bool es un int en Python: se rechaza a propósito.
        if isinstance(valor, bool) or not isinstance(valor, (int, float)) or not math.isfinite(valor):
          return self._descartar("{} debe ser un número".format(clave))
        valor = int(valor)
      imagen[clave] = valor
    # Las claves de más se ignoran.

    with self._cond:
      self._foto = (imagen, time.monotonic())
      self._cond.notify_all()

  def _descartar(self, motivo):
    self._descartes += 1
    self._motivo_descarte = motivo
    ahora = time.monotonic()
    if ahora - self._t_aviso_descarte >= _INTERVALO_AVISO_DESCARTE_S:
      log.warning("TXT %d: imagen de %s descartada (%s). Descartadas desde el último aviso: %d.",
                  self.numero_txt, self.topic_in, self._motivo_descarte, self._descartes)
      self._descartes = 0
      self._t_aviso_descarte = ahora

  @staticmethod
  def _es_fresca(foto):
    """True si hay imagen y tiene 0,5 s o menos (D006.3)."""
    return foto is not None and time.monotonic() - foto[1] <= CADUCIDAD_S

  def lazo_sano(self):
    """El lazo de control está sano si la imagen de entradas está fresca (D006.7)."""
    return self._es_fresca(self._foto)

  def leer(self, clave):
    """Devuelve el valor de una entrada. Si la imagen está caducada, espera a una nueva.

    Con imagen fresca no se toma el cerrojo. SLD.py lee en bucles sin pausa, y
    si cada lectura tomara el cerrojo, el hilo de MQTT tardaría en poder guardar
    la imagen nueva. Entonces se podrían perder cambios de C1.
    """
    foto = self._foto
    if self._es_fresca(foto):
      return foto[0][clave]
    with self._cond:
      while not self._es_fresca(self._foto):
        # Se despierta al llegar una imagen o cada 50 ms para volver a mirar.
        self._cond.wait(timeout=0.05)
      return self._foto[0][clave]

  # --- Salidas ---

  def salida_cambiada(self):
    """La llaman los bornes al cambiar. Publica al momento si la imagen ha cambiado."""
    self._publicar_salidas(solo_si_cambia=True)

  def imagen_salidas(self):
    return {clave: self._salidas[clave].valor_vio() for clave in sorted(self._salidas)}

  def parar_salidas(self):
    """Parada de seguridad: todas las salidas a cero, un último `out` y nada más.

    Después se bloquean las salidas: si otro hilo del programa sigue escribiendo,
    ya no se publica. Así la cinta se para en el acto, sin esperar a los 0,5 s
    de D006.5, y no vuelve a arrancar.
    """
    with self._lock_out:
      for borne in self._salidas.values():
        borne.a_cero()
      if self.lazo_sano() and self._cliente is not None:
        info = self._cliente.publish(self.topic_out, json.dumps(self.imagen_salidas()), qos=QOS, retain=RETAIN)
        try:
          info.wait_for_publish(timeout=0.2)
        except (RuntimeError, ValueError):
          pass   # sin conexión: Unity pasará a estado seguro por falta de latido
      self._salidas_bloqueadas = True

  def _publicar_salidas(self, solo_si_cambia):
    with self._lock_out:
      if self._salidas_bloqueadas:
        return
      # D006.7: sin entradas frescas no se envía nada, ni cambios ni latido.
      if not self.lazo_sano():
        return
      imagen = self.imagen_salidas()
      if solo_si_cambia and imagen == self._ultima_out:
        return
      if self._cliente is not None:
        self._cliente.publish(self.topic_out, json.dumps(imagen), qos=QOS, retain=RETAIN)
      self._ultima_out = imagen

  def _vigilar(self):
    """Hilo propio: detecta cuándo caduca o vuelve la imagen y envía el latido cada 100 ms."""
    sano_antes = False
    t_caducada = None
    t_latido = 0.0
    while not self._parar.is_set():
      sano = self.lazo_sano()
      ahora = time.monotonic()
      if sano != sano_antes:
        if sano:
          if t_caducada is None:
            log.info("TXT %d: primera imagen de entradas recibida. Lazo sano: se envía %s.",
                     self.numero_txt, self.topic_out)
          else:
            log.warning("TXT %d: entradas recuperadas tras %.1f s. Se reanuda %s.",
                        self.numero_txt, ahora - t_caducada, self.topic_out)
          t_latido = 0.0   # publica ya, sin esperar al siguiente latido
        else:
          t_caducada = ahora
          log.warning("TXT %d: imagen de entradas caducada (más de %.1f s). Las lecturas esperan "
                      "una nueva y %s deja de enviarse (D006.3, D006.7).",
                      self.numero_txt, CADUCIDAD_S, self.topic_out)
        sano_antes = sano
      if sano and ahora - t_latido >= LATIDO_S:
        self._publicar_salidas(solo_si_cambia=False)
        t_latido = ahora
      time.sleep(_PASO_VIGILANCIA_S)
