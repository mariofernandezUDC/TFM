# NIVEL 1 · Solución: arrancar la cinta cuando llega una pieza a I1.

import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

print("Nivel 1 en marcha. Pon una pieza en la entrada de la cinta.")

while True:
  # Espera a que llegue una pieza a la entrada (I1 a oscuras).
  while not TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
  print("Pieza en I1.")

  # >>>>> TU CÓDIGO AQUÍ: arranca la cinta (2 líneas) >>>>>
  TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)
  TXT_SLD_E5_M1_encodermotor.start()
  # <<<<< FIN DE TU CÓDIGO <<<<<

  # Espera a que la pieza deje atrás I1, para no repetir con la misma pieza.
  while TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
