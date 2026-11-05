---
tags: [contexto, claude]
---

# Contexto para Claude

> Nota pensada para que Claude (o cualquier asistente) recupere el contexto al empezar una sesión. **Mantenerla al día al final de cada sesión.**

## Quién y qué
- **Mario** es ingeniero mecánico (sector naval y fabricación mecánica) y programa mucho en su trabajo. Este es **su** TFM.
- El TFM parte del gemelo digital en Unity de la Learning Factory 4.0 de Fischertechnik (repositorio de Adrián Romero Lustres, UDC) y lo convierte en una **planta virtual autónoma**. Ver [[Objetivo y alcance]].
- Repositorio en el PC de Mario: `C:\Users\Mario\Desktop\TFM-UNITY\Github\TFM` (Unity 6000.3.6f1, URP).
- Esta bóveda: `C:\Users\Mario\Desktop\TFM-UNITY\Cerebro-TFM`.

## Reglas de trabajo (ver [[D003 Forma de trabajo con Claude]])
1. **Mario decide y escribe el código central. Claude explica, propone y revisa.** Claude escribe solo el código repetitivo, y siempre explicándolo.
2. **🔧 La mecánica y el movimiento los diseña Mario**: cinemática, velocidades, carreras, conversión de pasos a mm, modelado de piezas. Claude plantea preguntas y opciones, pero **no da la solución mecánica** salvo que Mario la pida.
3. Cada cambio sigue el formato **qué y por qué → dónde exactamente (archivo, función, línea) → cómo comprobarlo**.
4. Pasos pequeños y verificables, con un commit por paso.
5. Claude **no toca el repositorio** sin que Mario se lo pida.
6. Preguntas de comprensión ❓ de vez en cuando, como preparación para la defensa.
7. **Mario es nuevo en Git** ([[Git para principiantes]], [[Rutina Git]]): **un comando cada vez**, decir qué salida se espera y pedirle que pegue lo que le sale. Nunca proponer comandos destructivos (`push --force`, `reset --hard`, `clean`) sin explicar el riesgo.
8. **No pisar a Adrián:** seguir [[D005 Gestión del repositorio]] (solo push a `origin`, rama `planta-virtual`, carpetas y escena propias).
9. Al final de cada sesión: nota en `06 Diario`, actualizar esta nota y el [[Plan de trabajo]], y crear o enlazar las notas nuevas.

## Estado actual
- **Fase:** F0 sin empezar. Siguientes pasos: [[Rutina Git]] apartado A (con Claude, paso a paso) → [[F0 - Prueba de concepto SLD]] F0.1.
- **Repositorio:** `origin` = fork de Mario (`mariofernandezUDC/TFM`), `upstream` = Adrián (`AdrianRomeroUDC/TFM`). Rama de trabajo prevista: `planta-virtual` (sin crear). Pendiente: de quién es `adaptacion-vr`.
- **Decisiones tomadas:** [[D001 Sustituir el hardware, no la lógica]], [[D002 Modo operador y modo programador]], [[D003 Forma de trabajo con Claude]], [[D004 Paquete autónomo para el alumno]], [[D005 Gestión del repositorio]].
- **Abierto:** [[Preguntas para el tutor]] y "doble fuente de movimiento" (ver [[Riesgos]]).

## Al empezar una sesión, leer
[[00 Inicio]] → esta nota → el último diario → [[Plan de trabajo]] → la nota de la fase en curso.
