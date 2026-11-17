"""Validación real vs virtual (virtual/validacion/): conversión a eventos y comparación, sin broker.

Las trazas de prueba imitan las de verdad:
- real: dt/sld/belt y dt/sld/cylinder, con "ts" del TXT y muestreo de 0,2 s;
  la velocidad pasa de 512 a 350 sin pasar por 0 (la parada no se ve).
- virtual: vio/txt5/in a 100 Hz y vio/txt5/out con M1 = {speed, running}.

Uso:  py -3.12 -m unittest discover -s virtual/tests -v
"""

import json
import sys
import tempfile
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "validacion"))

import a_eventos as ae          # noqa: E402
import comparar_trazas as ct    # noqa: E402
import grabar_trazas as gt      # noqa: E402

T0 = datetime(2026, 11, 17, 15, 0, 0, tzinfo=timezone.utc)

# Tiempos (s) de un ciclo por color: I1, cinta 512, I3, cinta 350, cilindro, rampa
CICLO = {
  "blanca": (0.0, 0.3, 3.0, 3.01, 3.9, 4.6),
  "roja":   (0.0, 0.3, 3.0, 3.01, 5.2, 5.9),
  "azul":   (0.0, 0.3, 3.0, 3.01, 6.6, 7.3),
}
COLOR_EN = {"blanca": "WHITE", "roja": "RED", "azul": "BLUE"}
RAMPA_REAL = {"blanca": "is_white", "roja": "is_red", "azul": "is_blue"}
VALVULA = {"blanca": "O5", "roja": "O6", "azul": "O7"}
BARRERA = {"blanca": "I6", "roja": "I7", "azul": "I8"}


def ts(seg):
  return (T0 + timedelta(seconds=seg)).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def cuantizar(t, periodo=0.2):
  """Hora a la que el hilo de 0,2 s de Digital_Twin.py publicaría un cambio ocurrido en t."""
  import math
  return math.ceil(round(t / periodo, 6)) * periodo


def traza_real(color, piezas=2, separacion=12.0, escala=1.0):
  m = []
  # Retenidos al suscribirse: estado viejo, no deben dar eventos.
  m.append({"t_pc": 0, "topic": "dt/sld/belt", "retain": True,
            "payload": {"ts": ts(-100), "entry_sensor": False, "cylinder_sensor": False, "speed": 0}})
  m.append({"t_pc": 0, "topic": "dt/sld/cylinder", "retain": True,
            "payload": {"ts": ts(-100), "cyl_color": None, "active": False,
                        "is_white": False, "is_red": False, "is_blue": False}})
  for k in range(piezas):
    base = k * separacion
    i1, c1, i3, c2, cil, rampa = (base + x * escala for x in CICLO[color])

    def belt(t, e, c, v):
      m.append({"t_pc": t + 0.05, "topic": "dt/sld/belt", "retain": False,
                "payload": {"ts": ts(cuantizar(t)), "entry_sensor": e, "cylinder_sensor": c, "speed": v}})

    def cyl(t, act, r):
      m.append({"t_pc": t + 0.05, "topic": "dt/sld/cylinder", "retain": False,
                "payload": {"ts": ts(cuantizar(t)), "cyl_color": COLOR_EN[color], "active": act,
                            "is_white": r == "is_white", "is_red": r == "is_red", "is_blue": r == "is_blue"}})
    belt(i1, True, False, 0)
    belt(c1, True, False, 512)
    belt(c1 + 0.6, False, False, 512)
    belt(i3, False, True, 512)
    belt(c2 + 0.2, False, False, 350)      # 512 -> 350 directo, y la pieza ya ha dejado I3
    belt(cil, False, False, 0)
    cyl(cil, True, None)
    cyl(cil + 0.5, False, None)
    cyl(rampa, False, RAMPA_REAL[color])
    cyl(rampa + 0.6, False, None)
  m.sort(key=lambda x: ae.ts_a_segundos(x["payload"]["ts"]))
  return m


