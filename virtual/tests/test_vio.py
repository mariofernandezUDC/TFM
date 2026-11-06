"""Reglas del enlace vio sin broker: validación de entradas, caducidad y D006.7.

Uso:  py -3.12 -m unittest discover -s virtual/tests -v
"""

import json
import sys
import threading
import time
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from fischertechnik import dispositivos as d   # noqa: E402
from fischertechnik import vio                  # noqa: E402

IMAGEN_OK = {"I1": False, "I2": 1450, "I3": False, "I6": False, "I7": False, "I8": False, "C1": False}


class _ClienteFalso:
  """Sustituye al cliente MQTT: apunta lo que se publica."""

  def __init__(self):
    self.publicados = []

  def publish(self, topic, carga, qos=0, retain=False):
    self.publicados.append((topic, json.loads(carga), qos, retain))


def txt5():
  """Crea el TXT 5 con los mismos bornes que vscode/lib/controller.py, sin conectarlo."""
  enlace = vio.EnlaceVio(5)
  c = d.Controlador(5, enlace)
  bornes = {
    "I1": d.PhotoTransistor(c, 1), "I2": d.ColorSensor(c, 2), "I3": d.PhotoTransistor(c, 3),
    "I6": d.PhotoTransistor(c, 6), "I7": d.PhotoTransistor(c, 7), "I8": d.PhotoTransistor(c, 8),
    "O5": d.MagneticValve(c, 5), "O6": d.MagneticValve(c, 6), "O7": d.MagneticValve(c, 7),
    "O8": d.Compressor(c, 8), "M1": d.EncoderMotorVirtual(c, 1), "C1": d.MiniSwitchCounter(c, 1),
  }
  enlace._cliente = _ClienteFalso()
  return enlace, bornes


class TestEntradas(unittest.TestCase):

  def test_imagen_valida_se_lee_desde_memoria(self):
    enlace, b = txt5()
    enlace.procesar_entrada(json.dumps(dict(IMAGEN_OK, I1=True, C1=True)).encode())
    self.assertTrue(b["I1"].is_dark())
    self.assertEqual(b["I2"].get_voltage(), 1450)
    self.assertTrue(b["C1"].get_state())

  def test_i2_decimal_se_entrega_entero(self):
    enlace, b = txt5()
    enlace.procesar_entrada(json.dumps(dict(IMAGEN_OK, I2=1450.7)).encode())
    self.assertEqual(b["I2"].get_voltage(), 1450)
    self.assertIsInstance(b["I2"].get_voltage(), int)

  def test_mensajes_invalidos_se_descartan_sin_renovar_la_hora(self):
    enlace, _ = txt5()
    malos = [b"no es json", b"[1, 2]",
             json.dumps({k: v for k, v in IMAGEN_OK.items() if k != "I3"}).encode(),   # falta I3
             json.dumps(dict(IMAGEN_OK, I1=1)).encode(),                                # I1 no es bool
             json.dumps(dict(IMAGEN_OK, I2=True)).encode(),                             # I2 no es número
             b'{"I1": false, "I2": NaN, "I3": false, "I6": false, "I7": false, "I8": false, "C1": false}']
    with self.assertLogs("vio", "WARNING") as avisos:
      for carga in malos:
        enlace.procesar_entrada(carga)
    self.assertFalse(enlace.lazo_sano())
    # Aviso limitado: 6 descartes seguidos dan un solo aviso.
    self.assertEqual(len(avisos.records), 1)

  def test_claves_de_mas_se_ignoran(self):
    enlace, _ = txt5()
    enlace.procesar_entrada(json.dumps(dict(IMAGEN_OK, X9=123)).encode())
    self.assertTrue(enlace.lazo_sano())

  def test_lectura_espera_si_la_imagen_ha_caducado(self):
    enlace, b = txt5()
    enlace.procesar_entrada(json.dumps(IMAGEN_OK).encode())
    time.sleep(vio.CADUCIDAD_S + 0.1)
    self.assertFalse(enlace.lazo_sano())
    resultado = []
    hilo = threading.Thread(target=lambda: resultado.append(b["I1"].is_dark()), daemon=True)
    hilo.start()
    hilo.join(timeout=0.3)
    self.assertTrue(hilo.is_alive(), "la lectura debería estar esperando")
    enlace.procesar_entrada(json.dumps(dict(IMAGEN_OK, I1=True)).encode())
    hilo.join(timeout=0.3)
    self.assertEqual(resultado, [True], "la lectura debe devolver el valor de la imagen NUEVA")


class TestSalidas(unittest.TestCase):

  def test_imagen_de_salidas_como_d006(self):
    enlace, b = txt5()
    enlace.procesar_entrada(json.dumps(IMAGEN_OK).encode())
    b["M1"].set_speed(350, "CW")
    b["M1"].start()
    b["O8"].on()
    _, ultima, qos, retain = enlace._cliente.publicados[-1]
    self.assertEqual(ultima, {"M1": {"speed": 350, "dir": "CW", "running": True},
                              "O5": False, "O6": False, "O7": False, "O8": True})
    self.assertEqual((qos, retain), (0, False))

  def test_sin_cambio_no_se_publica_por_cambio(self):
    enlace, b = txt5()
    enlace.procesar_entrada(json.dumps(IMAGEN_OK).encode())
    b["O5"].off()
    b["O5"].off()
    self.assertEqual(len(enlace._cliente.publicados), 1)

  def test_d006_7_sin_entradas_frescas_no_sale_nada(self):
    enlace, b = txt5()
    b["M1"].start()                       # aún no hay imagen de entradas
    self.assertEqual(enlace._cliente.publicados, [])
    enlace.procesar_entrada(json.dumps(IMAGEN_OK).encode())
    time.sleep(vio.CADUCIDAD_S + 0.1)     # caduca
    b["O8"].on()
    self.assertEqual(enlace._cliente.publicados, [])


class TestTontos(unittest.TestCase):

  def test_controlador_tonto_da_valores_neutros(self):
    c = d.Controlador(1)
    self.assertFalse(d.PhotoTransistor(c, 1).is_dark())
    self.assertEqual(d.ColorSensor(c, 8).get_voltage(), 0)
    self.assertFalse(d.MiniSwitch(c, 5).get_state())
    motor = d.MotorVirtual(c, 1)
    motor.set_speed(512, "CCW")
    motor.start()
    self.assertEqual(motor.get_speed(), 512)

  def test_borne_fuera_del_protocolo_en_txt5_avisa(self):
    with self.assertRaises(NotImplementedError):
      d.PhotoResistor(d.Controlador(5, vio.EnlaceVio(5)), 3)


if __name__ == "__main__":
  unittest.main(verbosity=2)
