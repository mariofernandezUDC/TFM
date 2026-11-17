---
tags: [proyecto, plan]
---

# Plan de trabajo

| Fase   | Contenido                                                                                                                                                                                  | Criterio de "hecho"                                        | Estado |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------- | ------ |
| **F0** | [[F0 - Prueba de concepto SLD]]: la clasificadora de extremo a extremo                                                                                                                     | `SLD.py` original clasifica piezas virtuales               | 🟡 en pausa |
| **F1** | Librería virtual completa, protocolo `vio/*`, simulación de display, NFC y nube                                                                                                            | `FactoryMain.py` arranca en el PC contra la planta virtual | ⏳      |
| **F2** | Modelos por estación: [[SLD - Clasificadora]] → [[HBW - Almacén]] → [[VGR - Robot de vacío]] → [[MPO - Horno y mesa giratoria]] → [[DPS - Entrada y salida]] / [[SSC - Cámara y semáforo]] | Cada estación completa su ciclo por separado               | ⏳      |
| **F3** | Flujo completo de pieza con el control original                                                                                                                                            | Blanca, roja y azul hacen el recorrido completo            | ⏳      |
| **F4** | Modo operador: panel de simulación, reset, velocidad, inyección de fallos                                                                                                                  | Cada fallo provoca la reacción esperada                    | ⏳      |
| **F5** | Modo programador: plantilla, ejemplos, monitor de E/S                                                                                                                                      | Un alumno completa los ejercicios con la guía              | 🚧      |
| **F6** | Empaquetado ([[D004 Paquete autónomo para el alumno]])                                                                                                                                     | Funciona en un PC limpio                                   | ⏳      |
| **F7** | Validación frente a la planta real ([[D011 Validación con la planta real]])                                                                                                                                                         | Desviación cuantificada                                    | 🚧 adelantada |

Detalle paso a paso: [[Guía - Índice]]. Riesgos: [[Riesgos]].

## Estado real (2026-11-17)
- **F0 · 🟡 en pausa** desde el 2026-11-06, porque el tutor priorizó «cargar código».
  - ✅ F0.1 (diagrama de estados), F0.3 (protocolo `vio`), F0.4 (librería virtual, hito 1), F0.5 (puente de E/S en Unity), F0.7 (quién mueve la escena).
  - 🟡 F0.6 (modelo físico): `SLD.py` clasifica las tres piezas; faltan las medidas reales de la maqueta 🔧.
  - ⏳ F0.2 (entorno), F0.8 (prueba integrada, versión corta) y F0.9 (cierre).
- **F5 · 🚧 en curso** (se adelantó a F1–F4 por petición del tutor).
  - ✅ F5.1 cargar código por comando y F5.2 ejercicios por niveles (2026-11-07).
  - ⏳ F5.3 programas de ROBO Pro Coding (prueba 🔧 de Mario).
  - 🚧 F5.4 cargar desde Unity ([[D008 Lanzador desde Unity]], [[D009 Dashboard del alumno]]): P1–P3 ✅, P4a ✅; P4b, P4c, P5 y P6 ⏳.
  - Detalle: [[F5 - Modo programador]].

- **F7 · 🚧 en curso**, adelantada el 2026-11-17 por petición del tutor ([[Reunión tutor 2026-11-17 - Acta]]).
  - ✅ Captura real con FactoryMain: 21 piezas con trazas `dt/sld/*` (blancas 5/5, azules 5/5, rojas 7/11 en su rampa).
  - ✅ Prueba del alumno en la maqueta: `soluciones/nivel4.py` **9/9 con umbral 1460** (con 1400, rojas 0/3).
  - ✅ Causa de las rojas encontrada: umbral 1460 en `set_calib_data_SLD()` frente a 1400 en los ejercicios ([[D012 Umbral de color y sensor de color virtual realista]], propuesta).
  - ⏳ Sin maqueta: V3 (misma prueba en la virtual), V5 (comparación), modo «alumno» en `a_eventos.py` (sin evento de cilindro), decidir D012, calibración con medidas físicas.
  - ⏳ Con maqueta 🔧: medidas con regla, vídeo a 60 fps, traza del programa del alumno (no se conectó al broker: averiguar por qué).
  - ⏳ Repetir la batería de `virtual/tests` con Unity parado (9 fallos con Unity en Play en el 1883).
  - Detalle: [[D011 Validación con la planta real]], [[2026-11-17]].

## Estimación orientativa
450–750 h para el plan completo, más la memoria. F0–F4 es el núcleo del TFM. Ver [[Preguntas para el tutor]].
