"""Mide la latencia PC ↔ TXT por la wifi con dt/ping → dt/pong (validación, V0).

Digital_Twin.py del TXT devuelve por dt/pong el mismo payload que recibe por
dt/ping. Ojo: solo contesta cuando los ejes de la fábrica ya están referenciados
(Digital_Twin.py: espera a get_axes_ready()).

Uso (con FactoryMain-DigitalTwin en marcha y el broker 1884 arrancado):
  py -3.12 virtual/validacion/medir_ping.py [--broker localhost] [--puerto 1884] [--n 10]

Imprime cada ida y vuelta, la media y el máximo. La latencia de un solo sentido
es aproximadamente la mitad.
"""

import argparse
import json
import sys
import threading
import time
import uuid


def main(argv=None):
  p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
  p.add_argument("--broker", default="localhost")
  p.add_argument("--puerto", type=int, default=1884)
  p.add_argument("--usuario")
  p.add_argument("--clave")
  p.add_argument("--n", type=int, default=10)
  p.add_argument("--espera", type=float, default=2.0, help="segundos máximos para cada pong")
  a = p.parse_args(argv)
  if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # la consola de Windows (cp1252) no sabe escribir algunos símbolos

  import paho.mqtt.client as mqtt
  from paho.mqtt.enums import CallbackAPIVersion

  marca = uuid.uuid4().hex[:8]
  pendientes, llegadas = {}, {}
  evento = threading.Event()

  def al_recibir(cliente, userdata, m):
    try:
      dato = json.loads(m.payload.decode("utf-8"))
    except ValueError:
      return
    if isinstance(dato, dict) and dato.get("marca") == marca and dato.get("id") in pendientes:
      llegadas[dato["id"]] = time.perf_counter()
      evento.set()

  cliente = mqtt.Client(callback_api_version=CallbackAPIVersion.VERSION2, client_id="ping-" + marca)
  if a.usuario:
    cliente.username_pw_set(a.usuario, a.clave)
  cliente.on_message = al_recibir
  cliente.connect(a.broker, a.puerto, keepalive=30)
  cliente.subscribe("dt/pong", qos=0)
  cliente.loop_start()
  time.sleep(0.5)

  rtts = []
  for i in range(a.n):
    evento.clear()
    pendientes[i] = time.perf_counter()
    cliente.publish("dt/ping", json.dumps({"marca": marca, "id": i}), qos=0)
    if evento.wait(a.espera) and i in llegadas:
      rtt = (llegadas[i] - pendientes[i]) * 1000
      rtts.append(rtt)
      print("  ping {:>2}: {:7.1f} ms".format(i + 1, rtt))
    else:
      print("  ping {:>2}: sin respuesta en {} s".format(i + 1, a.espera))
    time.sleep(0.3)

  cliente.loop_stop()
  cliente.disconnect()
  if not rtts:
    print("Ningún pong. ¿Está FactoryMain en marcha, conectado al {}:{} y con los ejes referenciados?".format(
      a.broker, a.puerto))
    return 1
  print("Ida y vuelta: media {:.1f} ms, máximo {:.1f} ms, {} de {} respondidos. Un sentido ≈ {:.1f} ms.".format(
    sum(rtts) / len(rtts), max(rtts), len(rtts), a.n, sum(rtts) / len(rtts) / 2))
  return 0


if __name__ == "__main__":
  sys.exit(main())
