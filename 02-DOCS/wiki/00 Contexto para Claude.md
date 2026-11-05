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

## Estado actual (2026-11-05)
- **Fase:** F0 en curso. F0.3 ✅ (protocolo `vio`, [[D006 Protocolo vio]]). F0.1 y F0.2 siguen ⏳. Siguiente meta: F0.4 (incluye la herramienta `maqueta_manual.py`) → 🏁 hito 1 ("`SLD.py` original piensa en mi PC sin maqueta, probado a mano con `maqueta_manual.py`"). De [[Rutina Git]] apartado A, el A3 (crear la rama) ya está hecho.
- **Repositorio:** `origin` = fork de Mario (`mariofernandezUDC/TFM`), `upstream` = Adrián (`AdrianRomeroUDC/TFM`, sin permiso de push). Rama de trabajo: `planta-virtual` (existe en local y en `origin`). Pendiente: de quién es `adaptacion-vr`.
- **Harness del asistente:** instalado el 2026-11-05 (commit `671f715`). `main` está bloqueada: el asistente trabaja en `planta-virtual` y no hace push. Configuración en `harness/`.
- **Decisiones tomadas:** [[D001 Sustituir el hardware, no la lógica]], [[D002 Modo operador y modo programador]], [[D003 Forma de trabajo con Claude]], [[D004 Paquete autónomo para el alumno]], [[D005 Gestión del repositorio]] (revisada el 2026-11-05), [[D006 Protocolo vio]] (2026-11-05, solo TXT 5; D006.5 latido y D006.6 prueba sin Unity cierran sus puntos abiertos).
- **Próxima decisión:** D007 = quién mueve la escena (F0.7).
- **Abierto:**
  - [[Preguntas para el tutor]].
  - "Doble fuente de movimiento" (ver [[Riesgos]]).
  - Pendientes de D006: medidas 🔧 de la cinta y de `C1`, tiempo T en las grabaciones, unidad de `get_voltage()`, autoría de `SLD.py` (licencia).
- **Formato del protocolo:** [[Protocolo vio]]. Valores iguales a los de la API real, sin conversión.

## Al empezar una sesión, leer
[[index]] → esta nota → el último diario → [[Plan de trabajo]] → la nota de la fase en curso.
