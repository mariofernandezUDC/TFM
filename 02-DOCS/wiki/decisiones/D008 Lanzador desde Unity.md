---
tags: [decisión, F5]
---

# D008 · Lanzador desde Unity

- **Fecha:** 2026-11-11 · **Estado:** ✅ Aceptada (prototipo pedido por el tutor; se construye en los pasos P1–P6 del [[Encargo - Lanzador desde Unity]])

## Contexto
Hoy, para ver su programa en la planta virtual, el alumno tiene que hacer tres cosas por separado ([[F5 - Modo programador]], F5.1):
1. tener Mosquitto en marcha (servicio de Windows, puerto 1883);
2. entrar en Play en Unity;
3. escribir en PowerShell `py -3.12 virtual/run_virtual.py --programa <ruta>`.

Al probar los niveles 1 y 2 como alumno, Mario vio que es demasiado complejo ([[2026-11-07]]). El tutor pidió un prototipo de «cargar código». Lo que se quiere es: **elegir el programa en un desplegable dentro de Unity, pulsar Cargar y verlo funcionar; pulsar Parar y ver los errores en la propia planta.**

Hay que decidir cuatro cosas: cómo ejecuta Unity el Python (A), qué broker se usa (B), cómo se elige el programa (C) y cómo se para (D).

## Opciones

### A. Cómo ejecuta Unity el programa del alumno
- A1. **Proceso aparte.** Unity lanza `run_virtual.py --programa <ruta>` con `System.Diagnostics.Process`, como si lo escribiera el alumno en PowerShell.
- A2. **Python dentro de Unity** (pythonnet): el intérprete de Python vive en el mismo proceso que Unity.

### B. Broker MQTT
- B1. **El servicio Mosquitto del PC en 1883**, como ahora.
- B2. **`mosquitto.exe` portable** que lanza Unity en un **puerto propio (18830)**.
- B3. **MQTTnet dentro de Unity** (broker en C#).
- B4. **Un broker escrito en Python.**

### C. Cómo se elige el programa
- C1. **Desplegable** con los `.py` de `virtual/alumno/`.
- C2. **Diálogo de «Abrir archivo».**

### D. Cómo se para
- D1. **Matar el proceso.**
- D2. **Parada limpia** con un mensaje `vio/sim/parar` que el lanzador de Python atiende.

## Decisión
- **A → A1. Proceso aparte.** Unity ejecuta `run_virtual.py --programa <ruta>` con `System.Diagnostics.Process`. Entre los dos procesos solo hay dos canales: **`vio` por MQTT** ([[D006 Protocolo vio]]) y la **salida estándar** del proceso, que Unity lee para enseñar mensajes y errores.
- **B → B1 ahora y B2 en P6.** De momento, el servicio Mosquitto en 1883. En P6, un `mosquitto.exe` portable lanzado por Unity en el **puerto 18830**, para no chocar con otro Mosquitto que tenga el alumno. P6 necesita la confirmación de Mario (binarios en el repo y licencia EPL-2.0/EDL).
- **C → C1. Desplegable.** Lista los `.py` de `virtual/alumno/`, buscados **de forma recursiva** y **excluyendo `pistas/`**. Cada entrada se ve con su ruta relativa, por ejemplo `ejercicios/nivel1_arrancar.py`.
- **D → D1. Parar = matar el proceso.** La parada limpia con `vio/sim/parar` (D2) queda para más adelante.

## Por qué
- **A1 mantiene el aislamiento de [[D001 Sustituir el hardware, no la lógica]].** El programa del alumno corre igual que en el TXT: un proceso de Python que solo habla con la planta por E/S. Unity no sabe qué hay dentro; solo ve `vio`. Si Python va dentro de Unity (A2), la lógica y la planta comparten proceso y ese aislamiento se rompe.
- **Con A2 no se puede parar un programa colgado.** Un hilo de Python no se puede matar desde fuera. Si el alumno escribe un `while True:` sin salida, con Python dentro de Unity la única forma de pararlo sería cerrar Unity entero. Con un proceso aparte (A1) basta con matar ese proceso, y Unity sigue en marcha.
- **A1 sirve también si el tutor elige PLC.** Si los alumnos programan en PLC en vez de Python ([[Preguntas para el tutor]]), Unity lanzaría otro proceso que hable `vio`. La arquitectura no cambia.
- **B1 ya funciona hoy.** Es lo que se usa desde F0. B2 hace que el paquete del alumno sea autónomo ([[D004 Paquete autónomo para el alumno]]) y el puerto 18830 evita chocar con un Mosquitto que el alumno ya tenga en 1883.
- **B3 y B4 se descartan.** B3 mete otra DLL en Unity, y ahora solo está el cliente M2Mqtt (`Unity/Assets/Plugins/M2Mqtt.Net.dll`). B4 sería un broker hecho a mano, sin la fiabilidad de Mosquitto.
- **C1 funciona en el ejecutable.** Un ejecutable de Unity **no tiene diálogo de abrir archivo sin plugins** (solo existe en el editor). El desplegable funciona igual en el editor y en la build. Se excluye `pistas/` porque allí solo hay `.md`, y así ninguna pista futura en `.py` aparece como programa.
- **D1 es seguro sin tocar Python.** Al matar el proceso se corta el latido y Unity pasa a **estado seguro en 0,5 s**: motor parado y válvulas cerradas ([[D006 Protocolo vio]], D006.5). La seguridad no depende de que el programa del alumno colabore.

## Consecuencias
- **Ruta de Python:** Unity no lanza `py -3.12`. `py.exe` abre `python.exe` como proceso hijo, y al matar `py` el hijo puede quedarse vivo con la cinta en marcha. Unity lanza directamente el `python.exe` real (campo `rutaPython`, paso P2).
- **Python huérfano:** al salir de Play, Unity tiene que matar el proceso si sigue vivo.
- **Hilos:** la salida estándar llega a Unity en otro hilo. Se guarda en una cola y se lee desde `Update()`; desde ese hilo no se toca ningún objeto de Unity.
- **Errores:** el panel puede enseñar el bloque que ya imprime `mostrar_error` en `run_virtual.py` (entre dos líneas de `=`). No hace falta cambiar `run_virtual.py`.
- **Código de salida:** 0 = el programa terminó bien, 1 = error del alumno, 2 = no se encuentra el programa.
- **D1 no avisa al programa:** el alumno no puede hacer nada al parar (por ejemplo, un `finally` suyo no se ejecuta). Lo cubre el estado seguro de Unity.
- **Raíz del repo:** en el editor se calcula desde `Application.dataPath`. En la build cambia, y se resuelve en F6 ([[D004 Paquete autónomo para el alumno]]).
- El comando de PowerShell sigue funcionando: queda como plan B.

## Pendientes
- [ ] P6: `mosquitto.exe` portable en 18830 (pedir confirmación a Mario y revisar la licencia).
- [ ] Parada limpia con `vio/sim/parar` (D2).
- [ ] F6: raíz del repo y ruta de Python en la build.

Relacionado: [[D001 Sustituir el hardware, no la lógica]], [[D004 Paquete autónomo para el alumno]], [[D006 Protocolo vio]], [[F5 - Modo programador]], [[Encargo - Lanzador desde Unity]].
