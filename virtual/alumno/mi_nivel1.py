# =============================================================================
#  NIVEL 1 · Arrancar la cinta
# =============================================================================
#
#  QUÉ TIENE QUE PASAR EN LA PLANTA
#    Pones una pieza en la entrada. La barrera I1 se oscurece y la cinta
#    arranca. (Todavía no se para: la pieza llegará al final de la cinta.
#    Pararla es el nivel 2.)
#
#  QUÉ USAS
#    TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)   velocidad, no arranca
#    TXT_SLD_E5_M1_encodermotor.start()                    arranca
#
#  CÓMO SE EJECUTA (con Unity en Play)
#    py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel1_arrancar.py
#    Pon una pieza con la tecla 1 en la ventana Game de Unity. Ctrl+C para parar.
#
#  ¿Atascado?  Pista: pistas/nivel1.md     Solución: soluciones/nivel1.py
# =============================================================================

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
