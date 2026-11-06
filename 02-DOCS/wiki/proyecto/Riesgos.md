---
tags: [proyecto, riesgos]
---

# Riesgos y decisiones abiertas

| Riesgo | Por qué importa | Mitigación |
|---|---|---|
| **Sondeo con retraso** | La lógica original mueve un eje y espera en bucle a que se cierre un [[Final de carrera]]. Si la E/S llega tarde, el eje se pasa | [[Imagen de E-S]] a ~100 Hz + topes mecánicos en la simulación. Ver [[Hilos y sondeo (polling)]] |
| **Pulsos perdidos** | La [[SLD - Clasificadora]] cuenta pulsos de un final de carrera **leyendo su estado en bucle**. Si los pulsos son más rápidos que la imagen de E/S, se pierden. Ver [[Contador por pulsos]] | ❓ ¿Qué frecuencia de pulsos da la cinta? 🔧 Mario. Opciones de software en [[Contador por pulsos]] |
| **Semántica del firmware** | `set_distance`, `start_sync`… hay que reproducirlos tal cual. Ver [[Motor con encoder]] | Documentación de Fischertechnik + trazas reales (F7) |
| **Calibración** | `FactoryCalibDefault.json` está en pasos de encoder; la escena en metros | 🔧 Tabla pasos ↔ mm/grados por eje (Mario) |
| **Doble fuente de movimiento** | Hoy los `*_mqtt.cs` animan a partir de `dt/*` (ver [[Marioneta vs planta]]). La planta virtual también moverá la escena | ✅ Decidido en [[D007 Quién mueve la escena]]: en modo virtual manda la planta. `PlantaVirtual.unity` no lleva las marionetas de la SLD. Pendiente para F2: el VGR lee propiedades de `ControladorCilindrosSLD_mqtt` |
| **Bucles sin timeout** | El código original espera indefinidamente (p. ej. `while not isEjectionTriggered()`). Un sensor fallido lo cuelga | Es *didáctico*: se puede usar como ejercicio de fallos (F4) |
| **Licencia** | `lib/` es código de Fischertechnik | Comprobar si se puede redistribuir a los alumnos |
| **Alcance** | 72 E/S y 6 estaciones | Priorizar F0–F4 |
