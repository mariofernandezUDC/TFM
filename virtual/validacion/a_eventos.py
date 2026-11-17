"""Convierte una traza (real, virtual o vídeo) en ciclos de eventos, uno por pieza (validación, V4).

Los tres orígenes acaban en la misma lista de eventos con su tiempo en segundos:

  I1_on         la pieza tapa la barrera de entrada (I1)
  cinta_on_1    la cinta arranca (512)
  I3_on         la pieza llega a la barrera de los cilindros (I3)
  cinta_on_2    la cinta vuelve a arrancar, más despacio (350), contando C1
  cilindro_on   se abre la válvula de un cilindro (fin de la fase 2)
  rampa         la barrera de la rampa (I6, I7 o I8) se tapa

Esta es la secuencia real de vscode/lib/SLD.py (líneas 63-112). La cinta se para
un instante entre la fase 1 y la 2, pero en la traza real (muestreo de 0,2 s) esa
parada casi nunca se ve: la velocidad pasa de 512 a 350 directamente. Por eso la
fase 2 se detecta como "primer cambio de velocidad después de I3".

De dónde sale el tiempo:
  real     -> campo "ts" del payload, que pone el TXT. Así se quita el retraso de la wifi.
  virtual  -> t_pc, la hora del PC al recibir (vio va por localhost, sin wifi).
  vídeo    -> fotograma / fps, de la plantilla video_real.csv.

Los mensajes retenidos (retain) solo sirven para conocer el estado inicial: no
generan eventos, porque son viejos.

Uso:
  py -3.12 virtual/validacion/a_eventos.py virtual/validacion/trazas/real_blanca_XXXX.jsonl
"""

import csv
import json
import sys
from datetime import datetime, timezone

COLOR_ES = {"WHITE": "blanca", "RED": "roja", "BLUE": "azul"}
VALVULA_COLOR = {"O5": "blanca", "O6": "roja", "O7": "azul"}
BARRERA_COLOR = {"I6": "blanca", "I7": "roja", "I8": "azul"}
CAMPO_RAMPA_REAL = {"is_white": "blanca", "is_red": "roja", "is_blue": "azul"}

# Orden obligatorio dentro de un ciclo. "rampa" se comprueba solo si aparece.
SECUENCIA = ["I1_on", "cinta_on_1", "I3_on", "cinta_on_2", "cilindro_on"]


# --- Lectura ---------------------------------------------------------------

def leer_traza(ruta):
  """Devuelve (cabecera, mensajes) de un JSONL de grabar_trazas.py."""
  cabecera, mensajes = {}, []
  with open(ruta, encoding="utf-8") as f:
    for linea in f:
      linea = linea.strip()
      if not linea:
        continue
      dato = json.loads(linea)
      if "cabecera" in dato:
        cabecera = dato["cabecera"]
      else:
        mensajes.append(dato)
  return cabecera, mensajes


def ts_a_segundos(ts):
  """'2026-11-17T15:40:12.345Z' (Time.timestamp_utcnow del TXT) -> segundos epoch."""
  return datetime.strptime(ts.rstrip("Z"), "%Y-%m-%dT%H:%M:%S.%f").replace(tzinfo=timezone.utc).timestamp()


# --- Eventos crudos por origen ----------------------------------------------
# Cada función devuelve [(t, nombre, dato)], ordenada por t.
# Nombres crudos: I1_on, I1_off, I3_on, vel (dato = velocidad efectiva),
# cilindro_on (dato = color), rampa (dato = color).

def _flancos(estado, clave, valor, t, nombre, eventos, dato=None, iniciar=False):
  """Apunta un evento al pasar `clave` de False a True. Con iniciar=True solo fija el estado."""
  anterior = estado.get(clave)
  estado[clave] = valor
  if iniciar or anterior is None:
    return
  if valor and not anterior:
    eventos.append((t, nombre, dato))
  elif anterior and not valor and nombre.endswith("_on") and nombre.startswith("I"):
    eventos.append((t, nombre[:-3] + "_off", dato))


