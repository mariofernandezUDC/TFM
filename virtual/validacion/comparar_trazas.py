"""Compara la planta real con la virtual y dice si cumple los criterios de D011 (validación, V5).

Se le pasan las trazas de cada lado (pueden ser varias: una por color):

  py -3.12 virtual/validacion/comparar_trazas.py \
      --real    virtual/validacion/trazas/real_blanca_*.jsonl  real_roja_*.jsonl  real_azul_*.jsonl \
      --virtual virtual/validacion/trazas/virtual_blanca_*.jsonl virtual_roja_*.jsonl virtual_azul_*.jsonl \
      [--video  virtual/validacion/video_real.csv] \
      [--periodo 0.2] [--tolerancia 0.10] [--salida "02-DOCS/wiki/proyecto/Validación planta real.md"] [--grafica ruta.png]

Criterios (D011, acordados en V0):
  Clasificación  % de piezas que caen en su rampa                              objetivo 100 % en las dos
  Secuencia      % de ciclos con I1 -> cinta 512 -> I3 -> cinta 350 -> cilindro  objetivo 100 %
  Tiempos        por tramo y color, error de la MEDIA virtual frente a la real:
                 cumple si |error| <= tolerancia (10 %)
                 o si |diferencia| <= 2 x periodo de muestreo de la traza real (0,2 s -> 0,4 s)

Si se pasa --video, sus tiempos se comparan también contra la virtual. El vídeo a
60 fps es mucho más fino que el muestreo de 0,2 s de dt/sld, así que sirve de
control de la traza real.

Sale con código 0 si cumple todo y 1 si no.
"""

import argparse
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from a_eventos import SECUENCIA, ciclos_de_archivo, secuencia_correcta  # noqa: E402

COLORES = ["blanca", "roja", "azul"]

# (nombre en la tabla, evento inicial, evento final)
TRAMOS = [
  ("I1 → I3 (cinta 512)", "I1_on", "I3_on"),
  ("I3 → cilindro", "I3_on", "cilindro_on"),
  ("Fase 2 (cinta 350)", "cinta_on_2", "cilindro_on"),
  ("Ciclo I1 → cilindro", "I1_on", "cilindro_on"),
]


# --- Carga -------------------------------------------------------------------

def cargar(rutas):
  """Junta los ciclos de varias trazas. Cada ciclo sale con su color esperado."""
  todos = []
  for ruta in rutas or []:
    origen, color, ciclos = ciclos_de_archivo(ruta)
    for c in ciclos:
      c = dict(c)
      c["esperado"] = c.get("color") or color
      c["archivo"] = Path(ruta).name
      todos.append(c)
  return todos


# --- Cálculos ----------------------------------------------------------------

def media_desv(valores):
  if not valores:
    return None, None
  m = sum(valores) / len(valores)
  if len(valores) < 2:
    return m, 0.0
  return m, math.sqrt(sum((v - m) ** 2 for v in valores) / (len(valores) - 1))


def duraciones(ciclos, color, inicio, fin):
  out = []
  for c in ciclos:
    if color and c.get("esperado") != color:
      continue
    ev = c["eventos"]
    if inicio in ev and fin in ev:
      out.append(ev[fin] - ev[inicio])
  return out


def clasificacion(ciclos):
  """(bien, total, sin_dato). Bien = la rampa (o, si no se vio, el cilindro) es la del color esperado."""
  bien = total = sin_dato = 0
  for c in ciclos:
    if not c.get("esperado"):
      continue
    resultado = c.get("color_rampa") or c.get("color_cilindro")
    if resultado is None:
      sin_dato += 1
      continue
    total += 1
    bien += resultado == c["esperado"]
  return bien, total, sin_dato


def comparar_tramo(real, virtual, periodo, tolerancia):
  """Devuelve dict con medias, error relativo, diferencia y si cumple."""
  mr, dr = media_desv(real)
  mv, dv = media_desv(virtual)
  if mr is None or mv is None:
    return {"real": (mr, dr, len(real)), "virtual": (mv, dv, len(virtual)), "error": None, "dif": None, "cumple": None}
  dif = mv - mr
  error = dif / mr if mr else None
  cumple = (error is not None and abs(error) <= tolerancia) or abs(dif) <= 2 * periodo
  return {"real": (mr, dr, len(real)), "virtual": (mv, dv, len(virtual)), "error": error, "dif": dif, "cumple": cumple}


def pct(a, b):
  return "{:.0f} % ({}/{})".format(100.0 * a / b, a, b) if b else "sin datos"


def fmt_md(m, d, n):
  return "-" if m is None else "{:.3f} ± {:.3f} (n={})".format(m, d, n)


