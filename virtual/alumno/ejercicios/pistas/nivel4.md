# Pista · Nivel 4

**Contar cambios de C1.** No cuentes cuántas veces lees `True`: cuenta cuántas veces el valor es **distinto del anterior**. Guarda el estado de antes y compáralo con el de ahora:

```python
cambios = 0
anterior = TXT_SLD_E5_C1_mini_switch.get_state()
while cambios < PULSOS[color]:
  ahora = TXT_SLD_E5_C1_mini_switch.get_state()
  if ahora != anterior:
    # aquí: suma 1 a cambios y recuerda el estado nuevo
    ...
  time.sleep(0.001)
```

Usa una pausa **corta** (`0.001`). Si es larga, te puedes saltar un cambio y la pieza se pasa de su cilindro.

**Expulsar.** Cuando salgas del bucle: `stop()` del motor, después `VALVULAS[color].on()`, `time.sleep(0.5)` y `VALVULAS[color].off()`.
