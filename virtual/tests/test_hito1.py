"""Prueba del hito 1 (F0.4): SLD.py original + librería virtual + maqueta falsa.

Lanza run_sld_virtual.py como proceso aparte y lo maneja con MaquetaVirtual,
la misma clase que usa maqueta_manual.py. Comprueba los criterios a) a d)
y el latido de D006.5. Necesita Mosquitto en marcha (si no, se salta).

Uso:  py -3.12 -m unittest discover -s virtual/tests -v
"""

import os
import socket
import subprocess
import sys
import threading
import time
import unittest
from pathlib import Path

VIRTUAL = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(VIRTUAL))

from maqueta_manual import I2_BLANCO, MaquetaVirtual  # noqa: E402

BROKER = os.environ.get("VIO_BROKER", "localhost")
PUERTO = int(os.environ.get("VIO_PUERTO", "1883"))


def broker_disponible():
  try:
    with socket.create_connection((BROKER, PUERTO), timeout=1):
      return True
  except OSError:
    return False


def esperar(condicion, timeout, paso=0.01):
  limite = time.monotonic() + timeout
  while time.monotonic() < limite:
    resultado = condicion()
    if resultado:
      return resultado
    time.sleep(paso)
  return None


def m1(imagen):
  return imagen.get("M1", {})


