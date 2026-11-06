"""Mide desde fuera el ritmo real de `vio/txtN/in` (F0.5, criterio b).

Escucha el topic durante un tiempo y dice:
- cuántos mensajes llegan cada segundo;
- la media de Hz, el hueco más largo entre dos mensajes y cuántos huecos pasan de 20 ms;
- cuántos mensajes no son una imagen de entradas válida (claves y tipos de D006.4).

Veredicto del criterio b de F0.5: media entre 98 y 102 Hz y ningún hueco de más de 30 ms.
Sale con código 0 si cumple y 1 si no.

Uso (con Unity en Play):
    py -3.12 virtual/medir_vio.py                 # 60 s, TXT 5, localhost:1883
    py -3.12 virtual/medir_vio.py --segundos 20
"""

import argparse
import json
import math
import sys
import threading
import time

import paho.mqtt.client as mqtt
from paho.mqtt.enums import CallbackAPIVersion

# Claves y tipos de la imagen de entradas del TXT 5 (D006.4). Los mismos que
# registra la librería virtual al crear los bornes.
TIPOS_IN = {"I1": bool, "I2": int, "I3": bool, "I6": bool, "I7": bool, "I8": bool, "C1": bool}

HZ_MIN, HZ_MAX = 98.0, 102.0   # criterio b: media
HUECO_MAX_MS = 30.0            # criterio b: ningún hueco mayor
HUECO_AVISO_MS = 20.0          # se cuentan aparte, para ver la calidad


def imagen_valida(texto):
  """True si el mensaje es una imagen de entradas completa y con los tipos de D006.4."""
  try:
    img = json.loads(texto)
  except (ValueError, UnicodeDecodeError):
    return False
  if not isinstance(img, dict) or set(img) != set(TIPOS_IN):
    return False
  for clave, tipo in TIPOS_IN.items():
    v = img[clave]
    if tipo is bool and not isinstance(v, bool):
      return False
    # bool es un int en Python: se rechaza aparte, igual que en vio.py
    if tipo is int and (isinstance(v, bool) or not isinstance(v, int)):
      return False
  return True


def main():
  p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
  p.add_argument("--broker", default="localhost")
  p.add_argument("--puerto", type=int, default=1883)
  p.add_argument("--txt", type=int, default=5)
  p.add_argument("--segundos", type=float, default=60.0)
  a = p.parse_args()
  topic = "vio/txt{}/in".format(a.txt)

  llegadas = []      # (hora de llegada, válido)
  cerrojo = threading.Lock()
  conectado = threading.Event()

  def al_conectar(cliente, userdata, flags, reason_code, properties):
    if not reason_code.is_failure:
      cliente.subscribe(topic, qos=0)
      conectado.set()

  def al_recibir(cliente, userdata, msg):
    t = time.perf_counter()   # hora de llegada, con resolución de microsegundos
    ok = imagen_valida(msg.payload.decode("utf-8", errors="replace"))
    with cerrojo:
      llegadas.append((t, ok))

  cliente = mqtt.Client(callback_api_version=CallbackAPIVersion.VERSION2,
                        client_id="medir-vio-{}".format(int(time.time())))
  cliente.on_connect = al_conectar
  cliente.on_message = al_recibir
  cliente.connect(a.broker, a.puerto, keepalive=30)
  cliente.loop_start()
  if not conectado.wait(5):
    print("No puedo conectar con {}:{}".format(a.broker, a.puerto))
    return 1

  print("Escuchando {} durante {:.0f} s...".format(topic, a.segundos))
  inicio = time.perf_counter()
  fin = inicio + a.segundos
  vistos = 0
  while time.perf_counter() < fin:
    time.sleep(1.0)
    with cerrojo:
      n = len(llegadas)
    print("  s {:3.0f}: {:4d} mensajes".format(time.perf_counter() - inicio, n - vistos))
    vistos = n
  cliente.loop_stop()
  cliente.disconnect()

  with cerrojo:
    datos = [x for x in llegadas if x[0] <= fin]
  if len(datos) < 2:
    print("\nNo ha llegado nada (o casi nada) por {}. ¿Está Unity en Play?".format(topic))
    return 1

  horas = [t for t, _ in datos]
  invalidos = sum(1 for _, ok in datos if not ok)
  huecos_ms = [(b - a_) * 1000 for a_, b in zip(horas, horas[1:])]
  duracion = horas[-1] - horas[0]
  hz = (len(horas) - 1) / duracion
  hueco_max = max(huecos_ms)
  n_20 = sum(1 for h in huecos_ms if h > HUECO_AVISO_MS)
  n_30 = sum(1 for h in huecos_ms if h > HUECO_MAX_MS)
  media = sum(huecos_ms) / len(huecos_ms)
  desv = math.sqrt(sum((h - media) ** 2 for h in huecos_ms) / len(huecos_ms))

  print("\nResultado ({} mensajes en {:.1f} s):".format(len(horas), duracion))
  print("  media           {:.2f} Hz".format(hz))
  print("  hueco medio     {:.2f} ms (desviación {:.2f} ms)".format(media, desv))
  print("  hueco más largo {:.1f} ms".format(hueco_max))
  print("  huecos > 20 ms  {} ({:.2f} %)".format(n_20, 100.0 * n_20 / len(huecos_ms)))
  print("  huecos > 30 ms  {}".format(n_30))
  print("  inválidos       {}".format(invalidos))

  cumple = HZ_MIN <= hz <= HZ_MAX and n_30 == 0 and invalidos == 0
  print("\nCriterio b de F0.5 (media {:.0f}–{:.0f} Hz, ningún hueco > {:.0f} ms, ningún inválido): {}".format(
    HZ_MIN, HZ_MAX, HUECO_MAX_MS, "CUMPLE" if cumple else "NO CUMPLE"))
  return 0 if cumple else 1


if __name__ == "__main__":
  sys.exit(main())
