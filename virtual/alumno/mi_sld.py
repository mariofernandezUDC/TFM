# =============================================================================
#  Plantilla para programar la SLD (cinta clasificadora por colores)
# =============================================================================
#
#  Este archivo es tu programa. Se ejecuta de arriba abajo, igual que el
#  main.py de un proyecto de ROBO Pro Coding. Funciona sin cambios en:
#    - la planta virtual:  py -3.12 virtual/run_virtual.py --programa tu_programa.py
#    - el TXT 4.0 real de la SLD (con el mismo lib/controller.py de la fábrica).
#
#  Copia este archivo con otro nombre (por ejemplo mi_sld.py) y cambia el
#  bucle del final. Si tu programa da un error, la planta se para sola y en
#  la consola verás el archivo, la línea y el mensaje.
#
# -----------------------------------------------------------------------------
#  Bornes de la SLD (TXT 5). Todos salen de "from lib.controller import *".
# -----------------------------------------------------------------------------
#
#  ENTRADAS
#  TXT_SLD_E5_I1_photo_transistor   Barrera de luz a la entrada de la cinta.
#                                   .is_dark() -> True si hay una pieza delante.
#  TXT_SLD_E5_I2_color_sensor       Sensor de color, encima de la cinta.
#                                   .get_voltage() -> número. Cuanto más clara
#                                   es la pieza, MENOS voltaje. Con la cinta
#                                   vacía da un valor alto. Umbrales de fábrica:
#                                     blanca   200 a 1149
#                                     roja    1150 a 1399
#                                     azul    1400 a 1999
#  TXT_SLD_E5_I3_photo_transistor   Barrera de luz delante de los cilindros.
#                                   .is_dark() -> True si hay una pieza delante.
#  TXT_SLD_E5_I6_photo_transistor   Barrera de la rampa BLANCA.  .is_dark()
#  TXT_SLD_E5_I7_photo_transistor   Barrera de la rampa ROJA.    .is_dark()
#  TXT_SLD_E5_I8_photo_transistor   Barrera de la rampa AZUL.    .is_dark()
#  TXT_SLD_E5_C1_mini_switch        Pulsador que cuenta el avance de la cinta.
#                                   .get_state() -> True/False. Cambia cada
#                                   poco trozo de cinta: cuenta los CAMBIOS
#                                   para saber cuánto ha avanzado la pieza.
#                                   Desde I3: unos 6 cambios hasta el cilindro
#                                   blanco, 15 hasta el rojo y 26 hasta el azul.
#
#  SALIDAS
#  TXT_SLD_E5_M1_encodermotor       Motor de la cinta.
#                                   .set_speed(v, Motor.CW)  v de 0 a 512.
#                                       No arranca el motor: solo fija la velocidad.
#                                   .start()       arranca con esa velocidad.
#                                   .stop()        para.
#                                   .is_running()  -> True si está en marcha.
#  TXT_SLD_E5_O5_magnetic_valve     Válvula del cilindro BLANCO. .on() / .off()
#  TXT_SLD_E5_O6_magnetic_valve     Válvula del cilindro ROJO.   .on() / .off()
#  TXT_SLD_E5_O7_magnetic_valve     Válvula del cilindro AZUL.   .on() / .off()
#  TXT_SLD_E5_O8_compressor         Compresor. .on() / .off()
#                                   Sin compresor los cilindros no empujan:
#                                   enciéndelo un poco antes de abrir la válvula.
#
#  CONSEJOS
#  - Pon un time.sleep() corto (por ejemplo 0.01 s) en los bucles de espera.
#  - Ctrl+C en la consola para tu programa. La cinta se para sola.
# =============================================================================

import time

from fischertechnik.controller.Motor import Motor
from lib.controller import *

cinta = TXT_SLD_E5_M1_encodermotor
entrada = TXT_SLD_E5_I1_photo_transistor
cilindros = TXT_SLD_E5_I3_photo_transistor

print("Plantilla SLD en marcha. Pon una pieza en la entrada de la cinta.")

while True:
  # 1. Espera a que llegue una pieza a la entrada (I1 a oscuras).
  while not entrada.is_dark():
    time.sleep(0.01)
  print("Pieza en la entrada: arranco la cinta.")
  cinta.set_speed(512, Motor.CW)
  cinta.start()

  # 2. Deja que avance hasta los cilindros (I3 a oscuras) y para.
  while not cilindros.is_dark():
    time.sleep(0.01)
  cinta.stop()
  print("Pieza delante de los cilindros: paro la cinta.")

  # 3. Aquí iría tu código: leer el color, contar pulsos de C1 y expulsar.
  #    Mientras, espera a que retires la pieza (I3 vuelve a luz).
  while cilindros.is_dark():
    time.sleep(0.01)
