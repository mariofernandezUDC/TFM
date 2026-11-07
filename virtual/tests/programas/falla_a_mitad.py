# Programa de prueba: arranca la cinta en I1 y en I3 comete un error de escritura
# (set_sped en vez de set_speed) con la cinta todavía en marcha.
import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

while not TXT_SLD_E5_I1_photo_transistor.is_dark():
  time.sleep(0.01)
TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)
TXT_SLD_E5_M1_encodermotor.start()
TXT_SLD_E5_O8_compressor.on()

while not TXT_SLD_E5_I3_photo_transistor.is_dark():
  time.sleep(0.01)
TXT_SLD_E5_M1_encodermotor.set_sped(350, Motor.CW)
