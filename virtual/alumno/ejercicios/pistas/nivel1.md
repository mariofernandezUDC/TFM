# Pista · Nivel 1

- El motor de la cinta es `TXT_SLD_E5_M1_encodermotor`.
- Hacen falta **dos** llamadas, en este orden:
  1. `set_speed(...)` fija la velocidad (de 0 a 512) y el sentido (`Motor.CW`). **No arranca el motor.**
  2. `start()` lo arranca.
- Si solo pones `set_speed`, la cinta no se mueve. Es el error más típico.
