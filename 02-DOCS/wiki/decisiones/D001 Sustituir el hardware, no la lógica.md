---
tags: [decisión]
---

# D001 · Sustituir el hardware, no la lógica

- **Fecha:** 2026-10-29 · **Estado:** ✅ Aceptada

## Contexto
Hay que conseguir que la planta funcione sin la maqueta. Opciones:
- A. Reescribir la lógica de control dentro de Unity (C#).
- B. Mantener la lógica Python original y sustituir **solo** la capa de hardware por una [[Librería fischertechnik virtual]].

## Decisión
**B.**

## Por qué
- El código de las estaciones no cambia, así que se valida contra el comportamiento real.
- El alumno programa contra **la misma API** que en el TXT: lo que aprende se puede llevar a la planta real.
- La superficie de la API es pequeña (~30 métodos).
- Encaja con la [[Puesta en marcha virtual]] (SIL) que se usa en la industria.

## Consecuencias
- Hace falta un puente de E/S en tiempo real ([[Imagen de E-S]]) y hay riesgos de temporización ([[Riesgos]]).
- Unity se queda sin lógica de control: es solo planta y visor.

Relacionado: [[Arquitectura de la planta virtual]].
