# NIVEL 3 · Solución: leer el color mientras pasa y escribirlo al parar en I3.

import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

UMBRAL_MINIMO = 200
UMBRAL_BLANCO_ROJO = 1150
UMBRAL_ROJO_AZUL = 1400
UMBRAL_MAXIMO = 2000

print("Nivel 3 en marcha. Pon una pieza en la entrada de la cinta.")

while True:
  # Nivel 1: espera una pieza en I1 y arranca la cinta.
  while not TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
  TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)
  TXT_SLD_E5_M1_encodermotor.start()

  # Mientras la pieza va de I1 a I3, lee el sensor de color.
  minimo = 3000
  while not TXT_SLD_E5_I3_photo_transistor.is_dark():
    # >>>>> HUECO 1: lee el sensor y guarda en minimo el valor más bajo (1 línea) >>>>>
    minimo = min(minimo, TXT_SLD_E5_I2_color_sensor.get_voltage())
    # <<<<< FIN DEL HUECO 1 <<<<<
    time.sleep(0.001)

  # Nivel 2: la pieza está en I3, se para la cinta.
  TXT_SLD_E5_M1_encodermotor.stop()
  if minimo == 3000:
    print("Falta tu código en el hueco 1 del nivel 3.")

  # >>>>> HUECO 2: pon en color "blanca", "roja" o "azul" según minimo (6 líneas) >>>>>
  color = None
  if UMBRAL_MINIMO <= minimo < UMBRAL_BLANCO_ROJO:
    color = "blanca"
  elif UMBRAL_BLANCO_ROJO <= minimo < UMBRAL_ROJO_AZUL:
    color = "roja"
  elif UMBRAL_ROJO_AZUL <= minimo < UMBRAL_MAXIMO:
    color = "azul"
  # <<<<< FIN DEL HUECO 2 <<<<<

  if color is None:
    print("Color desconocido (valor {}). ¿Has rellenado el hueco 2?".format(minimo))
  else:
    print("La pieza es {} (valor {}).".format(color, minimo))

  print("Quita la pieza (tecla 0 en Unity) para seguir.")
  while TXT_SLD_E5_I3_photo_transistor.is_dark():
    time.sleep(0.01)
