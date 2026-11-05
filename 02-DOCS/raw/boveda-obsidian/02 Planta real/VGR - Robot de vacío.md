---
tags: [planta, estación]
---

# VGR — Robot de vacío (TXT 2)

Robot de 3 ejes con encoder (giro `M1`, vertical `M2`, extensión `M3`) y una **ventosa** (compresor `O7` + válvula `O8`). Mueve las piezas entre las estaciones. Es la estación más compleja junto con el MPO.

## E/S
| Puerto | Tipo | Función |
|---|---|---|
| I1 | Final de carrera | Interruptor que avisa cuando el brazo termina de girar hasta el tope |
| I2 | Final de carrera | Interruptor que avisa cuando el brazo termina de subir |
| I3 | Final de carrera | Interruptor que avisa cuando el brazo termina de retraerse |
| I7 | Barrera de luz (fototransistor) | Fototransistor que detecta la pieza en la salida de la estacion DPS |
| I8 | Sensor de color | Sensor que lee el color de la pieza en la entrada de la estacion DPS |
| O7 | Compresor | Bomba que genera el vacio para que la ventosa agarre la pieza |
| O8 | Electroválvula | Valvula que abre o cierra el paso de vacio hacia la ventosa |
| M1 | Motor con encoder | Motor que gira el brazo hacia cada estacion |
| M2 | Motor con encoder | Motor que sube y baja el brazo |
| M3 | Motor con encoder | Motor que estira y retrae el brazo para alcanzar la pieza |
| C1 | Contador de encoder | Cuenta el giro del brazo, para saber hacia que estacion esta orientado |
| C2 | Contador de encoder | Cuenta la subida y bajada del brazo |
| C3 | Contador de encoder | Cuenta cuanto se ha estirado o retraido el brazo |
| C4 | Contador por barrera de luz | Cuenta las piezas que salen por la estacion DPS |

## Código
- Python: `VGR.py` (38 KB, la más larga), `VGR_Axes1Ref.py`, `Axes1Ref.py` y `Axes2Ref.py` (referenciado y movimiento de ejes).
- Posiciones de destino en `data/FactoryCalibDefault.json` → `VGR.poslist` (DSI, DSO, HBW, MPO, lector de color…).
- Unity: `ControladorVGR_mqtt.cs`, `GripVGR_proxy.cs`. Topics: `dt/vgr/pos`, `dt/vgr/grip`.

## 🔧 Diseño de Mario
- ❓ Cinemática de 3 ejes: pasos de encoder → ángulo y desplazamientos → posición de la ventosa.
- ❓ ¿Cuándo "agarra" la ventosa? (contacto + vacío). Ver [[Neumática - válvula y compresor]].
