---
tags: [concepto, software]
---

# Hilos y sondeo (*polling*)

## Hilos
`FactoryMain.py` lanza **un hilo por estación** y otros para la comunicación. Todos se ejecutan "a la vez" y comparten variables globales. Ver [[Lógica de control en Python]].

## Sondeo
La lógica espera a que pase algo **preguntando en bucle**:
```python
while not isEjectionTriggered():   # SLD.py
    lastColorValue = readColorValue()
```
En el TXT eso es una lectura local de microsegundos. En la planta virtual, cada lectura sale de la [[Imagen de E-S]], así que el bucle "ve" el mundo con un retraso de hasta un periodo de refresco.

## Consecuencias
- Un eje que se referencia contra un [[Final de carrera]] puede pasarse un poco → hacen falta topes mecánicos en la simulación.
- Los pulsos rápidos se pueden perder → [[Contador por pulsos]].
- Un bucle sin timeout se queda colgado para siempre si el sensor nunca cambia (útil para enseñar a gestionar fallos).

❓ Si la cinta avanza a 30 mm/s y la imagen se refresca cada 10 ms, ¿cuánto puede pasarse como máximo la pieza al detectarla?
