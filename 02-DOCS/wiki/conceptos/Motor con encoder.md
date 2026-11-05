---
tags: [concepto, componente]
---

# Motor con encoder

Motor de 9 V con un encoder que da pulsos al girar. El TXT cuenta esos pulsos en un **contador** (`motor_step_counter`) y así conoce la posición del eje.

## API usada (ver [[Librería fischertechnik virtual]])
- `set_speed(v, Motor.CW/CCW)`: velocidad 0–512 y sentido.
- `start()` / `stop()`: marcha y paro.
- `set_distance(n)` + `start_sync()` / `stop_sync()`: mover **n pasos** y parar solo (lo hace el firmware). `is_running()` dice si sigue moviéndose.
- Contador: `get_count()`, `set_motor()`.

## Modelo en la planta virtual
🔧 **Diseño de Mario.** Preguntas guía:
- ❓ ¿Relación velocidad (0–512) → pasos/s? ¿Es lineal? ¿Hay rampa de aceleración?
- ❓ ¿Relación pasos → mm (o grados) del eje? Ver la calibración en `FactoryCalibDefault.json`.
- ❓ ¿Qué pasa al llegar a un tope mecánico? (El eje se para, pero el motor sigue "encendido".)
- ❓ ¿Cómo se comporta el contador al cambiar de sentido?

💻 La parte de software (qué hace exactamente `set_distance` en el firmware) se documenta en F1.

Usado en: [[VGR - Robot de vacío]], [[HBW - Almacén]], [[SLD - Clasificadora]], [[SSC - Cámara y semáforo]].
