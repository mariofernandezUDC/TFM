# Log

## [2026-11-11] decision | Sesión 6: lanzador desde Unity (P1–P4a), D008 y D009
- Código fuera de la wiki: `LanzadorPrograma.cs`, `UI/` del dashboard y tests de PlayMode (`f14c4e8` … `651ce47`).
- Nuevas: [D009 Dashboard del alumno](decisiones/D009%20Dashboard%20del%20alumno.md), diario [2026-11-11](diario/2026-11-11.md).
- Actualizadas: D008 (soluciones ocultas), 00 Contexto para Claude (Estado actual), Plan de trabajo (F0 y F5), F5 - Modo programador (F5.4), Rutina Git (Ruido de Unity), Guía - Índice, index.
- Capturas en `02-DOCS/attachments/F5/` (las de 1920×1080 pasan de 1 MB: pendiente).

## [2026-11-06] guía | Cierre de sesión: F0.5 ✅, F0.6 pasos 1–3, nueva prioridad "cargar código"
- Probado a mano: hito 1 (con `maqueta_manual.py`, y el experimento sin `I6` confirma el comportamiento raro nº 1) y F0.5 (desde el Inspector).
- F0.6 pasos 1 a 3 (`3b95fb7`, `34ce739`, `eb10ee5`; los pasos 2 y 3 los hizo Codex): `SLD.py` clasifica las tres piezas solas. Sensor rojo +15 mm en X solo en `PlantaVirtual.unity` (`e765709`). GIL: p95 de 105 a 5 ms con `sys.setswitchinterval(0.001)` (`cf1a692`), y deja a la vista el sesgo de la roja (−8,4 mm).
- Mensaje del tutor: la próxima prioridad es el prototipo de "cargar código". F0.8 pasa a versión corta.
- Decisión: D003 ampliada con "Capturas para la memoria". Nueva carpeta `02-DOCS/attachments/F0/` (3 PNG y 3 CSV).
- Actualizadas: D003, diario 2026-11-06, guía F0 (hito 1, F0.5 ✅, F0.6 🟡, F0.8), Preguntas para el tutor, 00 Contexto para Claude.

## [2026-11-06] guía | F0.5 código hecho y medido; F0.7 cerrado
- Código nuevo fuera de la wiki: `Unity/Assets/Scripts/PlantaVirtual/VirtualIO_TXT.cs`, escena `PlantaVirtual.unity`, `virtual/medir_vio.py`.
- Decisiones: D006.8 en [D006](decisiones/D006%20Protocolo%20vio.md); [D007](decisiones/D007%20Qui%C3%A9n%20mueve%20la%20escena.md).
- Actualizadas: guía F0 (F0.5 🟡, F0.7 ✅), SLD - Clasificadora (posiciones), Protocolo vio, README de `virtual/`, diario 2026-11-06, 00 Contexto para Claude.

## [2026-11-06] guía | F0.4 cerrado: librería virtual mínima y hito 1
- Código nuevo fuera de la wiki: `virtual/` (librería `fischertechnik` virtual, `run_sld_virtual.py`, `maqueta_manual.py`, tests, README).
- Decisión: D006.7 en [D006](decisiones/D006%20Protocolo%20vio.md) (sin entradas frescas no se envía `out`).
- Actualizadas: Librería fischertechnik virtual, Protocolo vio, D006, guía F0 (F0.4 ✅, hito 1, aviso en F0.5), diario 2026-11-06, 00 Contexto para Claude.

## [2026-11-06] guía | F0.1 cerrado: diagrama de estados de la SLD
- Actualizadas: SLD - Clasificadora (sección "Diagrama de estados"), guía F0 (F0.1 ✅ y respuestas de comprobación), 00 Contexto para Claude.
- Nuevo: diario [2026-11-06](diario/2026-11-06.md).

## [2026-11-05] decision | D006.5 latido y D006.6 prueba sin Unity
- Cierran los dos puntos abiertos de [D006](decisiones/D006%20Protocolo%20vio.md).
- Actualizadas: Protocolo vio (latido, `maqueta_manual.py`), guía F0 (F0.3, tarea 4 de F0.4, hito 1, comprobación de F0.5), diario 2026-11-05, 00 Contexto para Claude.

## [2026-11-05] decision | D006 protocolo vio (TXT 5)
- Nuevas: [D006 Protocolo vio](decisiones/D006%20Protocolo%20vio.md), [Protocolo vio](conceptos/Protocolo%20vio.md).
- Actualizadas: guía F0 (F0.3 ✅, hito 1), Contador por pulsos (enlace a D006.2), diario 2026-11-05, 00 Contexto para Claude.
- Dos puntos abiertos apuntados en D006 (latido de Python; prueba a mano con la regla de 0,5 s).

## [2026-11-05] sweep | 48 ingested, 0 failed, 51 articles touched
- Ingested: bóveda de Obsidian (48 notas) `inbox/` → `raw/boveda-obsidian/` (copia verificada por SHA-256) → compilada en `wiki/` ([index](index.md)).
- Carpetas renombradas: `01 Proyecto`→`proyecto/`, `02 Planta real`→`planta/`, `03 Software existente`→`software/`, `04 Conceptos`→`conceptos/`, `05 Decisiones`→`decisiones/`, `06 Diario`→`diario/`, `07 Guía de desarrollo`→`guia/`, `08 Memoria`→`memoria/`, `99 Plantillas`→`plantillas/`. `00 Inicio` → `index.md`. Nombres de nota sin cambios.
- Fusión: tablas de E/S de SSC, HBW, VGR, MPO y SLD → fuente única en [Inventario de E-S](planta/Inventario%20de%20E-S.md) (72 filas comprobadas).
- Revisada [D005](decisiones/D005%20Gesti%C3%B3n%20del%20repositorio.md) punto 9. Nuevo diario [2026-11-05](diario/2026-11-05.md).
- No ingerido: `inbox/.obsidian/` (configuración local de Obsidian). Borrado bloqueado por el guard; pendiente de decisión.
