---
tags: [planta, estación]
---

# HBW — Almacén automático (TXT 1)

Almacén de 9 huecos (3×3) con un transelevador de 2 ejes con encoder (horizontal `M2`, vertical `M4`), una horquilla que se extiende y se retrae (`M3`, con finales de carrera) y una cinta de entrada y salida (`M1`).

## E/S
| Puerto | Tipo | Función |
|---|---|---|
| I1 | Barrera de luz (fototransistor) | Fototransistor que detecta una pieza llegando por fuera del almacen |
| I4 | Barrera de luz (fototransistor) | Fototransistor que detecta una pieza dentro del almacen |
| I5 | Final de carrera | Interruptor que avisa cuando el brazo del almacen llega a la posicion de coger la pieza |
| I6 | Final de carrera | Interruptor que avisa cuando el brazo del almacen termina de retraerse |
| I7 | Final de carrera | Interruptor que avisa cuando el brazo del almacen termina de extenderse |
| I8 | Final de carrera | Interruptor que avisa cuando el brazo del almacen termina de subir |
| M1 | Motor | Motor de la cinta que mete y saca piezas del almacen |
| M2 | Motor con encoder | Motor que mueve el brazo del almacen de lado a lado, para elegir la columna del estante |
| M3 | Motor | Motor que estira y retrae el brazo del almacen para coger o dejar la pieza |
| M4 | Motor con encoder | Motor que sube y baja el brazo del almacen, para elegir la fila del estante |
| C2 | Contador de encoder | Cuenta el movimiento lateral del brazo, para saber en que columna esta |
| C4 | Contador de encoder | Cuenta la subida y bajada del brazo, para saber en que fila esta |

## Código
- Python: `HBW.py`, `HBW_AxesNRef.py` (referenciado de ejes), `HBW_Storage.py` (inventario), `HBW_MQTT.py`.
- Unity: `ControladorHBWposition_mqtt.cs`, `ControladorCintaHBW_mqtt.cs`, `ControladorSpawnPiecesHBW_mqtt.cs`, proxies `BrazoHBW`, `CintaHBW`, `ContenedorHBW`, `SoporteHBW`.
- Topics: `dt/hbw/pos`, `dt/hbw/belt`, `f/i/stock`. Ver [[Contrato MQTT]].

## 🔧 Diseño de Mario
- ❓ Posición de cada hueco en pasos de encoder ↔ coordenadas en la escena.
- ❓ Topes mecánicos de cada eje.