def eventos_real(mensajes):
  estado, eventos = {}, []
  for m in mensajes:
    p = m.get("payload")
    if not isinstance(p, dict):
      continue
    retenido = m.get("retain", False)
    t = ts_a_segundos(p["ts"]) if "ts" in p else m["t_pc"]
    if m["topic"] == "dt/sld/belt":
      _flancos(estado, "I1", bool(p.get("entry_sensor")), t, "I1_on", eventos, iniciar=retenido)
      _flancos(estado, "I3", bool(p.get("cylinder_sensor")), t, "I3_on", eventos, iniciar=retenido)
      vel = abs(int(p.get("speed", 0)))
      if estado.get("vel") is not None and vel != estado["vel"] and not retenido:
        eventos.append((t, "vel", vel))
      estado["vel"] = vel
    elif m["topic"] == "dt/sld/cylinder":
      color = COLOR_ES.get(p.get("cyl_color"), p.get("cyl_color"))
      _flancos(estado, "cil", bool(p.get("active")), t, "cilindro_on", eventos, dato=color, iniciar=retenido)
      for campo, c in CAMPO_RAMPA_REAL.items():
        _flancos(estado, campo, bool(p.get(campo)), t, "rampa", eventos, dato=c, iniciar=retenido)
  eventos.sort(key=lambda e: e[0])
  return eventos


def eventos_virtual(mensajes):
  estado, eventos = {}, []
  for m in mensajes:
    p = m.get("payload")
    if not isinstance(p, dict):
      continue
    t = m["t_pc"]
    if m["topic"].endswith("/in"):
      _flancos(estado, "I1", bool(p.get("I1")), t, "I1_on", eventos)
      _flancos(estado, "I3", bool(p.get("I3")), t, "I3_on", eventos)
      for borne, c in BARRERA_COLOR.items():
        _flancos(estado, borne, bool(p.get(borne)), t, "rampa", eventos, dato=c)
    elif m["topic"].endswith("/out"):
      m1 = p.get("M1") or {}
      vel = int(m1.get("speed", 0)) if m1.get("running") else 0
      if estado.get("vel") is not None and vel != estado["vel"]:
        eventos.append((t, "vel", vel))
      estado["vel"] = vel
      for borne, c in VALVULA_COLOR.items():
        _flancos(estado, borne, bool(p.get(borne)), t, "cilindro_on", eventos, dato=c)
  eventos.sort(key=lambda e: e[0])
  return eventos


# --- Ciclos ------------------------------------------------------------------

def partir_en_ciclos(eventos):
  """Un ciclo empieza en cada I1_on y acaba antes del siguiente.

  Devuelve una lista de dicts: {"t0", "eventos": {nombre: t_relativo}, "color_cilindro", "color_rampa"}.
  Solo se queda con la primera vez que ocurre cada evento dentro del ciclo.
  """
  ciclos, actual = [], None
  for t, nombre, dato in eventos:
    if nombre == "I1_on":
      nuevo = {"t0": t, "eventos": {"I1_on": 0.0}, "vel1": None, "color_cilindro": None, "color_rampa": None}
      if actual is not None and "I3_on" not in actual["eventos"]:
        if "cinta_on_1" not in actual["eventos"]:
          # La cinta aún no ha arrancado: el I1 anterior fue la mano al colocar la pieza.
          # El ciclo empieza en este I1, que es el que dispara SLD.py (traza real 2026-11-17, blanca 4).
          ciclos[-1] = actual = nuevo
        # Con la cinta ya en marcha y sin llegar a I3, es un rebote de I1: no abre ciclo nuevo.
        continue
      actual = nuevo
      ciclos.append(actual)
      continue
    if actual is None:
      continue
    ev = actual["eventos"]
    rel = round(t - actual["t0"], 4)
    if nombre == "vel":
      if dato > 0 and "cinta_on_1" not in ev and "I3_on" not in ev:
        ev["cinta_on_1"] = rel
        actual["vel1"] = dato
      elif dato > 0 and "I3_on" in ev and "cinta_on_2" not in ev and "cilindro_on" not in ev:
        # Fase 2: cualquier arranque o cambio de velocidad tras I3 (512 -> 350, o 0 -> 350).
        ev["cinta_on_2"] = rel
    elif nombre == "cilindro_on" and "cilindro_on" not in ev:
      ev["cilindro_on"] = rel
      actual["color_cilindro"] = dato
    elif nombre == "rampa" and "rampa" not in ev and "cilindro_on" in ev:
      ev["rampa"] = rel
      actual["color_rampa"] = dato
    elif nombre == "I3_on" and "I3_on" not in ev:
      ev["I3_on"] = rel
  return ciclos


