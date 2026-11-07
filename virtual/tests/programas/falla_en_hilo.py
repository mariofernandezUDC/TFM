# Programa de prueba: el error ocurre en un hilo del alumno, no en el principal.
# El hilo principal sigue en un bucle con la cinta en marcha.
import threading
import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *


def vigilar_i3():
  while not TXT_SLD_E5_I3_photo_transistor.is_dark():
    time.sleep(0.01)
  raise ValueError("fallo a propósito en el hilo")


while not TXT_SLD_E5_I1_photo_transistor.is_dark():
  time.sleep(0.01)
TXT_SLD_E5_M1_encodermotor.set_speed(512, Motor.CW)
TXT_SLD_E5_M1_encodermotor.start()
threading.Thread(target=vigilar_i3, name="vigilar_i3").start()
while True:
  TXT_SLD_E5_M1_encodermotor.start()
  time.sleep(0.01)
