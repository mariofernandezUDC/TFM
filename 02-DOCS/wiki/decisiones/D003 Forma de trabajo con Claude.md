---
tags: [decisión, claude]
---

# D003 · Forma de trabajo con Claude

- **Fecha:** 2026-10-29 · **Estado:** ✅ Aceptada · **Ampliada:** 2026-11-06 (Capturas para la memoria)

## Decisión
- **Mario decide y escribe** el código central. Claude **explica, propone y revisa**. Claude solo escribe el código repetitivo, siempre explicándolo.
- 🔧 **La mecánica y el movimiento los diseña Mario.** Claude plantea preguntas y opciones, no soluciones mecánicas.
- Formato de cada cambio: **qué y por qué → dónde (archivo, función, línea) → cómo comprobarlo**.
- Pasos pequeños, con un commit por paso.
- Esta bóveda (`02-DOCS/wiki`) es el **contexto global**: se actualiza al final de cada sesión (diario, plan, [[00 Contexto para Claude]]).

## Capturas para la memoria (2026-11-06)
- En **cada hito o resultado importante**, el asistente hace una captura con la Unity CLI (`unity command capture ...`).
- Se guarda en `02-DOCS/attachments/<fase>/` (por ejemplo, `attachments/F0/`) con el nombre `AAAA-MM-DD_descripcion.png`. Los datos que la acompañan (CSV, registros) van en la misma carpeta, con el mismo prefijo de fecha.
- Se enlaza desde el diario del día **y** desde la nota que corresponda, con `![[...]]` y un pie de foto de una línea: qué se ve y por qué importa.
- **Nunca** en carpetas temporales: lo que solo está en una carpeta temporal se pierde. PNG de tamaño razonable, **menos de 1 MB** (reducir la resolución si hace falta).

## Por qué
Es el TFM de Mario: tiene que entender y poder defender cada línea ante el tribunal. Además, el diario y las decisiones alimentan directamente la memoria ([[Estructura de la memoria]]). Las capturas hechas en el momento son la prueba gráfica de cada resultado: rehacerlas meses después exige volver a montar el mismo estado de la planta.

## Pendiente
Revisar la normativa del máster sobre el uso de IA y declararlo en la memoria ([[Preguntas para el tutor]]).
