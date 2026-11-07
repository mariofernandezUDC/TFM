"""Prueba de «cargar código»: run_virtual.py --programa con programas de alumno.

Lanza run_virtual.py como proceso aparte y lo maneja con MaquetaVirtual, igual
que test_hito1.py. Comprueba:
- que el ejemplo resuelto clasifica una pieza blanca;
- que un programa que falla a mitad (en el hilo principal o en uno suyo) para
  la cinta, deja de enviar `out` y termina en menos de 1 s, con el error claro.
Necesita Mosquitto en marcha (si no, se salta).

Uso:  py -3.12 -m unittest discover -s virtual/tests -v
"""

import os
import subprocess
import sys
import threading
import time
import unittest
from pathlib import Path

VIRTUAL = Path(__file__).resolve().parent.parent
TESTS = Path(__file__).resolve().parent
sys.path.insert(0, str(VIRTUAL))

from maqueta_manual import I2_BLANCO, MaquetaVirtual  # noqa: E402
from test_hito1 import BROKER, PUERTO, broker_disponible, esperar, m1  # noqa: E402

EJEMPLO = VIRTUAL / "alumno" / "ejemplos" / "clasificar_por_color.py"
PLANTILLA = VIRTUAL / "alumno" / "plantilla_sld.py"


class BaseLanzador(unittest.TestCase):
  """Lanza run_virtual.py --programa contra la maqueta falsa. Sin tests propios: la usan otros archivos."""

  def lanzar(self, programa):
    self.maqueta = MaquetaVirtual(BROKER, PUERTO)
    self.maqueta.arrancar()
    entorno = dict(os.environ, PYTHONIOENCODING="utf-8", PYTHONUNBUFFERED="1")
    self.proceso = subprocess.Popen(
      [sys.executable, str(VIRTUAL / "run_virtual.py"), "--programa", str(programa),
       "--broker", BROKER, "--puerto", str(PUERTO)],
      stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=entorno, encoding="utf-8")
    self.consola = []
    self._lock = threading.Lock()
    threading.Thread(target=self._leer_consola, daemon=True).start()
    # Primer out: el lazo está sano.
    self.buscar(lambda img: "M1" in img, timeout=5)

  def tearDown(self):
    if getattr(self, "proceso", None) is None:
      return   # ya cerrado (o nunca lanzado)
    if self.proceso.poll() is None:
      self.proceso.terminate()
    self.proceso.wait(timeout=5)
    self.proceso.stdout.close()
    self.maqueta.parar()
    if self._outcome.result.failures or self._outcome.result.errors:
      print("\n--- consola de run_virtual.py ---\n" + "".join(self.consola))
    self.proceso = None

  def _leer_consola(self):
    for linea in self.proceso.stdout:
      with self._lock:
        self.consola.append(linea)

  def texto(self):
    with self._lock:
      return "".join(self.consola)

  def buscar(self, condicion, desde=0, timeout=3.0):
    def mirar():
      for i, (_, t, imagen) in enumerate(self.maqueta.historial()):
        if i >= desde and condicion(imagen):
          return i, t, imagen
      return None
    encontrado = esperar(mirar, timeout)
    self.assertIsNotNone(encontrado, "no llegó en {} s el out esperado".format(timeout))
    return encontrado


