---
tags: [proyecto, arquitectura]
---

# Arquitectura de la planta virtual

La idea central es **sustituir el hardware, no la lógica** ([[D001 Sustituir el hardware, no la lógica]]).

```
┌─────────────────────────────────────────────────────────────┐
│ 4. LÓGICA DE CONTROL (Python)                                │
│    Operador:    FactoryMain.py + lib/ originales             │
│    Programador: programa del alumno (misma API)              │
└──────────────────────────────┬──────────────────────────────┘
                               │ motor.set_speed(), switch.is_closed()…
┌──────────────────────────────▼──────────────────────────────┐
│ 3. LIBRERÍA `fischertechnik` VIRTUAL (Python)                │
└──────────────────────────────┬──────────────────────────────┘
                               │ imagen de E/S por MQTT (`vio/…`)
┌──────────────────────────────▼──────────────────────────────┐
│ 2. PUENTE DE E/S — broker MQTT local/embebido                │
└──────────────────────────────┬──────────────────────────────┘
┌──────────────────────────────▼──────────────────────────────┐
│ 1. PLANTA VIRTUAL (Unity): ejes, sensores, neumática, piezas │
│    + escena 3D existente + compatibilidad `dt/*`             │
└─────────────────────────────────────────────────────────────┘
```

## Capas
1. **Planta virtual (Unity).** Un modelo de comportamiento por tipo de componente: [[Motor con encoder]], [[Final de carrera]], [[Barrera de luz]], [[Sensor de color]], [[Neumática - válvula y compresor]], [[Contador por pulsos]]. Las piezas tienen color propio y son ellas las que *causan* las lecturas de los sensores (ver [[Marioneta vs planta]]). 🔧 El diseño mecánico de cada modelo es de Mario.
2. **Puente de E/S.** Broker MQTT local, embebido en el paquete final ([[D004 Paquete autónomo para el alumno]]). Ver [[MQTT]].
3. **[[Librería fischertechnik virtual]].** Tiene la misma interfaz que la del TXT 4.0 y, por debajo, lee y escribe una [[Imagen de E-S]].
4. **Lógica de control.** La original de Fischertechnik ([[Lógica de control en Python]]) o la del alumno.

## Qué se reutiliza
- La escena 3D, la interfaz y los modos de [[Gemelo en Unity]].
- El [[Contrato MQTT]] `dt/*`: el hilo original `Digital_Twin.py` sigue publicándolo a partir de los contadores virtuales.
- Las grabaciones del [[Modo simulación actual]], como trazas de referencia para la validación (F7).

## Decisión: ¿quién mueve la escena? ✅
En modo virtual manda la planta: ver [[D007 Quién mueve la escena]] (F0.7). Era el riesgo "Doble fuente de movimiento" de [[Riesgos]].
