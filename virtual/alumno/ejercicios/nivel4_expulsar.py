# =============================================================================
#  NIVEL 4 · Expulsar la pieza a su rampa
# =============================================================================
#
#  QUÉ TIENE QUE PASAR EN LA PLANTA
#    La pieza llega a I3 y ya sabes su color (niveles 1 a 3, ya hechos).
#    Ahora la cinta la lleva, despacio, hasta su cilindro, se para y el
#    cilindro la empuja a su rampa: blanca, roja o azul.
#
#  CÓMO SABES QUE HA LLEGADO A SU CILINDRO
#    El pulsador C1 cambia de estado (True/False) cada poco trozo de cinta.
#    Cuenta los CAMBIOS desde I3: al llegar a PULSOS[color], la pieza está
#    delante de su cilindro.
#
#  QUÉ USAS
#    TXT_SLD_E5_C1_mini_switch.get_state()    True o False
#    PULSOS[color]                            cambios de C1 hasta su cilindro
#    VALVULAS[color].on() / .off()            abre y cierra su cilindro
#    TXT_SLD_E5_M1_encodermotor.stop()        para la cinta
#    time.sleep(0.5)                          medio segundo de empujón
#
#  CÓMO SE EJECUTA (con Unity en Play)
#    py -3.12 virtual/run_virtual.py --programa virtual/alumno/ejercicios/nivel4_expulsar.py
#    Prueba las tres piezas: teclas 1, 2 y 3.
#
#  ¿Atascado?  Pista: pistas/nivel4.md     Solución: soluciones/nivel4.py
# =============================================================================

import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

UMBRAL_MINIMO = 200
UMBRAL_BLANCO_ROJO = 1150
UMBRAL_ROJO_AZUL = 1400
UMBRAL_MAXIMO = 2000

# Cambios de C1 desde I3 hasta cada cilindro: los mismos que usa la fábrica (SLD.py).
PULSOS = {"blanca": 6, "roja": 15, "azul": 26}
VALVULAS = {"blanca": TXT_SLD_E5_O5_magnetic_valve,
            "roja": TXT_SLD_E5_O6_magnetic_valve,
            "azul": TXT_SLD_E5_O7_magnetic_valve}
BARRERAS = {"blanca": TXT_SLD_E5_I6_photo_transistor,
            "roja": TXT_SLD_E5_I7_photo_transistor,
            "azul": TXT_SLD_E5_I8_photo_transistor}

print("Nivel 4 en marcha. Pon una pieza en la entrada de la cinta.")

while True:
  # Niveles 1 a 3: arranca en I1, lee el color y para en I3.
  while not TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
  TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)
  TXT_SLD_E5_M1_encodermotor.start()
  minimo = 3000
  while not TXT_SLD_E5_I3_photo_transistor.is_dark():
    minimo = min(minimo, TXT_SLD_E5_I2_color_sensor.get_voltage())
    time.sleep(0.001)
  TXT_SLD_E5_M1_encodermotor.stop()
  color = None
  if UMBRAL_MINIMO <= minimo < UMBRAL_BLANCO_ROJO:
    color = "blanca"
  elif UMBRAL_BLANCO_ROJO <= minimo < UMBRAL_ROJO_AZUL:
    color = "roja"
  elif UMBRAL_ROJO_AZUL <= minimo < UMBRAL_MAXIMO:
    color = "azul"
  if color is None:
    print("Color desconocido (valor {}). Quita la pieza (tecla 0).".format(minimo))
    while TXT_SLD_E5_I3_photo_transistor.is_dark():
      time.sleep(0.01)
    continue
  print("La pieza es {}. La llevo a su cilindro.".format(color))

  # Enciende el compresor y avanza despacio hacia los cilindros.
  TXT_SLD_E5_O8_compressor.on()
  TXT_SLD_E5_M1_encodermotor.set_speed(350, Motor.CW)
  TXT_SLD_E5_M1_encodermotor.start()

  # >>>>> TU CÓDIGO AQUÍ (unas 10 líneas) >>>>>
  #   1. Cuenta los cambios de C1 hasta PULSOS[color].
  #   2. Para la cinta.
  #   3. Abre la válvula de su color medio segundo y ciérrala.
  print("Falta tu código en el hueco del nivel 4.")   # borra esta línea
  # <<<<< FIN DE TU CÓDIGO <<<<<

  TXT_SLD_E5_O8_compressor.off()
  time.sleep(0.5)   # deja caer la pieza por la rampa
  if BARRERAS[color].is_dark():
    print("Pieza {} en su rampa.".format(color))
  else:
    print("La pieza {} no ha llegado a su rampa.".format(color))
