---
tags: [concepto, software]
---

# Imagen de E/S (*process image*)

Técnica clásica de los PLC: en cada ciclo se **copian todas las entradas** a una tabla en memoria, el programa trabaja con esa copia y al final se **escriben todas las salidas**.

## En la planta virtual
- Unity publica el estado de todas las entradas de un TXT (sensores y contadores) varias veces por segundo (objetivo: ~100 Hz).
- La [[Librería fischertechnik virtual]] guarda la última imagen recibida, así que `is_dark()` responde al instante, sin esperar a la red.
- Las salidas (motores, válvulas) se envían cuando cambian.

## Ventajas
- La lógica no nota la red: lee de memoria igual que en el TXT.
- La tasa de refresco está controlada y se puede medir.

## Riesgo
Un cambio de entrada más rápido que el refresco se pierde. Ver [[Contador por pulsos]] y [[Hilos y sondeo (polling)]].

Protocolo concreto (`vio/*`): se diseña en [[F0 - Prueba de concepto SLD]], paso F0.3.
