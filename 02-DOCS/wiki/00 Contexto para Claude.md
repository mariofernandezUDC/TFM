---
tags: [contexto, claude]
---

# Contexto para Claude

> Nota pensada para que Claude (o cualquier asistente) recupere el contexto al empezar una sesión. **Mantenerla al día al final de cada sesión.**

## Quién y qué
- **Mario** es ingeniero mecánico (sector naval y fabricación mecánica) y programa mucho en su trabajo. Este es **su** TFM.
- El TFM parte del gemelo digital en Unity de la Learning Factory 4.0 de Fischertechnik (repositorio de Adrián Romero Lustres, UDC) y lo convierte en una **planta virtual autónoma**. Ver [[Objetivo y alcance]].
- Repositorio en el PC de Mario: `C:\Users\Mario\Desktop\TFM-UNITY\Github\TFM` (Unity 6000.3.6f1, URP).
- Esta bóveda: `02-DOCS/wiki` dentro del repositorio (rama `planta-virtual`). Original tal como llegó: `02-DOCS/raw/boveda-obsidian/`. Ver [[D005 Gestión del repositorio]], punto 9.

## Reglas de trabajo (ver [[D003 Forma de trabajo con Claude]])
1. **Mario decide y escribe el código central. Claude explica, propone y revisa.** Claude escribe solo el código repetitivo, y siempre explicándolo.
2. **🔧 La mecánica y el movimiento los diseña Mario**: cinemática, velocidades, carreras, conversión de pasos a mm, modelado de piezas. Claude plantea preguntas y opciones, pero **no da la solución mecánica** salvo que Mario la pida.
3. Cada cambio sigue el formato **qué y por qué → dónde exactamente (archivo, función, línea) → cómo comprobarlo**.
4. Pasos pequeños y verificables, con un commit por paso.
5. Claude **no toca el repositorio** sin que Mario se lo pida. **El push lo hace siempre Mario.**
6. Preguntas de comprensión ❓ de vez en cuando, como preparación para la defensa.
7. **Mario es nuevo en Git** ([[Git para principiantes]], [[Rutina Git]]): **un comando cada vez**, decir qué salida se espera y pedirle que pegue lo que le sale. Nunca proponer comandos destructivos (`push --force`, `reset --hard`, `clean`) sin explicar el riesgo.
8. **No pisar a Adrián:** seguir [[D005 Gestión del repositorio]] (solo push a `origin`, rama `planta-virtual`, carpetas y escena propias).
9. Al final de cada sesión: nota en `diario/`, actualizar esta nota y el [[Plan de trabajo]], y crear o enlazar las notas nuevas.

## Estado actual (2026-11-06)
- **Fase:** F0 en curso. F0.1 ✅ (diagrama de estados en [[SLD - Clasificadora]]). F0.3 ✅ (protocolo `vio`, [[D006 Protocolo vio]], ampliado con D006.7 y D006.8). F0.4 ✅ (librería virtual en `virtual/`, ver [[Librería fischertechnik virtual]]) → 🏁 hito 1 ✅ (test automático y prueba a mano; el experimento sin `I6` confirma el comportamiento raro nº 1). F0.5 ✅ (`VirtualIO_TXT.cs`, escena `PlantaVirtual.unity`, 100 Hz, probado a mano desde el Inspector). F0.6 🟡: pasos 1 a 3 hechos (los pasos 2 y 3 los hizo Codex), `SLD.py` clasifica las tres piezas solas; faltan las medidas reales 🔧. F0.7 ✅ ([[D007 Quién mueve la escena]]). F0.8 ⏳ en versión corta. F0.2 sigue ⏳. Python del PC: `py -3.12` (con paho-mqtt 2.2.0); Mosquitto como servicio de Windows. De [[Rutina Git]] apartado A, el A3 (crear la rama) ya está hecho.
- **⭐ Próxima prioridad (petición del tutor, 2026-11-06):** prototipo de **"cargar código"**: un lanzador que ejecute un programa del alumno en lugar de `SLD.py`, contra la planta virtual. Va por delante de afinar la física. El tutor dijo: "el simulador ya va muy por delante de lo que necesitaremos; ¿tienes un prototipo del controlador de la SLD que permita cargar código?". Antes, preguntarle si los alumnos programarán en Python o en PLC ([[Preguntas para el tutor]]).
- **Lo último medido (2026-11-06):**
  - `sys.setswitchinterval(0.001)` en `run_sld_virtual.py` (`cf1a692`) baja el p95 de recepción de 105 ms a 5 ms. La causa era el GIL.
  - Sin ese retraso aleatorio, la roja para siempre en −8,4 mm (franja de 2 mm). 8 de 10 quedan fuera de la tolerancia provisional de ±8 mm. Había dos errores que se compensaban a ratos.
  - Sensor rojo `I7` movido +15 mm en X, solo en `PlantaVirtual.unity`.
- **Pendientes 🔧 de Mario:** decidir la tolerancia de captura. Medir en la maqueta `v_max`, los mm por cuenta de `C1`, las distancias de `I3` a cada cilindro y la inercia del motor al parar (el modelo para en seco).
- **Otros pendientes:** un display falso que solo imprima los cambios. Borrar la rama local `f05-cierre` (`git branch -D f05-cierre`; su commit ya está en `planta-virtual`).
- **Unity:** Unity CLI con `com.unity.pipeline` 0.10.0-exp.1. Burst está desactivado en el editor porque Smart App Control de Windows lo bloquea. Las capturas para la memoria van en `02-DOCS/attachments/<fase>/` ([[D003 Forma de trabajo con Claude]]).
- **Repositorio:** `origin` = fork de Mario (`mariofernandezUDC/TFM`), `upstream` = Adrián (`AdrianRomeroUDC/TFM`, sin permiso de push). Rama de trabajo: `planta-virtual` (existe en local y en `origin`). Pendiente: de quién es `adaptacion-vr`.
- **Harness del asistente:** instalado el 2026-11-05 (commit `671f715`). Codex añadido como segundo asistente el 2026-11-06 (`3a62d3b`). `main` está bloqueada: el asistente trabaja en `planta-virtual` y no hace push. Aislamiento de worktrees desactivado ([[D005 Gestión del repositorio]], "Sin worktrees"). Configuración en `harness/`.
- **Decisiones tomadas:** [[D001 Sustituir el hardware, no la lógica]], [[D002 Modo operador y modo programador]], [[D003 Forma de trabajo con Claude]], [[D004 Paquete autónomo para el alumno]], [[D005 Gestión del repositorio]] (revisada el 2026-11-05), [[D006 Protocolo vio]] (2026-11-05, solo TXT 5; D006.5 latido y D006.6 prueba sin Unity cierran sus puntos abiertos; D006.7 y D006.8 el 2026-11-06), [[D007 Quién mueve la escena]] (2026-11-06: en modo virtual manda la planta; pendiente para F2, el VGR lee propiedades de `ControladorCilindrosSLD_mqtt`).
- **Abierto:**
  - [[Preguntas para el tutor]].
  - "Doble fuente de movimiento" (ver [[Riesgos]]).
  - Pendientes de D006: medidas 🔧 de la cinta y de `C1`, tiempo T en las grabaciones, unidad de `get_voltage()`, autoría de `SLD.py` (licencia).
- **Formato del protocolo:** [[Protocolo vio]]. Valores iguales a los de la API real, sin conversión.

## Al empezar una sesión, leer
[[index]] → esta nota → el último diario → [[Plan de trabajo]] → la nota de la fase en curso.
