"""Maqueta falsa del TXT 5 (SLD) manejada desde el teclado (D006.6).

- Publica la imagen de entradas en `vio/txt5/in` a 100 Hz (configurable).
- Con el teclado cambias I1, I2, I3, I6, I7, I8 y C1.
- Muestra lo que llega por `vio/txt5/out`.

Sirve para probar SLD.py sin Unity. Un mensaje publicado a mano en MQTT
Explorer no vale: caduca a los 0,5 s (D006.3).

Uso (PowerShell):  py -3.12 virtual/maqueta_manual.py [--broker localhost] [--puerto 1883] [--hz 100]
"""

import argparse
import collections
import copy
import json
import os
import sys
import threading
import time
import uuid

import paho.mqtt.client as mqtt
from paho.mqtt.enums import CallbackAPIVersion

# Valores de I2 para las pruebas. VALORES DE PRUEBA, NO MEDIDOS: son el centro
# de cada rango de color de SLD.py, con los umbrales con los que arranca
# thread_SLD (200, 1150, 1400 y 2000). El 1460 de set_calib_data_SLD solo se
# aplica si alguien llama a esa función.
I2_BLANCO = 675        # centro de [200, 1150)
I2_ROJO = 1275         # centro de [1150, 1400)
I2_AZUL = 1700         # centro de [1400, 2000)
I2_SIN_PIEZA = 2500    # por encima de todos: SLD.py se queda con el MÍNIMO
I2_PASO = 50           # paso de las teclas + y -

UMBRALES_SLD = (200, 1150, 1400, 2000)

ENTRADAS_INICIALES = {
  "I1": False, "I2": I2_SIN_PIEZA, "I3": False,
  "I6": False, "I7": False, "I8": False, "C1": False,
}

NOMBRES = {
  "I1": "barrera de entrada",
  "I2": "sensor de color",
  "I3": "barrera de los cilindros",
  "I6": "barrera rampa blanca",
  "I7": "barrera rampa roja",
  "I8": "barrera rampa azul",
  "C1": "contador (final de carrera)",
  "M1": "motor de la cinta",
  "O5": "válvula blanca",
  "O6": "válvula roja",
  "O7": "válvula azul",
  "O8": "compresor",
}


def reloj_de_1_ms():
  """En Windows pide un reloj del sistema de 1 ms para este proceso.

  Por defecto Windows despierta los sleep cada ~15,6 ms: la imagen saldría a
  unos 50 Hz en vez de 100 y SLD.py perdería cambios de C1 (medido en F0.4).
  """
  if os.name == "nt":
    import atexit
    import ctypes
    ctypes.windll.winmm.timeBeginPeriod(1)
    atexit.register(ctypes.windll.winmm.timeEndPeriod, 1)


def color_de(i2):
  """Qué color vería SLD.py con este valor de I2."""
  minimo, blanco_rojo, rojo_azul, maximo = UMBRALES_SLD
  if i2 < minimo:
    return "fuera de rango (<200)"
  if i2 < blanco_rojo:
    return "blanco"
  if i2 < rojo_azul:
    return "rojo"
  if i2 < maximo:
    return "azul"
  return "fuera de rango (sin pieza)"