def traza_virtual(color, piezas=2, separacion=12.0, escala=1.0, valvula=None):
  m = []
  for k in range(piezas):
    base = k * separacion
    i1, c1, i3, c2, cil, rampa = (base + x * escala for x in CICLO[color])
    fin = base + separacion - 0.5
    v = valvula or VALVULA[color]
    cae = {"O5": "blanca", "O6": "roja", "O7": "azul"}[v]   # la pieza cae donde la empuja la válvula
    t = base - 0.5   # el grabador ya está escuchando antes de meter la pieza
    while t < fin:
      m.append({"t_pc": t, "topic": "vio/txt5/in", "retain": False, "payload": {
        "I1": i1 <= t < c1 + 0.6, "I2": 1000, "I3": i3 <= t < c2 + 0.3,
        "I6": cae == "blanca" and rampa <= t < rampa + 0.5,
        "I7": cae == "roja" and rampa <= t < rampa + 0.5,
        "I8": cae == "azul" and rampa <= t < rampa + 0.5, "C1": False}})
      t = round(t + 0.01, 4)

    def out(t, speed, running, abierta):
      p = {"M1": {"speed": speed, "dir": "CW", "running": running}, "O5": False, "O6": False, "O7": False, "O8": False}
      if abierta:
        p[v] = True
      m.append({"t_pc": t, "topic": "vio/txt5/out", "retain": False, "payload": p})
    out(base + 0.001, 0, False, False)
    out(c1, 512, True, False)
    out(i3 + 0.002, 0, True, False)       # setConvBeltSpeedSLD(0): sigue running pero a 0
    out(c2, 350, True, False)
    out(cil, 350, False, True)            # stop() y válvula
    out(cil + 0.5, 350, False, False)
  m.sort(key=lambda x: x["t_pc"])
  return m


def escribir(tmp, nombre, origen, color, mensajes):
  ruta = Path(tmp) / nombre
  with open(ruta, "w", encoding="utf-8") as f:
    f.write(json.dumps({"cabecera": {"origen": origen, "color": color}}) + "\n")
    for x in mensajes:
      f.write(json.dumps(x) + "\n")
  return str(ruta)


class TestEventos(unittest.TestCase):

  def test_ts_del_txt(self):
    self.assertAlmostEqual(ae.ts_a_segundos("2026-11-17T15:00:01.250Z") - T0.timestamp(), 1.25, places=3)

  def test_real_un_ciclo_por_pieza_y_secuencia(self):
    ciclos = ae.partir_en_ciclos(ae.eventos_real(traza_real("roja", piezas=3)))
    self.assertEqual(len(ciclos), 3)
    for c in ciclos:
      self.assertTrue(ae.secuencia_correcta(c), c)
      self.assertEqual(c["color_cilindro"], "roja")
      self.assertEqual(c["color_rampa"], "roja")
      self.assertAlmostEqual(c["eventos"]["I3_on"], 3.0, delta=0.21)

  def test_mano_tapa_i1_antes_de_la_pieza(self):
    """La mano tapa y destapa I1 al colocar la pieza: el ciclo empieza en el I1 de la pieza, no en el de la mano."""
    m = traza_real("blanca", piezas=1)
    for t, e in ((-2.4, True), (-2.2, False)):
      m.append({"t_pc": t + 0.05, "topic": "dt/sld/belt", "retain": False,
                "payload": {"ts": ts(t), "entry_sensor": e, "cylinder_sensor": False, "speed": 0}})
    m.sort(key=lambda x: ae.ts_a_segundos(x["payload"]["ts"]))
    ciclos = ae.partir_en_ciclos(ae.eventos_real(m))
    self.assertEqual(len(ciclos), 1)
    self.assertTrue(ae.secuencia_correcta(ciclos[0]), ciclos[0])
    self.assertAlmostEqual(ciclos[0]["eventos"]["cinta_on_1"], 0.4, delta=0.21)

  def test_retenidos_no_dan_eventos(self):
    ev = ae.eventos_real(traza_real("blanca", piezas=1))
    self.assertTrue(all(t > T0.timestamp() - 1 for t, _, _ in ev))

  def test_virtual_detecta_fase_2_tras_parada(self):
    ciclos = ae.partir_en_ciclos(ae.eventos_virtual(traza_virtual("azul", piezas=2)))
    self.assertEqual(len(ciclos), 2)
    for c in ciclos:
      self.assertTrue(ae.secuencia_correcta(c), c)
      self.assertAlmostEqual(c["eventos"]["cinta_on_2"], 3.01, places=2)
      self.assertEqual(c["color_rampa"], "azul")

  def test_falta_un_evento_es_secuencia_mal(self):
    c = {"eventos": {"I1_on": 0, "cinta_on_1": 0.3, "I3_on": 3.0, "cilindro_on": 4.0}}
    self.assertFalse(ae.secuencia_correcta(c))

  def test_orden_cambiado_es_secuencia_mal(self):
    c = {"eventos": {"I1_on": 0, "cinta_on_1": 0.3, "I3_on": 3.0, "cinta_on_2": 2.0, "cilindro_on": 4.0}}
    self.assertFalse(ae.secuencia_correcta(c))

  def test_video_csv(self):
    with tempfile.TemporaryDirectory() as tmp:
      ruta = Path(tmp) / "video.csv"
      ruta.write_text("pieza;color;fps;f_I1;f_cinta1;f_I3;f_cinta2;f_cilindro;f_rampa;rampa_correcta;notas\n"
                      "1;roja;60;100;118;280;281;412;454;si;\n2;azul;60;;;;;;;;\n", encoding="utf-8")
      origen, _, ciclos = ae.ciclos_de_archivo(ruta)
    self.assertEqual(origen, "video")
    self.assertEqual(len(ciclos), 1)
    self.assertAlmostEqual(ciclos[0]["eventos"]["I3_on"], 3.0, places=3)
    self.assertEqual(ciclos[0]["color_rampa"], "roja")

  def test_linea_del_grabador(self):
    linea = json.loads(gt.linea_mensaje(1.5, "dt/sld/belt", 0, b'{"speed": 512}'))
    self.assertEqual(linea, {"t_pc": 1.5, "topic": "dt/sld/belt", "retain": False, "payload": {"speed": 512}})
    self.assertEqual(json.loads(gt.linea_mensaje(1.5, "x", 1, b"hola"))["payload"], "hola")


