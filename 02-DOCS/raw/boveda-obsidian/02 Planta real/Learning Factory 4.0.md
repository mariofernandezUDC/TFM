---
tags: [planta, mapa]
---

# Learning Factory 4.0 (Fischertechnik)

Maqueta de fábrica didáctica con **6 controladores TXT 4.0** (1 maestro + 5 esclavos), motores de 9 V, sensores y neumática. Todo el hardware está en [[Inventario de E-S]].

## Estaciones
| Estación | Qué hace | TXT |
|---|---|---|
| [[SSC - Cámara y semáforo]] | Cámara con pan/tilt, semáforo de estado, sensores ambientales | 0 (maestro) |
| [[HBW - Almacén]] | Almacén automático de 9 huecos con transelevador | 1 |
| [[VGR - Robot de vacío]] | Robot de 3 ejes con ventosa que mueve piezas entre estaciones | 2 |
| [[DPS - Entrada y salida]] | Entrada y salida de piezas, lector de color y NFC (usa las E/S del TXT 2) | 2 |
| [[MPO - Horno y mesa giratoria]] | Horno, brazo de transferencia, mesa giratoria con fresa y cinta | 3 y 4 |
| [[SLD - Clasificadora]] | Cinta que lee el color y expulsa cada pieza a su rampa | 5 |

## Recorrido típico de una pieza (pedido)
```
HBW (almacén) → VGR → MPO (horno → mesa → fresa → cinta) → SLD (clasifica) → VGR → DPS (salida)
```
🔧 ❓ Cuando tengas la maqueta delante, cronometra cada tramo. Esos tiempos son tu referencia para validar (F7).

## Piezas
Hay 3 tipos: **blanca, roja y azul**. Cada pieza lleva una etiqueta NFC. Prefabs en Unity: `Assets/Prefabs/9V_nfc_*`.
