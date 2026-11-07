# NIVEL 2 · Solución: parar la cinta cuando la pieza llega a I3.

import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

print("Nivel 2 en marcha. Pon una pieza en la entrada de la cinta.")

while True:
  # Nivel 1: espera una pieza en I1 y arranca la cinta.
  while not TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
  TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)
  TXT_SLD_E5_M1_encodermotor.start()
  print("Pieza en I1: cinta en marcha.")

  # >>>>> TU CÓDIGO AQUÍ: espera a que la pieza llegue a I3 y para la cinta (3 líneas) >>>>>
  while not TXT_SLD_E5_I3_photo_transistor.is_dark():
    time.sleep(0.01)
  TXT_SLD_E5_M1_encodermotor.stop()
  # <<<<< FIN DE TU CÓDIGO <<<<<

  print("Cinta parada. Quita la pieza (tecla 0 en Unity) para seguir.")
  while TXT_SLD_E5_I3_photo_transistor.is_dark():
    time.sleep(0.01)
