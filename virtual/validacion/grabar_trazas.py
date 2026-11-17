"""Graba en un archivo JSONL los mensajes MQTT de la SLD, real o virtual (validación, V1).

Cada línea del archivo es un mensaje tal como llega:
  {"t_pc": 1763390412.123, "topic": "dt/sld/belt", "retain": false, "payload": {...}}
La primera línea es una cabecera con el origen, el color de las piezas y el broker.

Se graba de forma continua: mete las 5 piezas de un color seguidas, una detrás de
otra, y para con Ctrl+C. Después `a_eventos.py` separa los ciclos (uno por pieza).

Valores por defecto según el origen:
  real     -> broker localhost:1884, topic dt/sld/#         (FactoryMain en el TXT)
  virtual  -> broker localhost:1883, topics vio/txt5/in y vio/txt5/out  (D007: vio es la única fuente)

Uso (PowerShell, en Github/TFM):
  py -3.12 virtual/validacion/grabar_trazas.py --origen real --color blanca
  py -3.12 virtual/validacion/grabar_trazas.py --origen virtual --color roja
  py -3.12 virtual/validacion/grabar_trazas.py --origen real --color azul --config virtual/validacion/config_local.json

--config (opcional) es un JSON local con broker, puerto, usuario y clave. NO va al
repositorio (está en .gitignore). Mira config_local.example.json.
"""

import argparse
import json
import sys
import threading
import time
from datetime import datetime
from pathlib import Path

AQUI = Path(__file__).resolve().parent

ORIGENES = {
  "real": {"puerto": 1884, "topics": ["dt/sld/#"]},
  "virtual": {"puerto": 1883, "topics": ["vio/txt5/in", "vio/txt5/out"]},
}
COLORES = ["blanca", "roja", "azul", "mezcla"]


def leer_config(ruta):
  if not ruta:
    return {}
  with open(ruta, encoding="utf-8") as f:
    return json.load(f)


def decodificar(carga):
  """Devuelve el JSON del mensaje, o el texto tal cual si no es JSON."""
  texto = carga.decode("utf-8", errors="replace")
  try:
    return json.loads(texto)
  except ValueError:
    return texto


def linea_mensaje(t_pc, topic, retain, carga):
  return json.dumps({"t_pc": t_pc, "topic": topic, "retain": bool(retain), "payload": decodificar(carga)},
                    ensure_ascii=False)


def main(argv=None):
  p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
  p.add_argument("--origen", choices=sorted(ORIGENES), required=True)
  p.add_argument("--color", choices=COLORES, required=True,
                 help="color de TODAS las piezas de esta grabación (mezcla = sin comprobar clasificación)")
  p.add_argument("--config", help="JSON local con broker/puerto/usuario/clave (no va al repo)")
  p.add_argument("--broker")
  p.add_argument("--puerto", type=int)
  p.add_argument("--usuario")
  p.add_argument("--clave")
  p.add_argument("--topics", nargs="+", help="por defecto, los del origen")
  p.add_argument("--segundos", type=float, default=0, help="0 = hasta Ctrl+C")
  p.add_argument("--salida", help="por defecto trazas/<origen>_<color>_<fecha>.jsonl")
  a = p.parse_args(argv)

  import paho.mqtt.client as mqtt
  from paho.mqtt.enums import CallbackAPIVersion

  cfg = leer_config(a.config)
  broker = a.broker or cfg.get("broker", "localhost")
  puerto = a.puerto or cfg.get("puerto", ORIGENES[a.origen]["puerto"])
  usuario = a.usuario or cfg.get("usuario")
  clave = a.clave or cfg.get("clave")
  topics = a.topics or ORIGENES[a.origen]["topics"]

  if a.salida:
    salida = Path(a.salida)
  else:
    salida = AQUI / "trazas" / "{}_{}_{}.jsonl".format(a.origen, a.color, datetime.now().strftime("%Y%m%d_%H%M%S"))
  salida.parent.mkdir(parents=True, exist_ok=True)

  archivo = open(salida, "w", encoding="utf-8")
  cabecera = {"cabecera": {"origen": a.origen, "color": a.color, "broker": broker, "puerto": puerto,
                           "topics": topics, "inicio": datetime.now().isoformat(timespec="milliseconds")}}
  archivo.write(json.dumps(cabecera, ensure_ascii=False) + "\n")
  archivo.flush()

  cerrojo = threading.Lock()
  cuenta = {"n": 0}

  def al_conectar(cliente, userdata, flags, reason_code, properties):
    if reason_code.is_failure:
      print("El broker {}:{} rechaza la conexión: {}".format(broker, puerto, reason_code))
      return
    for t in topics:
      cliente.subscribe(t, qos=1)
    print("Conectado a {}:{}. Grabando {} en {}".format(broker, puerto, ", ".join(topics), salida))
    print("Mete las piezas ({}) una detrás de otra. Ctrl+C para terminar.".format(a.color))

  def al_recibir(cliente, userdata, m):
    linea = linea_mensaje(time.time(), m.topic, m.retain, m.payload)
    with cerrojo:
      archivo.write(linea + "\n")
      archivo.flush()
      cuenta["n"] += 1

  cliente = mqtt.Client(callback_api_version=CallbackAPIVersion.VERSION2,
                        client_id="grabador-{}-{}".format(a.origen, int(time.time())))
  if usuario:
    cliente.username_pw_set(usuario, clave)
  cliente.on_connect = al_conectar
  cliente.on_message = al_recibir
  try:
    cliente.connect(broker, puerto, keepalive=30)
  except OSError as e:
    print("No se puede conectar con {}:{} ({}). ¿Está arrancado ese broker?".format(broker, puerto, e))
    archivo.close()
    return 1
  cliente.loop_start()

  inicio = time.time()
  try:
    while a.segundos <= 0 or time.time() - inicio < a.segundos:
      time.sleep(5)
      print("  {} mensajes grabados".format(cuenta["n"]))
  except KeyboardInterrupt:
    pass
  finally:
    cliente.loop_stop()
    cliente.disconnect()
    with cerrojo:
      archivo.close()
  print("Listo: {} mensajes en {}".format(cuenta["n"], salida))
  return 0


if __name__ == "__main__":
  sys.exit(main())
