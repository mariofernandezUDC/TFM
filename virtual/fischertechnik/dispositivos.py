"""Bornes virtuales del TXT: sensores, actuadores, motores y contadores.

Cada borne pertenece a un controlador. Hay dos casos:
- Controlador con enlace `vio` (de momento solo el TXT 5): las entradas leen
  de la última imagen de `vio/txt5/in` y las salidas avisan al enlace al
  cambiar, para que publique `vio/txt5/out`.
- Controlador "tonto" (los demás): las entradas devuelven un valor neutro y
  las salidas guardan el valor sin efecto.

Claves de la imagen: entradas `I<n>`, salidas `O<n>`, motores `M<n>` y
contadores `C<n>`, igual que en el Inventario de E/S.
"""

from fischertechnik.controller.Motor import Motor as _MotorBase


class Controlador:
  """Un TXT 4.0. `enlace` es None si el controlador es tonto."""

  def __init__(self, numero, enlace=None):
    self.numero = numero
    self.enlace = enlace

  def __repr__(self):
    return "Controlador(TXT {}, {})".format(self.numero, "vio" if self.enlace else "tonto")


class _Borne:
  def __init__(self, controlador, puerto, prefijo):
    self.controlador = controlador
    self.puerto = puerto
    self.clave = "{}{}".format(prefijo, puerto)
    self.enlace = controlador.enlace


class _SoloTonto(_Borne):
  """Borne que el protocolo `vio` aún no define. Solo existe en controladores tontos."""

  def __init__(self, controlador, puerto, prefijo):
    super().__init__(controlador, puerto, prefijo)
    if self.enlace is not None:
      raise NotImplementedError(
        "{} ({}) en el TXT {} no está en el protocolo vio (D006)".format(
          self.clave, type(self).__name__, controlador.numero))


# --- Entradas ---

class _Entrada(_Borne):
  TIPO = bool       # tipo que debe traer la imagen de entradas
  NEUTRO = False    # valor que devuelve un controlador tonto
  PREFIJO = "I"

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto, self.PREFIJO)
    if self.enlace is not None:
      self.enlace.registrar_entrada(self.clave, self.TIPO)

  def _leer(self):
    if self.enlace is None:
      return self.NEUTRO
    return self.enlace.leer(self.clave)


class PhotoTransistor(_Entrada):
  """Barrera de luz (fototransistor). True = a oscuras, hay algo delante."""

  def is_dark(self):
    return self._leer()


class MiniSwitch(_Entrada):
  """Final de carrera. get_state() devuelve el estado tal como viaja en la imagen."""

  def get_state(self):
    return self._leer()

  def is_closed(self):
    return bool(self._leer())

  def is_open(self):
    return not self._leer()


class ColorSensor(_Entrada):
  """Sensor de color. get_voltage() en la escala de la API real (D006.4)."""
  TIPO = int
  NEUTRO = 0

  def get_voltage(self):
    return self._leer()


class PhotoResistor(_SoloTonto):
  """LDR del TXT 0. Solo tonto: no está en vio."""

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto, "I")

  def get_resistance(self):
    return 0


# --- Salidas ---

class _Salida(_Borne):
  def __init__(self, controlador, puerto, prefijo="O"):
    super().__init__(controlador, puerto, prefijo)
    if self.enlace is not None:
      self.enlace.registrar_salida(self.clave, self)

  def _cambiado(self):
    if self.enlace is not None:
      self.enlace.salida_cambiada()


class _OnOff(_Salida):
  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto)
    self._encendido = False

  def on(self):
    self._encendido = True
    self._cambiado()

  def off(self):
    self._encendido = False
    self._cambiado()

  def valor_vio(self):
    return self._encendido

  def a_cero(self):
    """Apaga sin publicar. Lo usa la parada de seguridad del lanzador."""
    self._encendido = False


class MagneticValve(_OnOff):
  """Electroválvula. on() abre el paso de aire al cilindro."""


class Compressor(_OnOff):
  """Compresor que da presión a las válvulas."""


class Led(_Salida):
  """LED. set_brightness(0..512)."""

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto)
    self._brillo = 0

  def set_brightness(self, brillo):
    self._brillo = int(brillo)
    self._cambiado()

  def valor_vio(self):
    return self._brillo

  def a_cero(self):
    self._brillo = 0


# --- Motores ---

class MotorVirtual(_MotorBase, _Salida):
  """Motor. Guarda velocidad, sentido y si está en marcha.

  Imagen en `out` (D006.4): {"speed": 0-512, "dir": "CW"/"CCW", "running": bool}.
  set_speed() no arranca el motor: igual que la API real, hace falta start().
  """

  def __init__(self, controlador, puerto):
    _Salida.__init__(self, controlador, puerto, "M")
    self._velocidad = 0
    self._sentido = _MotorBase.CW
    self._en_marcha = False

  def set_speed(self, velocidad, sentido=_MotorBase.CW):
    self._velocidad = int(velocidad)
    self._sentido = sentido
    self._cambiado()

  def get_speed(self):
    return self._velocidad

  def start(self):
    self._en_marcha = True
    self._cambiado()

  def stop(self):
    self._en_marcha = False
    self._cambiado()

  def is_running(self):
    return self._en_marcha

  def valor_vio(self):
    return {"speed": self._velocidad, "dir": self._sentido, "running": self._en_marcha}

  def a_cero(self):
    self._velocidad = 0
    self._en_marcha = False


class EncoderMotorVirtual(MotorVirtual):
  """Motor con encoder. Lo que solo usan otras estaciones queda para F1."""

  def set_distance(self, *args, **kwargs):
    raise NotImplementedError("set_distance(): no lo usa la SLD; se implementará en F1")

  def start_sync(self, *args, **kwargs):
    raise NotImplementedError("start_sync(): no lo usa la SLD; se implementará en F1")

  def stop_sync(self, *args, **kwargs):
    raise NotImplementedError("stop_sync(): no lo usa la SLD; se implementará en F1")


# --- Contadores ---

class MiniSwitchCounter(_Entrada):
  """Final de carrera usado como contador (C1 de la SLD).

  D006.2: C1 viaja como ESTADO del interruptor. SLD.py cuenta los cambios él
  mismo con get_state(). La cuenta no viaja, por eso get_count() no existe en vio.
  """
  PREFIJO = "C"

  def get_state(self):
    return self._leer()

  def get_count(self):
    if self.enlace is not None:
      raise NotImplementedError("{}: la cuenta no viaja en vio, usar get_state() (D006.2)".format(self.clave))
    return 0


class EncoderMotorCounter(_SoloTonto):
  """Contador de pasos de un motor con encoder. Solo tonto en F0."""

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto, "C")
    self.motor = None

  def set_motor(self, motor):
    self.motor = motor

  def get_count(self):
    return 0


class PhotoTransistorCounter(_SoloTonto):
  """Fototransistor usado como contador (C4 del VGR). Solo tonto en F0."""

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto, "C")

  def get_count(self):
    return 0

  def get_state(self):
    return False


# --- I2C y USB (solo tontos) ---

class EnvironmentSensor(_SoloTonto):
  """Sensor ambiental BME680 del TXT 0."""

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto, "I2C")

  def get_temperature(self):
    return 0.0

  def get_humidity(self):
    return 0.0

  def get_pressure(self):
    return 0.0

  def get_indoor_air_quality_as_number(self):
    return 0

  def get_accuracy(self):
    return 0


class Camera(_SoloTonto):
  """Cámara USB del TXT 0. Sin funciones: la SLD no la usa."""

  def __init__(self, controlador, puerto):
    super().__init__(controlador, puerto, "USB")
