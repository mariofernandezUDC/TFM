---
tags: [planta, estación]
---

# MPO — Estación de procesado múltiple (TXT 3 y TXT 4)

- **Horno (TXT 3):** plataforma que entra y sale (`M1`), puerta neumática (`O7`), brazo de transferencia con ventosa (`M2`, `O5`, `O6`), luz del horno (`O8`).
- **Mesa giratoria (TXT 4):** gira (`M1`) entre 3 posiciones (brazo, fresa, cinta), fresa (`M2`), cinta de salida (`M3`), empujador neumático (`O7`) y un compresor (`O8`) que da aire a toda la estación.

## E/S — Horno
| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que avisa cuando la pieza esta dentro del horno |
| I2 | Final de carrera | Interruptor que avisa cuando la pieza vuelve a salir del horno |
| I3 | Final de carrera | Interruptor que avisa cuando el brazo llega a la posicion del horno |
| I5 | Barrera de luz (fototransistor) | Fototransistor que detecta la pieza en la entrada del horno |
| O5 | Electroválvula | Valvula de vacio de la ventosa del brazo del horno |
| O6 | Electroválvula | Valvula que hace bajar el brazo del horno |
| O7 | Electroválvula | Valvula que sube la puerta del horno |
| O8 | LED | LED que simula el horno encendido (se enciende mientras "cuece" la pieza) |
| M1 | Motor | Motor que estira o retrae la plataforma que mete la pieza en el horno |
| M2 | Motor | Motor que mueve el brazo del horno |

## E/S — Mesa giratoria
| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que marca cuando la mesa giratoria queda frente al brazo |
| I2 | Final de carrera | Interruptor que marca cuando la mesa giratoria queda frente a la fresa |
| I3 | Final de carrera | Interruptor que marca cuando la mesa giratoria queda frente a la cinta |
| I4 | Barrera de luz (fototransistor) | Fototransistor que detecta la pieza al final de la cinta |
| I5 | Final de carrera | Interruptor que avisa de la posicion del brazo sobre la mesa giratoria |
| O7 | Electroválvula | Cilindro neumatico que empuja la pieza fuera de la mesa giratoria |
| O8 | Compresor | Compresor que da presion de aire a la ventosa del brazo, al brazo del horno, a la puerta del horno y al cilindro de la mesa giratoria |
| M1 | Motor | Motor que gira la mesa giratoria |
| M2 | Motor | Motor de la fresa que mecaniza la pieza |
| M3 | Motor | Motor de la cinta que saca la pieza de esta estacion |

## Código
- Python: `MPO.py`. Unity: `ControladorHornoMPO_mqtt.cs`, `ControladorBrazoMPO_mqtt.cs`, `ControladorTurntableMPO_mqtt.cs`, `ControladorCintaMPO_mqtt.cs` + proxies. Topics `dt/mpo/*`.

## 🔧 Diseño de Mario
- ❓ El compresor del TXT 4 alimenta también la puerta y el brazo del horno (TXT 3): **dependencia neumática entre controladores**. ¿Cómo lo modelas?
