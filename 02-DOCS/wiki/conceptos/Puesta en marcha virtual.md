---
tags: [concepto]
---

# Puesta en marcha virtual (*virtual commissioning*)

Probar el **software de control real** contra un **modelo simulado de la máquina** antes de tenerla, o sin tenerla. En la industria se hace con un PLC real o emulado y un modelo de la planta (Siemens NX MCD, Process Simulate, Emulate3D, FACTORY I/O…). Las variantes son SIL (*software-in-the-loop*) y HIL (*hardware-in-the-loop*).

## Relación con este TFM
- La lógica de Fischertechnik (o la del alumno) es el "software de control real".
- La planta virtual en Unity es el "modelo de la máquina".
- La [[Librería fischertechnik virtual]] es el enchufe entre los dos → **SIL**.

Justifica [[D001 Sustituir el hardware, no la lógica]].

📚 Para el estado del arte: FACTORY I/O es el referente educativo más parecido. ❓ ¿En qué se diferencia tu planta virtual?
