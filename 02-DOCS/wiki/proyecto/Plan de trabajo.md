---
tags: [proyecto, plan]
---

# Plan de trabajo

| Fase   | Contenido                                                                                                                                                                                  | Criterio de "hecho"                                        | Estado |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------- | ------ |
| **F0** | [[F0 - Prueba de concepto SLD]]: la clasificadora de extremo a extremo                                                                                                                     | `SLD.py` original clasifica piezas virtuales               | ⏳      |
| **F1** | Librería virtual completa, protocolo `vio/*`, simulación de display, NFC y nube                                                                                                            | `FactoryMain.py` arranca en el PC contra la planta virtual | ⏳      |
| **F2** | Modelos por estación: [[SLD - Clasificadora]] → [[HBW - Almacén]] → [[VGR - Robot de vacío]] → [[MPO - Horno y mesa giratoria]] → [[DPS - Entrada y salida]] / [[SSC - Cámara y semáforo]] | Cada estación completa su ciclo por separado               | ⏳      |
| **F3** | Flujo completo de pieza con el control original                                                                                                                                            | Blanca, roja y azul hacen el recorrido completo            | ⏳      |
| **F4** | Modo operador: panel de simulación, reset, velocidad, inyección de fallos                                                                                                                  | Cada fallo provoca la reacción esperada                    | ⏳      |
| **F5** | Modo programador: plantilla, ejemplos, monitor de E/S                                                                                                                                      | Un alumno completa los ejercicios con la guía              | ⏳      |
| **F6** | Empaquetado ([[D004 Paquete autónomo para el alumno]])                                                                                                                                     | Funciona en un PC limpio                                   | ⏳      |
| **F7** | Validación frente a la planta real                                                                                                                                                         | Desviación cuantificada                                    | ⏳      |

Detalle paso a paso: [[Guía - Índice]]. Riesgos: [[Riesgos]].

## Estimación orientativa
450–750 h para el plan completo, más la memoria. F0–F4 es el núcleo del TFM. Ver [[Preguntas para el tutor]].
