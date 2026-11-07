# =============================================================================
#  Ejemplo resuelto: clasificar piezas por color en la SLD
# =============================================================================
#
#  Qué hace, para cada pieza:
#    1. Espera a que llegue a la entrada (I1) y arranca la cinta.
#    2. Mientras va de I1 a I3, se queda con la lectura MÁS BAJA del sensor de
#       color (I2). Es la de la pieza: la cinta vacía da un valor alto.
#    3. En I3 decide el color, enciende el compresor y baja la velocidad.
#    4. Cuenta los cambios de C1 hasta llegar a su cilindro y para la cinta.
#    5. Abre la válvula medio segundo y mira si la pieza ha caído en su rampa.
#
#  Se ejecuta de arriba abajo, como un main.py de ROBO Pro Coding:
#    py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejemplos/clasificar_por_color.py
# =============================================================================

import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

# Umbrales de fábrica del sensor de color (los mismos que usa la fábrica).
UMBRAL_MINIMO = 200
UMBRAL_BLANCO_ROJO = 1150
UMBRAL_ROJO_AZUL = 1400
UMBRAL_MAXIMO = 2000

# Para cada color: válvula de su cilindro, cambios de C1 desde I3 hasta el
# cilindro y barrera de su rampa.
RAMPAS = {
  "blanca": (TXT_SLD_E5_O5_magnetic_valve, 6, TXT_SLD_E5_I6_photo_transistor),
  "roja": (TXT_SLD_E5_O6_magnetic_valve, 15, TXT_SLD_E5_I7_photo_transistor),
  "azul": (TXT_SLD_E5_O7_magnetic_valve, 26, TXT_SLD_E5_I8_photo_transistor),
}

VELOCIDAD_TRANSPORTE = 512   # de I1 a I3
VELOCIDAD_POSICION = 350     # de I3 al cilindro, más despacio para contar bien

cinta = TXT_SLD_E5_M1_encodermotor
compresor = TXT_SLD_E5_O8_compressor
contador = TXT_SLD_E5_C1_mini_switch


def color_de(voltaje):
  """Nombre del color según el voltaje más bajo leído, o None si no encaja."""
  if UMBRAL_MINIMO <= voltaje < UMBRAL_BLANCO_ROJO:
    return "blanca"
  if UMBRAL_BLANCO_ROJO <= voltaje < UMBRAL_ROJO_AZUL:
    return "roja"
  if UMBRAL_ROJO_AZUL <= voltaje < UMBRAL_MAXIMO:
    return "azul"
  return None


clasificadas = {"blanca": 0, "roja": 0, "azul": 0}
print("Clasificador por color en marcha. Pon piezas en la entrada de la cinta.")

while True:
  # 1. Espera una pieza en la entrada.
  while not TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
  cinta.set_speed(VELOCIDAD_TRANSPORTE, Motor.CW)
  cinta.start()

  # 2. De I1 a I3: guarda la lectura más baja del sensor de color.
  minimo = 3000
  while not TXT_SLD_E5_I3_photo_transistor.is_dark():
    minimo = min(minimo, TXT_SLD_E5_I2_color_sensor.get_voltage())
    time.sleep(0.001)

  # 3. Decide el color.
  color = color_de(minimo)
  if color is None:
    cinta.stop()
    print("Color desconocido (voltaje {}). Retira la pieza de I3.".format(minimo))
    while TXT_SLD_E5_I3_photo_transistor.is_dark():
      time.sleep(0.01)
    continue
  valvula, pulsos, barrera = RAMPAS[color]
  print("Pieza {} (voltaje {}). La llevo a su cilindro.".format(color, minimo))
  compresor.on()
  cinta.set_speed(VELOCIDAD_POSICION, Motor.CW)
  cinta.start()

  # 4. Cuenta los cambios de C1 hasta su cilindro.
  cambios = 0
  anterior = contador.get_state()
  while cambios < pulsos:
    estado = contador.get_state()
    if estado != anterior:
      cambios += 1
      anterior = estado
    time.sleep(0.001)
  cinta.stop()

  # 5. Expulsa y comprueba la rampa.
  valvula.on()
  time.sleep(0.5)
  valvula.off()
  compresor.off()
  time.sleep(0.5)
  if barrera.is_dark():
    clasificadas[color] += 1
    print("Pieza {} en su rampa. Llevo: {}".format(color, clasificadas))
  else:
    print("La pieza {} no ha llegado a su rampa.".format(color))
