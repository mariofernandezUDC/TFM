"""Prueba de los ejercicios por niveles (virtual/alumno/ejercicios/).

- Cada solución hace en la maqueta falsa lo que pide su enunciado.
- Cada plantilla sin rellenar se ejecuta sin errores y avisa de que falta código.
Necesita Mosquitto en marcha (si no, se salta).

Uso:  py -3.12 -m unittest discover -s virtual/tests -v
"""

import time
import unittest

from test_programa_alumno import VIRTUAL, BaseLanzador  # prepara sys.path con virtual/
from maqueta_manual import I2_BLANCO, I2_ROJO  # noqa: E402
from test_hito1 import BROKER, PUERTO, broker_disponible, esperar, m1  # noqa: E402

EJERCICIOS = VIRTUAL / "alumno" / "ejercicios"
SOLUCIONES = EJERCICIOS / "soluciones"
PLANTILLAS = ["nivel1_arrancar.py", "nivel2_parar.py", "nivel3_color.py", "nivel4_expulsar.py"]


@unittest.skipUnless(broker_disponible(), "Mosquitto no responde en {}:{}".format(BROKER, PUERTO))
class TestEjercicios(BaseLanzador):

  def pieza_hasta_i3(self, valor_i2=None):
    """Pieza en I1 (la cinta tiene que arrancar a 512), pasa por I2 y llega a I3."""
    self.maqueta.poner("I1", True)
    i, _, _ = self.buscar(lambda img: m1(img).get("speed") == 512 and m1(img).get("running") is True)
    self.maqueta.poner("I1", False)
    if valor_i2 is not None:
      self.maqueta.poner("I2", valor_i2)
      time.sleep(0.1)
      self.maqueta.poner("I2", 2500)
    self.maqueta.poner("I3", True)
    return i

  def test_nivel1_arranca(self):
    self.lanzar(SOLUCIONES / "nivel1.py")
    self.pieza_hasta_i3()
    time.sleep(0.3)
    self.assertTrue(m1(self.maqueta.estado_out()[0]).get("running"), "en el nivel 1 la cinta no se para")

  def test_nivel2_para_en_i3(self):
    self.lanzar(SOLUCIONES / "nivel2.py")
    i = self.pieza_hasta_i3()
    self.buscar(lambda img: m1(img).get("running") is False, i)
    self.assertTrue(esperar(lambda: "Quita la pieza" in self.texto(), 1.0))

  def test_nivel3_escribe_el_color(self):
    self.lanzar(SOLUCIONES / "nivel3.py")
    i = self.pieza_hasta_i3(I2_ROJO)
    self.buscar(lambda img: m1(img).get("running") is False, i)
    self.assertTrue(esperar(lambda: "La pieza es roja" in self.texto(), 1.0), "no escribió el color")

  def test_nivel4_expulsa_la_blanca(self):
    self.lanzar(SOLUCIONES / "nivel4.py")
    i = self.pieza_hasta_i3(I2_BLANCO)
    i, _, _ = self.buscar(lambda img: m1(img).get("speed") == 350 and m1(img).get("running") is True
                          and img.get("O8") is True, i)
    for _ in range(6):
      self.maqueta.conmutar("C1")
      time.sleep(0.05)
    i, _, _ = self.buscar(lambda img: m1(img).get("running") is False, i)
    i, t_on, _ = self.buscar(lambda img: img.get("O5") is True, i)
    self.maqueta.poner("I3", False)
    self.maqueta.poner("I6", True)
    _, t_off, _ = self.buscar(lambda img: img.get("O5") is False, i)
    self.assertTrue(0.4 <= t_off - t_on <= 0.7)
    self.assertTrue(esperar(lambda: "Pieza blanca en su rampa" in self.texto(), 2.0))

  def test_plantillas_sin_rellenar_no_fallan(self):
    for nombre in PLANTILLAS:
      with self.subTest(plantilla=nombre):
        self.lanzar(EJERCICIOS / nombre)
        self.maqueta.poner("I1", True)
        time.sleep(0.2)
        self.maqueta.poner("I1", False)
        self.maqueta.poner("I2", I2_BLANCO)
        time.sleep(0.1)
        self.maqueta.poner("I3", True)
        self.assertTrue(esperar(lambda: "Falta tu código" in self.texto(), 3.0),
                        "{}: no avisó de que falta código".format(nombre))
        time.sleep(0.3)
        self.assertIsNone(self.proceso.poll(), "{}: el programa terminó".format(nombre))
        self.assertNotIn("ERROR", self.texto())
        self.tearDown()


if __name__ == "__main__":
  unittest.main(verbosity=2)