class MaquetaVirtual:
  """Publica la imagen de entradas y recoge la de salidas. La usan la interfaz y los tests."""

  def __init__(self, host="localhost", puerto=1883, hz=100.0, txt=5):
    self.host = host
    self.puerto = puerto
    self.hz = float(hz)
    self.topic_in = "vio/txt{}/in".format(txt)
    self.topic_out = "vio/txt{}/out".format(txt)

    self._lock = threading.Lock()
    self._entradas = dict(ENTRADAS_INICIALES)
    self.cambios_c1 = 0

    self.ultima_out = None
    self.t_ultima_out = None
    # Historial de imágenes de out distintas a la anterior: (hora local, monotónico, imagen)
    self.historial_out = collections.deque(maxlen=2000)
    self._llegadas_out = collections.deque()   # para medir mensajes de out por segundo
    self._envios_in = collections.deque()      # para medir la frecuencia real de in

    self._parar = threading.Event()
    self._hilo = None
    self._conectado = threading.Event()
    self._cliente = mqtt.Client(
      callback_api_version=CallbackAPIVersion.VERSION2,
      client_id="maqueta-manual-{}".format(uuid.uuid4().hex[:8]),
    )
    self._cliente.on_connect = self._al_conectar
    self._cliente.on_message = self._al_recibir

  # --- Conexión ---

  def arrancar(self, espera_s=3.0):
    reloj_de_1_ms()
    try:
      self._cliente.connect(self.host, self.puerto, keepalive=30)
    except OSError as error:
      raise ConnectionError("No se puede conectar con el broker {}:{} ({}). ¿Está Mosquitto en marcha?"
                            .format(self.host, self.puerto, error)) from error
    self._cliente.loop_start()
    if not self._conectado.wait(espera_s):
      self._cliente.loop_stop()
      raise ConnectionError("El broker {}:{} no responde".format(self.host, self.puerto))
    self._hilo = threading.Thread(target=self._publicar, name="maqueta-in", daemon=True)
    self._hilo.start()

  def parar(self):
    self._parar.set()
    if self._hilo is not None:
      self._hilo.join(timeout=1)
    self._cliente.disconnect()
    self._cliente.loop_stop()

  def _al_conectar(self, cliente, userdata, flags, reason_code, properties):
    if not reason_code.is_failure:
      cliente.subscribe(self.topic_out, qos=0)
      self._conectado.set()

  def _al_recibir(self, cliente, userdata, mensaje):
    try:
      imagen = json.loads(mensaje.payload)
    except ValueError:
      return
    ahora = time.monotonic()
    with self._lock:
      if imagen != self.ultima_out:
        self.historial_out.append((time.strftime("%H:%M:%S") + ".{:03d}".format(int(time.time() * 1000) % 1000),
                                   ahora, imagen))
      self.ultima_out = imagen
      self.t_ultima_out = ahora
      self._llegadas_out.append(ahora)

  # --- Publicación de la imagen de entradas ---

  def _publicar(self):
    periodo = 1.0 / self.hz
    siguiente = time.monotonic()
    while not self._parar.is_set():
      with self._lock:
        carga = json.dumps(self._entradas)
      self._cliente.publish(self.topic_in, carga, qos=0, retain=False)
      ahora = time.monotonic()
      with self._lock:
        self._envios_in.append(ahora)
      siguiente += periodo
      espera = siguiente - ahora
      if espera > 0:
        time.sleep(espera)
      elif espera < -periodo:
        siguiente = time.monotonic()   # muy atrasado: no intenta recuperar de golpe

  # --- Cambiar entradas ---

  def poner(self, clave, valor):
    with self._lock:
      if clave == "C1" and self._entradas["C1"] != valor:
        self.cambios_c1 += 1
      self._entradas[clave] = valor

  def conmutar(self, clave):
    with self._lock:
      valor = not self._entradas[clave]
    self.poner(clave, valor)

  def mover_i2(self, delta):
    with self._lock:
      self._entradas["I2"] = max(0, self._entradas["I2"] + delta)

  # --- Consultas ---

  def entradas(self):
    with self._lock:
      return dict(self._entradas)

  def estado_out(self):
    with self._lock:
      return copy.deepcopy(self.ultima_out), self.t_ultima_out

  def historial(self):
    with self._lock:
      return list(self.historial_out)

  def _por_segundo(self, cola):
    limite = time.monotonic() - 1.0
    with self._lock:
      while cola and cola[0] < limite:
        cola.popleft()
      return len(cola)

  def hz_in(self):
    return self._por_segundo(self._envios_in)

  def mensajes_out_por_segundo(self):
    return self._por_segundo(self._llegadas_out)


# --- Interfaz de teclado (Windows) ---

AYUDA = [
  "[1] I1  [3] I3  [6] I6  [7] I7  [8] I8  [c] C1   (conmutan)",
  "I2: [b] blanco {}  [r] rojo {}  [a] azul {}  [n] sin pieza {}  [+]/[-] paso {}".format(
    I2_BLANCO, I2_ROJO, I2_AZUL, I2_SIN_PIEZA, I2_PASO),
  "(I2 son valores de prueba, no medidos)   [q] salir",
]


def _texto_out(imagen):
  m1 = imagen.get("M1", {})
  def bit(clave):
    return "ON " if imagen.get(clave) else "-- "
  return "M1 {:>3} {:<3} {}  O5 {} O6 {} O7 {} O8 {}".format(
    m1.get("speed", "?"), m1.get("dir", "?"), "marcha" if m1.get("running") else "parado",
    bit("O5"), bit("O6"), bit("O7"), bit("O8"))


