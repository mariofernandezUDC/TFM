# =============================================================================
#  NIVEL 3 · Leer el color de la pieza
# =============================================================================
#
#  QUÉ TIENE QUE PASAR EN LA PLANTA
#    La cinta lleva la pieza de I1 a I3 (niveles 1 y 2, ya hechos). Mientras
#    pasa bajo el sensor de color, tu programa lee su valor. Al parar en I3,
#    escribe en pantalla si la pieza es blanca, roja o azul.
#
#  QUÉ USAS
#    TXT_SLD_E5_I2_color_sensor.get_voltage()   un número: cuanto más clara es
#                                               la pieza, MÁS BAJO. Sin pieza
#                                               debajo da un valor alto.
#    min(a, b)                                  el menor de dos números
#    Umbrales (ya están abajo como constantes):
#      blanca   200 a 1149  ·  roja  1150 a 1399  ·  azul  1400 a 1999
#
#  CÓMO SE EJECUTA (con Unity en Play)
#    py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel3_color.py
#    Prueba las tres piezas: teclas 1, 2 y 3. Quita cada una con la tecla 0.
#
#  ¿Atascado?  Pista: pistas/nivel3.md     Solución: soluciones/nivel3.py
# =============================================================================

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
    pass   # borra esta línea
    # <<<<< FIN DEL HUECO 1 <<<<<
    time.sleep(0.001)

  # Nivel 2: la pieza está en I3, se para la cinta.
  TXT_SLD_E5_M1_encodermotor.stop()
  if minimo == 3000:
    print("Falta tu código en el hueco 1 del nivel 3.")

  # >>>>> HUECO 2: pon en color "blanca", "roja" o "azul" según minimo (6 líneas) >>>>>
  color = None
  # <<<<< FIN DEL HUECO 2 <<<<<

  if color is None:
    print("Color desconocido (valor {}). ¿Has rellenado el hueco 2?".format(minimo))
  else:
    print("La pieza es {} (valor {}).".format(color, minimo))

  print("Quita la pieza (tecla 0 en Unity) para seguir.")
  while TXT_SLD_E5_I3_photo_transistor.is_dark():
    time.sleep(0.01)
