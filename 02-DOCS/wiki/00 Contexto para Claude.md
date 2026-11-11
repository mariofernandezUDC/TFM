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

## Estado actual (2026-11-11)
- **Fase en curso: F5, lanzador «Cargar programa» desde Unity** (prioridad del tutor). Encargo: [[Encargo - Lanzador desde Unity]]. Último diario: [[2026-11-11]].
  - P1 ✅ [[D008 Lanzador desde Unity]] (`f14c4e8`). P2 ✅ `LanzadorPrograma.cs` y test automático (`752617f`, `d1ccf58`, `4bd36c4`, `027b87a`). P3 ✅ Parar y código de salida (`27defdc`).
  - P4a ✅ esqueleto del dashboard (`4ced235`) y ajustes tras la revisión de Mario (`651ce47`): soluciones y plantilla ocultas al alumno, SLD entera en el hueco central (vista 55°/90°), paneles negros con alfa 0,85, salida en 4 líneas con «Ver detalle». Diseño en [[D009 Dashboard del alumno]].
  - P4b ✅ panel «Sensores y salidas» y etiquetas `I1`…`C1` sobre la planta (cian = activa); escala mínima 12/14 para que el texto no baje de 12 px a 1366×768.
  - P4c ✅ «Resultado» (% de acierto, bien/mal/en cinta), «Ciclo por pieza» (media y las 4 últimas) y «Retos» (niveles 1–4; pinchar uno lo elige). Cada pieza cuenta una vez, al pasar a `EnRampa`; nunca por el estado de I6–I8.
  - P4d ✅ píldoras Blanca, Roja, Azul y Quitar encima de la planta (lo mismo que las teclas 1, 2, 3 y 0). Botón neutro; el color solo en el punto (D009). Si la entrada está ocupada, aviso en «Salida».
  - P5 ✅ errores en rojo en «Salida»: el bloque de `mostrar_error` (archivo y línea, también de un hilo), la salida se amplía sola y el estado pasa a «Error». Si el programa no llega a arrancar (Python o programa no encontrado), aviso en rojo y código −2 (evento `LanzadorPrograma.NoArranca`).
  - 🏁 Hito para el tutor: «Cargar programa» se puede enseñar. Guion de la reunión al día (demo con el dashboard; PowerShell como plan B).
  - **P6 aplazado** (2026-11-11) hasta después de la reunión con el tutor: meter binarios de terceros en el repo y su licencia (EPL-2.0/EDL) se deciden allí. Para la demo basta el Mosquitto del PC (servicio en 1883).
  - 14 tests de PlayMode en verde (`Assets/Scripts/PlantaVirtual/Tests/LanzadorProgramaTest.cs`; necesitan Mosquitto en marcha). Tiempos a rampa 2,7 / 4,3 / 5,3 s; estado seguro a 0,50 s tras Parar.
- **F0 en pausa** desde que el tutor priorizó «cargar código». F0.1, F0.3, F0.4 (hito 1), F0.5 y F0.7 ✅. F0.6 🟡: `SLD.py` clasifica las tres piezas; faltan las medidas reales 🔧. F0.2, F0.8 (versión corta) y F0.9 ⏳.
- **F5:** F5.1 ✅ y F5.2 ✅ (2026-11-07). F5.3 ⏳ (prueba de ROBO Pro Coding 🔧). F5.4 🚧 lanzador desde Unity.
- **Lo último medido (2026-11-11):** en el test de P2 la roja paró a −8,8 mm. Entra en la tolerancia provisional de 10 mm (con 8 mm habría fallado). La tolerancia definitiva sale de la medida en la maqueta 🔧.
- **Pendientes 🔧 de Mario:** tolerancia de captura de la roja. Medir en la maqueta `v_max`, los mm por cuenta de `C1`, las distancias de `I3` a cada cilindro y la inercia del motor al parar. Prueba de ROBO Pro Coding (F5.3).
- **Otros pendientes:** display falso que solo imprima los cambios. Borrar la rama local `f05-cierre`. Preguntar al tutor si los alumnos programarán en Python o en PLC ([[Preguntas para el tutor]]).
- **Entorno (sigue vigente):** Python del PC `py -3.12` con paho-mqtt 2.2.0; Mosquitto como servicio de Windows en 1883. `sys.setswitchinterval(0.001)` en el lanzador (`cf1a692`) baja el p95 de recepción de 105 a 5 ms (era el GIL). Sensor rojo `I7` movido +15 mm en X, solo en `PlantaVirtual.unity`.
- **Unity:** Unity CLI con `com.unity.pipeline`. Burst desactivado en el editor (Smart App Control). `MQTT_InterfaceClient` desactivado en `PlantaVirtual.unity` (host `CHANGE_ME`). Unity ensucia el árbol de trabajo (Layout Groups, saltos de línea, `Unity.slnx`): ver [[Rutina Git]], «Ruido de Unity». Capturas en `02-DOCS/attachments/<fase>/`.
- **Repositorio:** `origin` = fork de Mario (`mariofernandezUDC/TFM`), `upstream` = Adrián (`AdrianRomeroUDC/TFM`, sin permiso de push). Rama de trabajo: `planta-virtual`. En `origin` está hasta `27defdc`; **pendientes de push por Mario:** `73fa1b7`, `4ced235`, `651ce47` y el registro de esta sesión. Pendiente: de quién es `adaptacion-vr`.
- **Harness del asistente:** rsc 4.1.2 (actualizado solo el 2026-11-11). Codex es el segundo asistente desde el 2026-11-06. `main` bloqueada: el asistente trabaja en `planta-virtual` y no hace push. Sin worktrees ([[D005 Gestión del repositorio]]). Configuración en `harness/`.
- **Decisiones tomadas:** [[D001 Sustituir el hardware, no la lógica]], [[D002 Modo operador y modo programador]], [[D003 Forma de trabajo con Claude]], [[D004 Paquete autónomo para el alumno]], [[D005 Gestión del repositorio]], [[D006 Protocolo vio]] (solo TXT 5; D006.5 latido de 0,5 s), [[D007 Quién mueve la escena]], [[D008 Lanzador desde Unity]] (2026-11-11: proceso aparte, desplegable, Parar = matar el proceso), [[D009 Dashboard del alumno]] (2026-11-11: planta en el centro, UI Toolkit, acentos lima/cian/ámbar, soluciones ocultas).
- **Abierto:**
  - [[Preguntas para el tutor]].
  - "Doble fuente de movimiento" (ver [[Riesgos]]).
  - Pendientes de D006: medidas 🔧 de la cinta y de `C1`, tiempo T en las grabaciones, unidad de `get_voltage()`, autoría de `SLD.py` (licencia).
  - Pendientes de D008: P6 (broker portable en 18830; aplazado hasta después de la reunión con el tutor), parada limpia con `vio/sim/parar`, ruta de Python y raíz del repo en la build (F6).
- **Formato del protocolo:** [[Protocolo vio]]. Valores iguales a los de la API real, sin conversión.

## Al empezar una sesión, leer
[[index]] → esta nota → el último diario → [[Plan de trabajo]] → la nota de la fase en curso.
