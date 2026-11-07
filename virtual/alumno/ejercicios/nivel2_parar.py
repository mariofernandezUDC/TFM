# =============================================================================
#  NIVEL 2 · Parar la cinta delante de los cilindros
# =============================================================================
#
#  QUÉ TIENE QUE PASAR EN LA PLANTA
#    La cinta arranca con la pieza en I1 (esto ya viene hecho: es el nivel 1)
#    y se para cuando la pieza llega delante de los cilindros, en la barrera I3.
#
#  QUÉ USAS
#    TXT_SLD_E5_I3_photo_transistor.is_dark()   True si hay una pieza delante
#    TXT_SLD_E5_M1_encodermotor.stop()          para la cinta
#    time.sleep(0.01)                           espera 10 ms (en los bucles de espera)
#
#  CÓMO SE EJECUTA (con Unity en Play)
#    py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel2_parar.py
#    Pon una pieza con la tecla 1. Cuando se pare, quítala con la tecla 0.
#
#  ¿Atascado?  Pista: pistas/nivel2.md     Solución: soluciones/nivel2.py
# =============================================================================

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
  print("Falta tu código en el hueco del nivel 2.")   # borra esta línea
  # <<<<< FIN DE TU CÓDIGO <<<<<

  print("Cinta parada. Quita la pieza (tecla 0 en Unity) para seguir.")
  while TXT_SLD_E5_I3_photo_transistor.is_dark():
    time.sleep(0.01)
