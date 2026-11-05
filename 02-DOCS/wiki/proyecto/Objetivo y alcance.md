---
tags: [proyecto]
---

# Objetivo y alcance

## Objetivo
Convertir el gemelo digital existente (ver [[Gemelo en Unity]]), que hoy **necesita la planta física**, en una **planta virtual autónoma** que se pueda descargar. Un alumno podrá ejecutarla en su ordenador y probar, programar y "jugar" con la fábrica igual que con la maqueta real, sin maqueta, sin broker externo, sin InfluxDB y sin gafas de VR.

## Dos formas de uso ([[D002 Modo operador y modo programador]])
- **Modo operador:** la fábrica funciona sola con su lógica por defecto. El alumno pide piezas, cambia parámetros, provoca fallos y observa.
- **Modo programador:** el alumno sustituye la lógica de una o varias estaciones por su propio programa.

## Principio rector
**El mismo código funciona en la planta virtual y en la real.** Ver [[D001 Sustituir el hardware, no la lógica]] y [[Puesta en marcha virtual]].

## Qué NO es
- No es un simulador físico de alta fidelidad: basta con un comportamiento creíble a nivel de E/S (ver [[Imagen de E-S]]).
- No es otro reproductor de grabaciones: eso ya existe ([[Modo simulación actual]]).

## Alcance mínimo realista
Fases F0–F4 del [[Plan de trabajo]] completas. F5–F7 en versión básica o como trabajo futuro, según lo que decida el tutor ([[Preguntas para el tutor]]).
