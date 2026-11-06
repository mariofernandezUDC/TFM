"""Fábricas de E/S virtuales: lo que `vscode/lib/controller.py` llama al arrancar.

Mismos nombres que la librería del TXT: `init()`, `init_*_factory()`,
`*_factory.create_*()` e `initialized()`. Cada `create_*` devuelve un borne
virtual (ver dispositivos.py).

Solo los controladores de CONTROLADORES_VIO se conectan al protocolo `vio`.
El resto son tontos, pero se crean igual porque `controller.py` crea las E/S
de los 6 TXT al importarse.
"""

from fischertechnik import dispositivos as _d
from fischertechnik import vio as _vio

CONTROLADORES_VIO = {5}   # F0: solo el TXT 5 (SLD), D006

_enlaces = []
_inicializado = False


def init():
  """En el TXT arranca el firmware. Aquí no hace falta nada."""


def init_input_factory():
  pass


def init_output_factory():
  pass


def init_motor_factory():
  pass


def init_counter_factory():
  pass


def init_i2c_factory():
  pass


def init_usb_factory():
  pass


def init_camera_factory():
  pass


def initialized():
  """Fin de la configuración: arranca los enlaces vio, ya con todos los bornes creados."""
  global _inicializado
  if _inicializado:
    return
  _inicializado = True
  for enlace in _enlaces:
    enlace.arrancar()


def cerrar():
  """Desconecta los enlaces vio. Sin latido, Unity pasa a estado seguro."""
  for enlace in _enlaces:
    enlace.cerrar()


class _ControllerFactory:
  def create_graphical_controller(self, numero=0):
    # controller.py llama al TXT 0 (master) sin número.
    enlace = None
    if numero in CONTROLADORES_VIO:
      enlace = _vio.EnlaceVio(numero)
      _enlaces.append(enlace)
    return _d.Controlador(numero, enlace)


class _InputFactory:
  def create_mini_switch(self, controlador, puerto):
    return _d.MiniSwitch(controlador, puerto)

  def create_photo_transistor(self, controlador, puerto):
    return _d.PhotoTransistor(controlador, puerto)

  def create_photo_resistor(self, controlador, puerto):
    return _d.PhotoResistor(controlador, puerto)

  def create_color_sensor(self, controlador, puerto):
    return _d.ColorSensor(controlador, puerto)


class _OutputFactory:
  def create_led(self, controlador, puerto):
    return _d.Led(controlador, puerto)

  def create_magnetic_valve(self, controlador, puerto):
    return _d.MagneticValve(controlador, puerto)

  def create_compressor(self, controlador, puerto):
    return _d.Compressor(controlador, puerto)


class _MotorFactory:
  def create_motor(self, controlador, puerto):
    return _d.MotorVirtual(controlador, puerto)

  def create_encodermotor(self, controlador, puerto):
    return _d.EncoderMotorVirtual(controlador, puerto)


class _CounterFactory:
  def create_encodermotor_counter(self, controlador, puerto):
    return _d.EncoderMotorCounter(controlador, puerto)

  def create_mini_switch_counter(self, controlador, puerto):
    return _d.MiniSwitchCounter(controlador, puerto)

  def create_photo_transistor_counter(self, controlador, puerto):
    return _d.PhotoTransistorCounter(controlador, puerto)


class _I2cFactory:
  def create_environment_sensor(self, controlador, puerto):
    return _d.EnvironmentSensor(controlador, puerto)


class _UsbFactory:
  def create_camera(self, controlador, puerto):
    return _d.Camera(controlador, puerto)


controller_factory = _ControllerFactory()
input_factory = _InputFactory()
output_factory = _OutputFactory()
motor_factory = _MotorFactory()
counter_factory = _CounterFactory()
i2c_factory = _I2cFactory()
usb_factory = _UsbFactory()