def informe(real, virtual, video, periodo, tolerancia):
  """Devuelve (texto Markdown, cumple_todo, filas para la gráfica)."""
  lineas, cumple_todo, filas = [], True, []
  lineas.append("## Resultado de la comparación\n")
  lineas.append("Ciclos: real {}, virtual {}{}.  Periodo de muestreo real: {} s. Tolerancia: {:.0f} % "
                "o ≤ {:.2f} s.\n".format(len(real), len(virtual), ", vídeo {}".format(len(video)) if video else "",
                                         periodo, tolerancia * 100, 2 * periodo))

  # Clasificación y secuencia
  lineas.append("### Clasificación y secuencia\n")
  lineas.append("| Origen | Clasificación correcta | Sin dato de rampa | Secuencia correcta | Rampa vista en la traza |")
  lineas.append("|---|---|---|---|---|")
  for nombre, ciclos in (("Real", real), ("Virtual", virtual), ("Vídeo", video)):
    if not ciclos:
      continue
    b, t, s = clasificacion(ciclos)
    seq = sum(secuencia_correcta(c) for c in ciclos) if nombre != "Vídeo" else None
    rampa = sum("rampa" in c["eventos"] for c in ciclos)
    lineas.append("| {} | {} | {} | {} | {} |".format(nombre, pct(b, t), s,
                                                    pct(seq, len(ciclos)) if seq is not None else "-",
                                                    pct(rampa, len(ciclos))))
    if t and b != t:
      cumple_todo = False
    if seq is not None and seq != len(ciclos):
      cumple_todo = False
  lineas.append("")

  # Tiempos
  for ref_nombre, ref in (("real (dt/sld)", real), ("vídeo", video)):
    if not ref:
      continue
    lineas.append("### Tiempos: virtual frente a {} (segundos, media ± desviación)\n".format(ref_nombre))
    lineas.append("| Color | Tramo | {} | Virtual | Diferencia | Error | Cumple |".format(ref_nombre.capitalize()))
    lineas.append("|---|---|---|---|---|---|---|")
    p = periodo if ref is real else 0.0
    for color in COLORES:
      for tramo, ini, fin in TRAMOS:
        r = comparar_tramo(duraciones(ref, color, ini, fin), duraciones(virtual, color, ini, fin), p, tolerancia)
        if r["real"][2] == 0 and r["virtual"][2] == 0:
          continue
        if r["cumple"] is False:
          cumple_todo = False
        lineas.append("| {} | {} | {} | {} | {} | {} | {} |".format(
          color, tramo, fmt_md(*r["real"]), fmt_md(*r["virtual"]),
          "-" if r["dif"] is None else "{:+.3f}".format(r["dif"]),
          "-" if r["error"] is None else "{:+.1f} %".format(100 * r["error"]),
          {True: "✅", False: "❌", None: "sin datos"}[r["cumple"]]))
        if ref is real:
          filas.append((color, tramo, r["real"][0], r["virtual"][0]))
    lineas.append("")

  lineas.append("**Veredicto: {}**".format("CUMPLE" if cumple_todo else "NO CUMPLE"))
  return "\n".join(lineas) + "\n", cumple_todo, filas


def detalle(ciclos, titulo):
  """Tabla pieza a pieza, para el anexo."""
  cols = SECUENCIA + ["rampa"]
  out = ["### Detalle por pieza · {}\n".format(titulo),
         "| # | Archivo | Esperado | " + " | ".join(cols) + " | Rampa | Secuencia |",
         "|" + "---|" * (len(cols) + 5)]
  for i, c in enumerate(ciclos, 1):
    ev = c["eventos"]
    out.append("| {} | {} | {} | {} | {} | {} |".format(
      i, c.get("archivo", ""), c.get("esperado") or "-",
      " | ".join("{:.3f}".format(ev[n]) if n in ev else "-" for n in cols),
      c.get("color_rampa") or c.get("color_cilindro") or "-",
      "OK" if secuencia_correcta(c) else "MAL"))
  return "\n".join(out) + "\n"


def grafica(filas, ruta):
  try:
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
  except ImportError:
    print("matplotlib no está instalado: no hago la gráfica (py -3.12 -m pip install matplotlib).")
    return False
  filas = [f for f in filas if f[2] is not None and f[3] is not None]
  if not filas:
    return False
  etiquetas = ["{}\n{}".format(c, t) for c, t, _, _ in filas]
  x = range(len(filas))
  fig, ax = plt.subplots(figsize=(max(8, len(filas) * 0.9), 4.5))
  ax.bar([i - 0.2 for i in x], [f[2] for f in filas], width=0.4, label="Real", color="#2a9d8f")
  ax.bar([i + 0.2 for i in x], [f[3] for f in filas], width=0.4, label="Virtual", color="#e9c46a")
  ax.set_xticks(list(x))
  ax.set_xticklabels(etiquetas, fontsize=7)
  ax.set_ylabel("segundos")
  ax.set_title("SLD: tiempos por tramo, planta real frente a virtual")
  ax.legend()
  ax.spines[["top", "right"]].set_visible(False)
  fig.tight_layout()
  Path(ruta).parent.mkdir(parents=True, exist_ok=True)
  fig.savefig(ruta, dpi=150)
  return True


def main(argv=None):
  p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
  p.add_argument("--real", nargs="+", default=[])
  p.add_argument("--virtual", nargs="+", required=True)
  p.add_argument("--video")
  p.add_argument("--periodo", type=float, default=0.2, help="periodo de muestreo de dt/sld en la planta real (s)")
  p.add_argument("--tolerancia", type=float, default=0.10)
  p.add_argument("--salida", help="archivo Markdown donde escribir el informe")
  p.add_argument("--grafica", help="PNG con la comparación de tiempos (necesita matplotlib)")
  a = p.parse_args(argv)

  real, virtual = cargar(a.real), cargar(a.virtual)
  video = cargar([a.video]) if a.video else []
  texto, ok, filas = informe(real, virtual, video, a.periodo, a.tolerancia)
  anexo = detalle(real, "real") + "\n" + detalle(virtual, "virtual")
  print(texto)
  if a.salida:
    Path(a.salida).parent.mkdir(parents=True, exist_ok=True)
    Path(a.salida).write_text(texto + "\n" + anexo, encoding="utf-8")
    print("Informe guardado en", a.salida)
  if a.grafica and grafica(filas, a.grafica):
    print("Gráfica guardada en", a.grafica)
  return 0 if ok else 1


if __name__ == "__main__":
  sys.exit(main())