@unittest.skipUnless(broker_disponible(), "Mosquitto no responde en {}:{}".format(BROKER, PUERTO))
class TestHito1(unittest.TestCase):

  def setUp(self):
    self.maqueta = MaquetaVirtual(BROKER, PUERTO)
    self.maqueta.arrancar()
    entorno = dict(os.environ, PYTHONIOENCODING="utf-8", PYTHONUNBUFFERED="1")
    self.proceso = subprocess.Popen(
      [sys.executable, str(VIRTUAL / "run_sld_virtual.py"), "--broker", BROKER, "--puerto", str(PUERTO)],
      stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=entorno, encoding="utf-8")
    self.consola = []
    self._lock = threading.Lock()
    threading.Thread(target=self._leer_consola, daemon=True).start()

  def tearDown(self):
    self.proceso.terminate()
    self.proceso.wait(timeout=5)
    self.proceso.stdout.close()
    self.maqueta.parar()
    if self._outcome.result.failures or self._outcome.result.errors:
      print("\n--- consola de run_sld_virtual.py ---\n" + "".join(self.consola))

  def _leer_consola(self):
    for linea in self.proceso.stdout:
      with self._lock:
        self.consola.append(linea)

  def lineas(self, texto):
    with self._lock:
      return [l for l in self.consola if texto in l]

  def eventos(self, desde=0):
    """Historial de out (imágenes distintas a la anterior) a partir de un índice."""
    return self.maqueta.historial()[desde:]

  def buscar(self, condicion, desde=0, timeout=3.0):
    """Espera a que aparezca en out una imagen que cumpla la condición. Devuelve (índice, t, imagen)."""
    def mirar():
      for i, (_, t, imagen) in enumerate(self.maqueta.historial()):
        if i >= desde and condicion(imagen):
          return i, t, imagen
      return None
    encontrado = esperar(mirar, timeout)
    self.assertIsNotNone(encontrado, "no llegó en {} s el out esperado".format(timeout))
    return encontrado

  def ver(self, etiqueta, desde, hasta=None):
    print("\n  {}".format(etiqueta))
    for hora, _, imagen in self.maqueta.historial()[desde:hasta]:
      print("    {}  {}".format(hora, imagen))

  def test_criterios_a_b_c_d(self):
    # Arranque: con entradas frescas llega el primer out y el latido va a ~10 msg/s (D006.5).
    self.buscar(lambda img: m1(img).get("running") is False, timeout=5)
    time.sleep(1.2)
    latidos = self.maqueta.mensajes_out_por_segundo()
    print("\n  latido en reposo: {} mensajes de out en el último segundo".format(latidos))
    self.assertTrue(8 <= latidos <= 12, "latido fuera de 100 ms: {} msg/s".format(latidos))
    hz = self.maqueta.hz_in()
    print("  vio/txt5/in a {} Hz reales".format(hz))
    self.assertTrue(hz >= 90, "la maqueta no llega a 100 Hz (D006.2): {} Hz".format(hz))

    # a) I1 oscuro -> M1 speed 512, running true
    inicio_a = len(self.maqueta.historial())
    self.maqueta.poner("I1", True)
    self.buscar(lambda img: m1(img).get("speed") == 512 and m1(img).get("running") is True, inicio_a)
    self.maqueta.poner("I1", False)        # la pieza ya ha pasado la barrera de entrada
    self.ver("a) I1 oscuro:", inicio_a)

    # b) I2 en blanco ANTES de I3 (el color se lee entre I1 e I3). Después I3 oscuro:
    #    M1 speed 0 con running true (setConvBeltSpeedSLD(0) hace set_speed(0) + start()),
    #    O8 true y luego M1 speed 350 running true.
    self.maqueta.poner("I2", I2_BLANCO)
    time.sleep(0.1)
    inicio_b = len(self.maqueta.historial())
    self.maqueta.poner("I3", True)
    i_parada, _, _ = self.buscar(lambda img: m1(img).get("speed") == 0 and m1(img).get("running") is True, inicio_b)
    i_o8, _, _ = self.buscar(lambda img: img.get("O8") is True, i_parada)
    i_350, _, _ = self.buscar(lambda img: m1(img).get("speed") == 350 and m1(img).get("running") is True
                              and img.get("O8") is True, i_o8)
    self.ver("b) I3 oscuro:", inicio_b)

    # c) 6 cambios de C1 -> M1 se para, O5 true ~0,5 s y luego false, O8 false
    inicio_c = len(self.maqueta.historial())
    for _ in range(6):
      self.maqueta.conmutar("C1")
      time.sleep(0.05)                     # 50 ms entre cambios: 5 imágenes de in en cada estado
    i_stop, _, _ = self.buscar(lambda img: m1(img).get("running") is False, inicio_c)
    i_o5_on, t_o5_on, _ = self.buscar(lambda img: img.get("O5") is True, i_stop)
    i_o5_off, t_o5_off, _ = self.buscar(lambda img: img.get("O5") is False, i_o5_on)
    # ejectWhite hace O5.off() y después O8.off(): dos cambios, dos mensajes seguidos.
    self.buscar(lambda img: img.get("O5") is False and img.get("O8") is False, i_o5_off)
    self.ver("c) 6 cambios de C1:", inicio_c)
    print("    O5 encendida {:.3f} s".format(t_o5_off - t_o5_on))
    self.assertTrue(0.4 <= t_o5_off - t_o5_on <= 0.7)
    self.maqueta.poner("I3", False)
    self.maqueta.poner("I2", 2500)
    time.sleep(1.5)                        # deja terminar ejectWhite y volver a reposo

    # d) Cierro la maqueta -> la librería avisa y deja de enviar out (D006.7).
    self.maqueta.parar()
    self.assertTrue(esperar(lambda: self.lineas("caducada"), 2.0), "la librería no avisó de que espera")
    print("\n  d) maqueta cerrada:\n    " + self.lineas("caducada")[-1].strip())

    # Escucho out durante 1 s con otra maqueta que NO publica in.
    oyente = MaquetaVirtual(BROKER, PUERTO)
    oyente._publicar = lambda: None        # solo escucha
    oyente.arrancar()
    time.sleep(1.0)
    _, t_ultima = oyente.estado_out()
    oyente.parar()
    self.assertIsNone(t_ultima, "con la maqueta cerrada no debería llegar ningún out")
    print("    1 s escuchando vio/txt5/out con la maqueta cerrada: 0 mensajes")

    # La vuelvo a abrir -> la librería sigue sola.
    self.maqueta = MaquetaVirtual(BROKER, PUERTO)
    self.maqueta.arrancar()
    self.assertTrue(esperar(lambda: self.lineas("recuperadas"), 2.0), "la librería no se recuperó")
    print("    maqueta reabierta:\n    " + self.lineas("recuperadas")[-1].strip())
    self.buscar(lambda img: True, timeout=2)
    inicio_d = len(self.maqueta.historial())
    self.maqueta.poner("I1", True)
    self.buscar(lambda img: m1(img).get("speed") == 512 and m1(img).get("running") is True, inicio_d)
    print("    y con I1 oscuro vuelve a arrancar la cinta (512, running true) sin reiniciar nada")
    self.assertEqual(self.proceso.poll(), None, "run_sld_virtual.py no debería haber terminado")


if __name__ == "__main__":
  unittest.main(verbosity=2)
