"""Arranca solo la SLD (`thread_SLD` de vscode/lib/SLD.py) sobre la librería virtual.

SLD.py no se modifica. Este script:
1. Pone virtual/ por delante en sys.path, para que `import fischertechnik`
   cargue la librería virtual, y añade vscode/ para encontrar `lib`.
2. Define los niveles de log propios que usa la SLD, con los mismos números
   que vscode/lib/Log.py, sin importar lib.Log (arrastraría media fábrica).
3. Configura el broker y lanza thread_SLD.

Uso:  py -3.12 virtual/run_sld_virtual.py [--broker localhost] [--puerto 1883] [--log INFO]
"""

import argparse
import logging
import os
import sys
import threading
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent

# Niveles de vscode/lib/Log.py:
#   initlog() -> TRACE0 = 1, TRACE = 9
#   initlog_GUI(5, 0, 0) -> TRACE0_GUI = 5, TRACE_GUI = 0, DEBUG_GUI = 0
#   initlog_FCL(4, 0, 0) -> TRACE0_FCL = 4, TRACE_FCL = 0, DEBUG_FCL = 0
# La SLD usa DEBUG, TRACE, TRACE0, TRACE0_GUI (SLD_Display), TRACE_FCL y DEBUG_FCL (SLD_MQTT).
NIVELES_LOG = {
  "TRACE0": 1,
  "TRACE": 9,
  "TRACE0_GUI": 5,
  "TRACE_GUI": 0,
  "DEBUG_GUI": 0,
  "TRACE0_FCL": 4,
  "TRACE_FCL": 0,
  "DEBUG_FCL": 0,
}

# Mismo formato que Log.py
FORMATO_LOG = "%(asctime)s [%(threadName)s] %(levelname)-10s %(funcName)3s %(message)s   #%(filename)3s:%(lineno)d"


def definir_niveles_log():
  for nombre, numero in NIVELES_LOG.items():
    setattr(logging, nombre, numero)
    # Al nivel 0 no se le pone nombre: pisaría el de NOTSET. Un mensaje de nivel 0
    # nunca se muestra, igual que en el TXT con esta configuración.
    if numero > 0:
      logging.addLevelName(numero, nombre)


def reloj_de_1_ms():
  """En Windows pide un reloj del sistema de 1 ms para este proceso.

  Por defecto Windows despierta los sleep cada ~15,6 ms. Con 1 ms, el
  time.sleep(0.001) de SLD.py dura lo mismo que en el TXT (Linux) y el hilo
  de vio vigila la imagen cada 10 ms de verdad.
  """
  if os.name == "nt":
    import atexit
    import ctypes
    ctypes.windll.winmm.timeBeginPeriod(1)
    atexit.register(ctypes.windll.winmm.timeEndPeriod, 1)


def main():
  parser = argparse.ArgumentParser(description="SLD.py original sobre la librería fischertechnik virtual")
  parser.add_argument("--broker", default="localhost", help="host del broker MQTT (por defecto localhost)")
  parser.add_argument("--puerto", type=int, default=1883, help="puerto del broker (por defecto 1883)")
  parser.add_argument("--log", default="INFO", choices=["WARNING", "INFO", "DEBUG", "TRACE", "TRACE0"],
                      help="nivel de log (por defecto INFO; DEBUG enseña los pasos de SLD.py)")
  args = parser.parse_args()

  # Una línea por print, aunque la salida vaya a un archivo o a otro proceso.
  sys.stdout.reconfigure(line_buffering=True)
  reloj_de_1_ms()

  # Sin esto, Python guardaría los .pyc de SLD.py en vscode/lib/__pycache__/,
  # y en vscode/ no se escribe nada.
  sys.dont_write_bytecode = True
  sys.path.insert(0, str(RAIZ / "vscode"))
  sys.path.insert(0, str(RAIZ / "virtual"))

  definir_niveles_log()
  nivel = NIVELES_LOG.get(args.log) or getattr(logging, args.log)
  logging.basicConfig(level=nivel, format=FORMATO_LOG, stream=sys.stdout)

  from fischertechnik import factories, vio
  vio.configurar(args.broker, args.puerto)

  # Importar lib.SLD ejecuta lib.controller: crea las E/S de los 6 TXT y arranca vio.
  from lib.SLD import thread_SLD

  print("SLD virtual en marcha. Broker {}:{}. Ctrl+C para parar.".format(args.broker, args.puerto))
  hilo = threading.Thread(target=thread_SLD, name="thread_SLD", daemon=True)
  hilo.start()
  try:
    while hilo.is_alive():
      hilo.join(timeout=0.5)
    print("thread_SLD ha terminado.")
  except KeyboardInterrupt:
    print("Parando la SLD virtual...")
  finally:
    factories.cerrar()


if __name__ == "__main__":
  main()
