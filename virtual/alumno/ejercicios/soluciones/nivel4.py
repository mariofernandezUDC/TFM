# NIVEL 4 · Solución: llevar la pieza a su cilindro contando C1 y expulsarla.

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
  cambios = 0
  anterior = TXT_SLD_E5_C1_mini_switch.get_state()
  while cambios < PULSOS[color]:
    ahora = TXT_SLD_E5_C1_mini_switch.get_state()
    if ahora != anterior:
      cambios = cambios + 1
      anterior = ahora
    time.sleep(0.001)
  TXT_SLD_E5_M1_encodermotor.stop()
  VALVULAS[color].on()
  time.sleep(0.5)
  VALVULAS[color].off()
  # <<<<< FIN DE TU CÓDIGO <<<<<

  TXT_SLD_E5_O8_compressor.off()
  time.sleep(0.5)   # deja caer la pieza por la rampa
  if BARRERAS[color].is_dark():
    print("Pieza {} en su rampa.".format(color))
  else:
    print("La pieza {} no ha llegado a su rampa.".format(color))
