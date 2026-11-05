---
tags: [software, mapa]
---

# Mapa del código

```
TFM-UNITY/
├── GUIA_CONEXION.md            ← puesta en marcha con la planta real (Mosquitto 1884, WiFi GII). Fuera del repositorio
└── Github/TFM/                 ← el repositorio (rama de trabajo: planta-virtual)
    ├── 01-TOOLS/               ← herramientas del harness
    ├── 02-DOCS/
    │   ├── wiki/               ← esta bóveda (abrir esta carpeta en Obsidian)
    │   └── raw/boveda-obsidian/  ← copia original de la bóveda, tal como llegó (2026-11-05)
    ├── ROBO Pro Coding/FactoryMain-DigitalTwin.ft  ← lo que realmente se ejecuta en el TXT
    ├── vscode/                 ← copia legible del Python  → [[Lógica de control en Python]]
    │   ├── FactoryMain.py
    │   ├── data/FactoryCalibDefault.json
    │   └── lib/  (controller, HBW, VGR, MPO, DPS, SLD, SSC_*, MQTT, Digital_Twin, Influx_Collector…)
    └── Unity/                  ← gemelo en Unity 6000.3.6f1  → [[Gemelo en Unity]]
        └── Assets/
            ├── Scenes/GemeloDigital_LearningFactory.unity
            ├── Modelo3D/LearningFactory_modelo3D_Unity_v2.fbx
            ├── Scripts/{MovimientoDelGemelo, Interfaz, Simulacion, Trazabilidad}
            └── DatosSimulacion/datosPieza{Blanca,Roja,Azul}.json
```

## Notas
- [[Lógica de control en Python]]: hilos, `controller.py` y cómo se accede al hardware.
- [[Gemelo en Unity]]: modos, scripts y cómo se mueve la escena.
- [[Contrato MQTT]]: los topics que unen las dos partes.
- [[Modo simulación actual]]: el reproductor de grabaciones.
