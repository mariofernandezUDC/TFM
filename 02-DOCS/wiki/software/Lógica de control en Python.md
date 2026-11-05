---
tags: [software, python]
---

# Lógica de control en Python (TXT 4.0)

## Arranque: `FactoryMain.py`
1. Inicializa el log, las luces, el referenciado de ejes (`Axes1Ref`, `Axes2Ref`), el NFC y la configuración MQTT.
2. Lanza **hilos daemon** de comunicación: `thread_ftCloud`, `thread_Local` (MQTT local), `thread_DigitalTwin` (publica `dt/*`) y `thread_InfluxCollector`.
3. Lanza **un hilo por estación**: `init_SSC_PTU`, `thread_HBW`, `thread_VGR`, `thread_MPO`, `thread_DPS`, `thread_SLD`.
4. Carga la calibración (`loadFileFactoryCalib`).

Ver [[Hilos y sondeo (polling)]].

## Acceso al hardware: `lib/controller.py`
Es "el plano de cableado escrito en Python": crea cada sensor y actuador con `fischertechnik.factories` y le da un nombre (`TXT_SLD_E5_I1_photo_transistor`, etc.). **Es la especificación de la planta virtual**: ver [[Inventario de E-S]].

## API de Fischertechnik que se usa
Unos 30 métodos sobre 12 tipos de componente. La lista completa está en [[Librería fischertechnik virtual]]. Es pequeña, y por eso es viable [[D001 Sustituir el hardware, no la lógica]].

## Estaciones
[[HBW - Almacén]] · [[VGR - Robot de vacío]] · [[MPO - Horno y mesa giratoria]] · [[DPS - Entrada y salida]] · [[SLD - Clasificadora]] · [[SSC - Cámara y semáforo]]

## Publicación al gemelo: `Digital_Twin.py`
- `thread_DigitalTwin_fast`: posiciones de ejes (VGR, HBW, cámara…), con filtro de saltos.
- `thread_DigitalTwin_slow`: estados de cintas, horno, cilindros, sensores, stock…
- Responde `dt/ping` → `dt/pong` para medir la latencia.
Ver [[Contrato MQTT]].
