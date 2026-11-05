---
tags: [concepto, componente, riesgo]
---

# Contador por pulsos

Algunas posiciones no se miden con un encoder sino **contando pulsos de un sensor**. Por ejemplo, un final de carrera que una rueda dentada pulsa al avanzar la cinta.

## Caso SLD
`SLD.py` cuenta los pulsos de `C1` **por software**: lee `get_state()` en bucle y suma 1 cada vez que cambia. Ver [[SLD - Clasificadora]].

## ⚠️ Riesgo en la planta virtual
Si la [[Imagen de E-S]] se refresca a 100 Hz (cada 10 ms) y el interruptor cambia de estado más deprisa, **se pierden cambios** y la pieza se expulsa en la rampa equivocada.

❓ Primero hay que medir: 🔧 ¿cuántos cambios por segundo da `C1` con la cinta a velocidad 350?

## Opciones de software (para decidir con datos)
1. Si los pulsos son lentos (< ~20 Hz): no hace falta hacer nada.
2. Subir la tasa de refresco solo para ese TXT.
3. Que la planta virtual "sirva" un historial de cambios en lugar de solo el último estado.
4. Que la librería virtual entregue cada cambio de estado a `get_state()` en orden.

→ Primera parte decidida en [[D006 Protocolo vio]] (D006.2): `C1` viaja como estado, la imagen va a 100 Hz por defecto (configurable) y `SLD.py` cuenta él mismo. Eso es la opción 1, con la 2 disponible porque la frecuencia es configurable. Las opciones 3 y 4 quedan en reserva hasta medir la frecuencia real de `C1` (F0.6).
