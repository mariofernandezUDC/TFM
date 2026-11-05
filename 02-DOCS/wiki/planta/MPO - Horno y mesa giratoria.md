---
tags: [planta, estación]
---

# MPO — Estación de procesado múltiple (TXT 3 y TXT 4)

- **Horno (TXT 3):** plataforma que entra y sale (`M1`), puerta neumática (`O7`), brazo de transferencia con ventosa (`M2`, `O5`, `O6`), luz del horno (`O8`).
- **Mesa giratoria (TXT 4):** gira (`M1`) entre 3 posiciones (brazo, fresa, cinta), fresa (`M2`), cinta de salida (`M3`), empujador neumático (`O7`) y un compresor (`O8`) que da aire a toda la estación.

## E/S — Horno

Tabla en el inventario (fuente única, no se duplica aquí):

![[Inventario de E-S#MPO – Horno (TXT 3)]]

## E/S — Mesa giratoria

Tabla en el inventario (fuente única, no se duplica aquí):

![[Inventario de E-S#MPO – Mesa giratoria, fresa y cinta (TXT 4)]]

## Código
- Python: `MPO.py`. Unity: `ControladorHornoMPO_mqtt.cs`, `ControladorBrazoMPO_mqtt.cs`, `ControladorTurntableMPO_mqtt.cs`, `ControladorCintaMPO_mqtt.cs` + proxies. Topics `dt/mpo/*`.

## 🔧 Diseño de Mario
- ❓ El compresor del TXT 4 alimenta también la puerta y el brazo del horno (TXT 3): **dependencia neumática entre controladores**. ¿Cómo lo modelas?