@unittest.skipUnless(broker_disponible(), "Mosquitto no responde en {}:{}".format(BROKER, PUERTO))
class TestProgramaAlumno(BaseLanzador):

  # --- El ejemplo resuelto ---

  def test_ejemplo_clasifica_una_blanca(self):
    self.lanzar(EJEMPLO)
    self.maqueta.poner("I1", True)
    self.buscar(lambda img: m1(img).get("speed") == 512 and m1(img).get("running") is True)
    self.maqueta.poner("I1", False)
    self.maqueta.poner("I2", I2_BLANCO)
    time.sleep(0.1)
    self.maqueta.poner("I2", 2500)       # la pieza ya ha pasado bajo el sensor
    inicio = len(self.maqueta.historial())
    self.maqueta.poner("I3", True)
    i, _, _ = self.buscar(lambda img: m1(img).get("speed") == 350 and m1(img).get("running") is True
                          and img.get("O8") is True, inicio)
    for _ in range(6):
      self.maqueta.conmutar("C1")
      time.sleep(0.05)
    i, _, _ = self.buscar(lambda img: m1(img).get("running") is False, i)
    i, t_on, _ = self.buscar(lambda img: img.get("O5") is True, i)
    self.maqueta.poner("I3", False)
    self.maqueta.poner("I6", True)       # la pieza cae en la rampa blanca
    i, t_off, _ = self.buscar(lambda img: img.get("O5") is False and img.get("O8") is False, i)
    print("\n  ejemplo: O5 encendida {:.3f} s".format(t_off - t_on))
    self.assertTrue(0.4 <= t_off - t_on <= 0.7)
    self.assertTrue(esperar(lambda: "Pieza blanca en su rampa" in self.texto(), 2.0),
                    "el ejemplo no confirmó la pieza en la rampa blanca")
    self.assertIn("Pieza blanca (voltaje {})".format(I2_BLANCO), self.texto())

  def test_plantilla_arranca_y_para(self):
    self.lanzar(PLANTILLA)
    self.maqueta.poner("I1", True)
    i, _, _ = self.buscar(lambda img: m1(img).get("speed") == 512 and m1(img).get("running") is True)
    self.maqueta.poner("I1", False)
    self.maqueta.poner("I3", True)
    self.buscar(lambda img: m1(img).get("running") is False, i)

  # --- Errores del alumno ---

  def comprobar_parada_por_error(self, programa, texto_error):
    self.lanzar(programa)
    self.maqueta.poner("I1", True)
    i, _, _ = self.buscar(lambda img: m1(img).get("running") is True)
    self.maqueta.poner("I1", False)
    t0 = time.monotonic()
    self.maqueta.poner("I3", True)       # aquí falla el programa

    # La cinta se para con un último out (sin esperar a los 0,5 s del latido).
    _, t_parada, img = self.buscar(lambda img: m1(img).get("running") is False, i, timeout=1.0)
    self.assertFalse(any(img.get(k) for k in ("O5", "O6", "O7", "O8")), img)
    # El proceso termina con código 1.
    self.assertTrue(esperar(lambda: self.proceso.poll() is not None, 1.0), "el proceso no terminó")
    t_fin = time.monotonic()
    self.assertEqual(self.proceso.returncode, 1)
    # Y ya no llega ningún out: Unity pasa a estado seguro a los 0,5 s (D006.5).
    _, t_ultima = self.maqueta.estado_out()
    time.sleep(0.7)
    self.assertEqual(self.maqueta.estado_out()[1], t_ultima, "siguen llegando out tras el error")
    print("\n  {}: cinta parada a {:.0f} ms del fallo, proceso terminado a {:.0f} ms, "
          "último out a {:.0f} ms".format(programa.name, (t_parada - t0) * 1000, (t_fin - t0) * 1000,
                                         (t_ultima - t0) * 1000))
    self.assertLess(t_ultima - t0, 1.0)
    time.sleep(0.2)                      # deja leer las últimas líneas de la consola
    self.assertIn(texto_error, self.texto())
    self.assertIn("La planta se ha parado", self.texto())
    print("  consola:\n    " + "\n    ".join(
      l for l in self.texto().splitlines() if l and not l[0].isdigit()))

  def test_error_en_el_programa(self):
    self.comprobar_parada_por_error(TESTS / "programas" / "falla_a_mitad.py",
                                    "ERROR en tu programa: falla_a_mitad.py, línea 16")

  def test_error_en_un_hilo_del_programa(self):
    self.comprobar_parada_por_error(TESTS / "programas" / "falla_en_hilo.py",
                                    "ERROR en tu programa (en el hilo vigilar_i3): falla_en_hilo.py, línea 13")


if __name__ == "__main__":
  unittest.main(verbosity=2)
