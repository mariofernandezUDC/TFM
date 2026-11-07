# Pista · Nivel 2

- Mira cómo espera el nivel 1 a la pieza en I1:
  ```python
  while not TXT_SLD_E5_I1_photo_transistor.is_dark():
    time.sleep(0.01)
  ```
  «Mientras I1 **no** esté a oscuras, espera un poco». Cuando sale del bucle, la pieza ya está ahí.
- Haz lo mismo con **I3**. Cuando salgas del bucle, para el motor con `stop()`.
- Si la cinta se para nada más arrancar, revisa el `not`.