def secuencia_correcta(ciclo):
  """True si están todos los eventos obligatorios y en orden (se admiten empates por el muestreo)."""
  ev = ciclo["eventos"]
  if any(n not in ev for n in SECUENCIA):
    return False
  tiempos = [ev[n] for n in SECUENCIA] + ([ev["rampa"]] if "rampa" in ev else [])
  return all(a <= b for a, b in zip(tiempos, tiempos[1:]))


# --- Vídeo -----------------------------------------------------------------

COLUMNAS_VIDEO = {"I1_on": "f_I1", "cinta_on_1": "f_cinta1", "I3_on": "f_I3",
                  "cinta_on_2": "f_cinta2", "cilindro_on": "f_cilindro", "rampa": "f_rampa"}


def ciclos_video(ruta):
  """Lee video_real.csv: una fila por pieza, fotogramas de cada evento. Celdas vacías = no visto."""
  ciclos = []
  with open(ruta, encoding="utf-8-sig", newline="") as f:
    for fila in csv.DictReader(f, delimiter=";"):
      if not (fila.get("f_I1") or "").strip():
        continue
      fps = float(fila["fps"].replace(",", "."))
      f0 = float(fila["f_I1"])
      ev = {}
      for nombre, col in COLUMNAS_VIDEO.items():
        valor = (fila.get(col) or "").strip()
        if valor:
          ev[nombre] = round((float(valor) - f0) / fps, 4)
      color = (fila.get("color") or "").strip().lower()
      rampa = (fila.get("rampa_correcta") or "").strip().lower()
      # En el vídeo no se ve el color "detectado": se apunta si cayó en su rampa (si/no).
      bien = rampa in ("si", "sí", "s", "1")
      resultado = color if bien else ("otra" if rampa else None)
      ciclos.append({"t0": 0.0, "eventos": ev, "vel1": None, "color": color,
                     "color_cilindro": resultado, "color_rampa": resultado})
  return ciclos


# --- Entrada única -----------------------------------------------------------

def ciclos_de_archivo(ruta):
  """Devuelve (origen, color_esperado, ciclos) de un .jsonl o un .csv de vídeo."""
  ruta = str(ruta)
  if ruta.lower().endswith(".csv"):
    ciclos = ciclos_video(ruta)
    return "video", None, ciclos
  cabecera, mensajes = leer_traza(ruta)
  origen = cabecera.get("origen", "real" if any(m["topic"].startswith("dt/") for m in mensajes) else "virtual")
  eventos = eventos_real(mensajes) if origen == "real" else eventos_virtual(mensajes)
  color = cabecera.get("color")
  return origen, (None if color == "mezcla" else color), partir_en_ciclos(eventos)


def main(argv=None):
  argv = sys.argv[1:] if argv is None else argv
  if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")  # la consola de Windows (cp1252) no sabe escribir algunos símbolos
  if not argv:
    print(__doc__)
    return 1
  for ruta in argv:
    origen, color, ciclos = ciclos_de_archivo(ruta)
    print("\n{}  ({}, color {}): {} ciclos".format(ruta, origen, color or "-", len(ciclos)))
    cols = SECUENCIA + ["rampa"]
    print("  #  " + "  ".join("{:>11}".format(c) for c in cols) + "   cilindro  rampa  secuencia")
    for i, c in enumerate(ciclos, 1):
      ev = c["eventos"]
      celdas = "  ".join("{:>11}".format("{:.3f}".format(ev[n]) if n in ev else "-") for n in cols)
      print("  {:<2} {}   {:<8}  {:<6} {}".format(i, celdas, c["color_cilindro"] or "-", c["color_rampa"] or "-",
                                                  "OK" if secuencia_correcta(c) else "MAL"))
  return 0


if __name__ == "__main__":
  sys.exit(main())
