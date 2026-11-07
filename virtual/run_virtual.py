"""Lanzador de la planta virtual: ejecuta un programa de control sobre la librería virtual.

- Sin `--programa`: lanza `thread_SLD` de vscode/lib/SLD.py, sin cambiarlo (hito 1).
- Con `--programa ruta.py`: ejecuta el programa de un alumno, de arriba abajo,
  igual que el TXT ejecuta un `main.py` de ROBO Pro Coding (`__name__ == "__main__"`).

En los dos casos este script:
1. Pone virtual/ por delante en sys.path, para que `import fischertechnik`
   cargue la librería virtual, y añade vscode/ para encontrar `lib`.
2. Define los niveles de log propios que usa la SLD, con los mismos números
   que vscode/lib/Log.py, sin importar lib.Log (arrastraría media fábrica).
3. Pide un reloj de 1 ms y configura el broker.

Si el programa del alumno lanza una excepción (en el hilo principal o en uno
suyo), el lanzador para todas las salidas en el acto (motor parado y válvulas
cerradas), enseña el error y termina con código 1. Sin latido, Unity pasa a
estado seguro (D006.5). Si el programa acaba o se para con Ctrl+C, también se
paran las salidas: la cinta nunca se queda en marcha.

Uso:  py -3.12 virtual/run_virtual.py [--programa ruta.py] [--broker localhost] [--puerto 1883] [--log INFO]
"""

import argparse
import logging
import os
import runpy
import sys
import threading
import traceback
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent
VIRTUAL = RAIZ / "virtual"
VSCODE = RAIZ / "vscode"

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

RAYA = "=" * 70


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


def preparar(args):
  # Una línea por print, aunque la salida vaya a un archivo o a otro proceso.
  sys.stdout.reconfigure(line_buffering=True)
  reloj_de_1_ms()
  # Durante el sondeo continuo de I3 y C1, paho necesita recibir las imágenes en otro hilo.
  sys.setswitchinterval(0.001)

  # Sin esto, Python guardaría los .pyc de SLD.py en vscode/lib/__pycache__/,
  # y en vscode/ no se escribe nada.
  sys.dont_write_bytecode = True
  sys.path.insert(0, str(VSCODE))
  sys.path.insert(0, str(VIRTUAL))

  definir_niveles_log()
  nivel = NIVELES_LOG.get(args.log) or getattr(logging, args.log)
  logging.basicConfig(level=nivel, format=FORMATO_LOG, stream=sys.stdout)

  from fischertechnik import vio
  vio.configurar(args.broker, args.puerto)


# --- Sin --programa: SLD.py original ---

def lanzar_sld(args):
  from fischertechnik import factories

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
  return 0


# --- Con --programa: el programa del alumno ---

def _es_del_alumno(archivo, carpeta):
  """True si el archivo es del alumno: está en la carpeta de su programa y no es de la librería."""
  try:
    ruta = Path(archivo).resolve()
  except (OSError, ValueError):
    return False
  if not ruta.is_relative_to(carpeta):
    return False
  return not any(ruta.is_relative_to(p) for p in (VIRTUAL / "fischertechnik", VIRTUAL / "lib", VSCODE))


def mostrar_error(exc, programa, hilo=None):
  """Enseña el error del alumno: su archivo, su línea y el mensaje, sin el ruido de la librería."""
  carpeta = programa.parent
  print(RAYA)
  donde = " (en el hilo {})".format(hilo) if hilo else ""
  if isinstance(exc, SyntaxError) and exc.filename and _es_del_alumno(exc.filename, carpeta):
    # Error de escritura: Python no ha llegado a ejecutar nada.
    print("ERROR en tu programa{}: {}, línea {}".format(donde, Path(exc.filename).name, exc.lineno))
    if exc.text:
      print("    " + exc.text.strip())
    print("{}: {}".format(type(exc).__name__, exc.msg))
  else:
    marcos = [m for m in traceback.extract_tb(exc.__traceback__) if _es_del_alumno(m.filename, carpeta)]
    if marcos:
      ultimo = marcos[-1]
      print("ERROR en tu programa{}: {}, línea {}".format(donde, Path(ultimo.filename).name, ultimo.lineno))
      if ultimo.line:
        print("    " + ultimo.line.strip())
      print("".join(traceback.format_exception_only(exc)).rstrip())
      if len(marcos) > 1:
        print("Camino hasta el error (solo tus archivos):")
        for m in marcos:
          print("  {}, línea {}, en {}".format(Path(m.filename).name, m.lineno, m.name))
    else:
      # El error no pasa por ningún archivo del alumno: se enseña entero.
      print("ERROR al ejecutar tu programa{}:".format(donde))
      print("".join(traceback.format_exception(exc)).rstrip())
  print("La planta se ha parado: motor parado y válvulas y compresor apagados.")
  print(RAYA)


def lanzar_programa(args):
  from fischertechnik import factories

  programa = Path(args.programa).resolve()
  if not programa.is_file():
    print("No encuentro el programa: {}".format(programa))
    return 2

  # Como en el TXT, el programa puede importar archivos que tenga en su misma carpeta.
  sys.path.insert(0, str(programa.parent))

  def error_en_hilo(info):
    """Un hilo del alumno ha fallado: se para todo, igual que si fallara el principal."""
    if issubclass(info.exc_type, SystemExit):
      return
    factories.parar_salidas()
    mostrar_error(info.exc_value, programa, hilo=info.thread.name if info.thread else "?")
    factories.cerrar()
    sys.stdout.flush()
    os._exit(1)

  threading.excepthook = error_en_hilo

  print("Ejecutando tu programa: {}".format(programa))
  print("Broker {}:{}. Ctrl+C para parar.".format(args.broker, args.puerto))
  codigo = 0
  try:
    runpy.run_path(str(programa), run_name="__main__")
    print("Tu programa ha terminado.")
  except SystemExit as fin:
    codigo = fin.code if isinstance(fin.code, int) else (0 if fin.code is None else 1)
    print("Tu programa ha terminado (sys.exit).")
  except KeyboardInterrupt:
    print("Programa parado con Ctrl+C.")
  except BaseException as exc:
    # Primero se para la planta y después se explica el error.
    factories.parar_salidas()
    mostrar_error(exc, programa)
    codigo = 1
  finally:
    factories.parar_salidas()
    factories.cerrar()
  return codigo


def main(argv=None):
  parser = argparse.ArgumentParser(description="Programa de control sobre la librería fischertechnik virtual")
  parser.add_argument("--programa", help="programa del alumno (.py). Sin él se lanza SLD.py original")
  parser.add_argument("--broker", default="localhost", help="host del broker MQTT (por defecto localhost)")
  parser.add_argument("--puerto", type=int, default=1883, help="puerto del broker (por defecto 1883)")
  parser.add_argument("--log", default="INFO", choices=["WARNING", "INFO", "DEBUG", "TRACE", "TRACE0"],
                      help="nivel de log (por defecto INFO; DEBUG enseña los pasos de SLD.py)")
  args = parser.parse_args(argv)

  preparar(args)
  if args.programa:
    return lanzar_programa(args)
  return lanzar_sld(args)


if __name__ == "__main__":
  sys.exit(main())