def dibujar(maqueta):
  e = maqueta.entradas()
  out, t_out = maqueta.estado_out()
  ahora = time.monotonic()
  def oscuro(clave):
    return "OSCURO (true) " if e[clave] else "luz    (false)"

  lineas = [
    "MAQUETA MANUAL · TXT 5 (SLD) · broker {}:{}".format(maqueta.host, maqueta.puerto),
    "",
    "ENTRADAS  {}  ·  {} Hz reales (objetivo {:.0f})".format(maqueta.topic_in, maqueta.hz_in(), maqueta.hz),
    "  [1] I1 {:<28} {}".format(NOMBRES["I1"], oscuro("I1")),
    "      I2 {:<28} {:>5}  -> {}".format(NOMBRES["I2"], e["I2"], color_de(e["I2"])),
    "  [3] I3 {:<28} {}".format(NOMBRES["I3"], oscuro("I3")),
    "  [6] I6 {:<28} {}".format(NOMBRES["I6"], oscuro("I6")),
    "  [7] I7 {:<28} {}".format(NOMBRES["I7"], oscuro("I7")),
    "  [8] I8 {:<28} {}".format(NOMBRES["I8"], oscuro("I8")),
    "  [c] C1 {:<28} {}   cambios: {}".format(NOMBRES["C1"], "true " if e["C1"] else "false",
                                              maqueta.cambios_c1),
    "",
  ]
  if out is None:
    lineas.append("SALIDAS  {}  ·  todavía no ha llegado nada".format(maqueta.topic_out))
  else:
    edad = ahora - t_out
    lineas.append("SALIDAS  {}  ·  último hace {:.2f} s  ·  {} msg/s".format(
      maqueta.topic_out, edad, maqueta.mensajes_out_por_segundo()))
    m1 = out.get("M1", {})
    lineas.append("  M1 {:<28} speed {}  dir {}  running {}".format(
      NOMBRES["M1"], m1.get("speed"), m1.get("dir"), str(m1.get("running")).lower()))
    for clave in ("O5", "O6", "O7", "O8"):
      lineas.append("  {} {:<28} {}".format(clave, NOMBRES[clave], "ON  (true)" if out.get(clave) else "off (false)"))
    if edad > 0.5:
      lineas.append("  !! Sin out desde hace {:.1f} s: la librería no está en marcha o espera entradas (D006.7)".format(edad))
  lineas.append("")
  lineas.append("CAMBIOS DE out (últimos 8)")
  for hora, _, imagen in maqueta.historial()[-8:]:
    lineas.append("  {}  {}".format(hora, _texto_out(imagen)))
  lineas.append("")
  lineas.extend(AYUDA)

  # \x1b[H = cursor arriba a la izquierda; \x1b[K = borra el resto de la línea; \x1b[J = borra lo de abajo
  sys.stdout.write("\x1b[H" + "".join(linea + "\x1b[K\n" for linea in lineas) + "\x1b[J")
  sys.stdout.flush()


def tecla(maqueta, ch):
  """Aplica una tecla. Devuelve False si hay que salir."""
  ch = ch.lower()
  conmutables = {"1": "I1", "3": "I3", "6": "I6", "7": "I7", "8": "I8", "c": "C1"}
  if ch in conmutables:
    maqueta.conmutar(conmutables[ch])
  elif ch == "b":
    maqueta.poner("I2", I2_BLANCO)
  elif ch == "r":
    maqueta.poner("I2", I2_ROJO)
  elif ch == "a":
    maqueta.poner("I2", I2_AZUL)
  elif ch == "n":
    maqueta.poner("I2", I2_SIN_PIEZA)
  elif ch == "+":
    maqueta.mover_i2(I2_PASO)
  elif ch == "-":
    maqueta.mover_i2(-I2_PASO)
  elif ch == "q":
    return False
  return True


def main():
  parser = argparse.ArgumentParser(description="Maqueta manual del TXT 5 (SLD) para el protocolo vio")
  parser.add_argument("--broker", default="localhost")
  parser.add_argument("--puerto", type=int, default=1883)
  parser.add_argument("--hz", type=float, default=100.0, help="frecuencia de vio/txt5/in (por defecto 100)")
  args = parser.parse_args()

  if os.name != "nt":
    sys.exit("maqueta_manual.py lee el teclado con msvcrt: funciona en Windows (PowerShell o Windows Terminal).")
  import msvcrt

  maqueta = MaquetaVirtual(args.broker, args.puerto, args.hz)
  try:
    maqueta.arrancar()
  except ConnectionError as error:
    sys.exit(str(error))

  os.system("")                        # activa los códigos ANSI en la consola de Windows
  sys.stdout.write("\x1b[2J\x1b[?25l")  # borra la pantalla y oculta el cursor
  try:
    seguir = True
    while seguir:
      while msvcrt.kbhit():
        ch = msvcrt.getwch()
        if ch in ("\x00", "\xe0"):     # flechas y teclas de función: traen un segundo carácter
          msvcrt.getwch()
          continue
        if not tecla(maqueta, ch):
          seguir = False
          break
      dibujar(maqueta)
      time.sleep(0.1)
  except KeyboardInterrupt:
    pass
  finally:
    maqueta.parar()
    sys.stdout.write("\x1b[?25h\n")    # vuelve a enseñar el cursor
    print("Maqueta cerrada: ya no se publica {}.".format(maqueta.topic_in))


if __name__ == "__main__":
  main()