class TestComparar(unittest.TestCase):

  def _rutas(self, tmp, escala_virtual=1.0, valvula_mal=None):
    reales, virtuales = [], []
    for color in ("blanca", "roja", "azul"):
      reales.append(escribir(tmp, "real_" + color + ".jsonl", "real", color, traza_real(color, piezas=5)))
      v = valvula_mal if color == "roja" else None
      virtuales.append(escribir(tmp, "virtual_" + color + ".jsonl", "virtual", color,
                                traza_virtual(color, piezas=5, escala=escala_virtual, valvula=v)))
    return reales, virtuales

  def test_iguales_cumple(self):
    with tempfile.TemporaryDirectory() as tmp:
      r, v = self._rutas(tmp)
      salida = Path(tmp) / "informe.md"
      codigo = ct.main(["--real"] + r + ["--virtual"] + v + ["--salida", str(salida)])
      texto = salida.read_text(encoding="utf-8")
    self.assertEqual(codigo, 0, texto)
    self.assertIn("CUMPLE", texto)
    self.assertIn("100 % (15/15)", texto)

  def test_virtual_un_30_por_ciento_lenta_no_cumple(self):
    with tempfile.TemporaryDirectory() as tmp:
      r, v = self._rutas(tmp, escala_virtual=1.3)
      texto, ok, _ = ct.informe(ct.cargar(r), ct.cargar(v), [], 0.2, 0.10)
    self.assertFalse(ok)
    self.assertIn("❌", texto)

  def test_tramo_corto_cumple_por_periodo(self):
    # 0,9 s frente a 1,2 s: error 33 %, pero la diferencia (0,3 s) es menor que 2 x 0,2 s.
    r = ct.comparar_tramo([0.9, 0.9], [1.2, 1.2], 0.2, 0.10)
    self.assertTrue(r["cumple"])
    r = ct.comparar_tramo([0.9, 0.9], [1.4, 1.4], 0.2, 0.10)
    self.assertFalse(r["cumple"])

  def test_pieza_en_rampa_equivocada(self):
    with tempfile.TemporaryDirectory() as tmp:
      r, v = self._rutas(tmp, valvula_mal="O5")   # la roja virtual la empuja el cilindro blanco
      _, ok, _ = ct.informe(ct.cargar(r), ct.cargar(v), [], 0.2, 0.10)
      b, t, _ = ct.clasificacion(ct.cargar([v[1]]))
    self.assertFalse(ok)
    self.assertEqual((b, t), (0, 5))


if __name__ == "__main__":
  unittest.main()
