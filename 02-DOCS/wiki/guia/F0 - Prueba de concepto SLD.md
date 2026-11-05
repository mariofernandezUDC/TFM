---
tags: [guía, F0]
---

# F0 · Prueba de concepto: la clasificadora (SLD)

> **Meta:** el `SLD.py` **original, sin cambiar una línea**, clasifica piezas virtuales en Unity. Blanca → rampa blanca, roja → roja, azul → azul.
> Estación: [[SLD - Clasificadora]] · Arquitectura: [[Arquitectura de la planta virtual]]

---

## F0.1 · Entender el circuito actual · ⏳
- **Objetivo:** saber explicar de memoria qué hace la SLD y qué E/S usa.
- **Qué hacer:**
  1. Lee `vscode/lib/SLD.py`, función `thread_SLD` (líneas ~39–127) y las funciones `eject*`, `is*`, `readColorValue`.
  2. 🔧 Dibuja el **diagrama de estados** de la SLD (reposo → clasificando → expulsando → comprobando → reposo / error). Hazlo a mano o con Mermaid en esta nota.
  3. Para cada línea que toque hardware, anota qué E/S es y qué significa físicamente.
  4. Lee `Digital_Twin.py` (bloques `dt/sld/belt` y `dt/sld/cylinder`) y `ControladorCintaSLD_mqtt.cs` para ver cómo llega a Unity.
- **❓ Comprobación:** ¿por qué la SLD se queda con el **mínimo** de tensión del sensor de color y no con la primera lectura? ¿Qué pasa si la pieza nunca llega a `I3`?
- **Entregable:** el diagrama de estados añadido a [[SLD - Clasificadora]].

## F0.2 · Preparar el entorno · ⏳
- **Qué hacer:**
  1. Unity Hub con la versión **6000.3.6f1** → abrir `Github/TFM/Unity`.
  2. Comprobar que el **modo Simulación** actual funciona (pedir una pieza roja y verla pasar por la SLD).
  3. Instalar Python 3.11+ en el PC, `pip install paho-mqtt`.
  4. Mosquitto local (ya está en `GUIA_CONEXION.md`) y **MQTT Explorer**.
  5. 💻 Git: seguir la [[Rutina Git]], apartado A (comprobar identidad, crear la rama `planta-virtual`). ✅ La rama ya existe (en local y en `origin`). Reglas: [[D005 Gestión del repositorio]]. Tu escena de trabajo será `PlantaVirtual.unity`: no se toca la de Adrián.
- **Comprobación:** con el modo Simulación en marcha, ves los mensajes `dt/sld/*` en MQTT Explorer… ❓ ¿Los ves? ¿Por qué sí o por qué no? (Pista: `SimuladorOffline` inyecta los mensajes *dentro* de Unity.)
- **Entregable:** commit "rama planta-virtual creada" + diario.

## F0.3 · Diseñar el protocolo de E/S (solo TXT 5) · ⏳
- **Objetivo:** definir los mensajes entre la [[Librería fischertechnik virtual]] y Unity. Ver [[Imagen de E-S]].
- **Qué hacer (💻 te propongo opciones y decides tú):**
  - Topics: `vio/txt5/in` (Unity → Python: I1, I2 en voltios, I3, I6, I7, I8 y el estado de C1) y `vio/txt5/out` (Python → Unity: M1 velocidad, sentido y marcha; O5–O8).
  - Formato JSON, tasa de refresco y qué pasa si Unity no está conectado.
- **❓** ¿Un mensaje con toda la imagen o un topic por cada E/S? Ventajas e inconvenientes.
- **Entregable:** nota nueva `conceptos/Protocolo vio.md` + decisión `D006` (D005 ya es la gestión del repositorio).

## F0.4 · Librería virtual mínima · ⏳
- **Objetivo:** que `import lib.controller` funcione en el PC sin hardware.
- **Qué hacer:**
  1. 💻 Crear `Github/TFM/virtual/fischertechnik/…` con las fábricas (`init_*`, `create_*`). Todas las E/S de los 6 TXT se crean, pero **solo las de la SLD se conectan** a MQTT; el resto son "tontas".
  2. Simular lo que importa `SLD.py`: `fischertechnik.controller.Motor`, `fischertechnik.mqtt.Constants` y `FTCloudClient` (desactivado), `lib.display` y los niveles de log propios (`TRACE`, `TRACE0`…).
  3. Script `run_sld_virtual.py` que solo lanza `thread_SLD`.
- **Comprobación:** sin Unity, publicas a mano en MQTT Explorer `I1 = oscuro` y ves en la consola que la SLD arranca la cinta (`vio/txt5/out` con M1 = 512).
- **❓** ¿Por qué hay que crear también las E/S de los otros TXT aunque no se usen? (Pista: `lib/controller.py`.)
- **Entregable:** commit + nota de lo aprendido.

## F0.5 · Puente de E/S en Unity · ⏳
- **Objetivo:** un componente de Unity que reciba `vio/txt5/out` y publique `vio/txt5/in`.
- **Dónde:** carpeta nueva `Assets/Scripts/PlantaVirtual/` → `VirtualIO_TXT.cs` (💻 te explico cómo reutilizar la conexión de `MQTTClient.cs`).
- **Comprobación:** en el Inspector se ven las salidas cambiar cuando la SLD manda, y las entradas se pueden cambiar a mano.

## F0.6 · Modelo físico de la SLD · ⏳ · 🔧 **Diseño de Mario**
- **Objetivo:** que la cinta, la pieza, los sensores y los cilindros se comporten como los reales.
- **Preguntas que debes resolver tú** (ver [[SLD - Clasificadora]]):
  - Cinta: velocidad del motor → mm/s. Cómo arrastra la pieza.
  - Pulsos de `C1`: cuántos por mm y a qué frecuencia ([[Contador por pulsos]]).
  - Posiciones de `I1`, `I2`, `I3`, de los 3 cilindros y de las barreras de las rampas.
  - Cilindros: tiempos y la condición compresor + válvula ([[Neumática - válvula y compresor]]).
  - [[Sensor de color]]: tensión por color + ruido.
- **💻 Claude:** te ayuda con la parte de Unity (raycasts, triggers, cómo mover un Transform cada frame), no con los valores ni la cinemática.
- **Dónde:** `Assets/Scripts/PlantaVirtual/SLD_Planta.cs` (y los componentes reutilizables que decidas: `BarreraLuzVirtual.cs`, `CilindroVirtual.cs`…).
- **❓** ¿Qué componentes de la SLD se podrán reutilizar en otras estaciones?

## F0.7 · Decidir quién mueve la escena · ⏳
- **Problema:** `ControladorCintaSLD_mqtt.cs` y `ControladorCilindrosSLD_mqtt.cs` también mueven la cinta y las piezas (son marionetas, ver [[Marioneta vs planta]]).
- **Opciones:** (a) en modo virtual se desactivan y manda la planta; (b) la planta solo calcula E/S y la animación sigue entrando por `dt/*`.
- **❓** ¿Qué pasa con las piezas "de repuesto" que crean estos scripts si los dejas activos?
- **Entregable:** decisión `D007`.

## F0.8 · Prueba integrada · ⏳
- **Casos:**
  - [ ] Pieza blanca → rampa blanca, estado vuelve a 1.
  - [ ] Pieza roja → rampa roja.
  - [ ] Pieza azul → rampa azul.
  - [ ] Tensión fuera de rango → error SLD (estado 4).
  - [ ] Fallo: `I3` bloqueado → la SLD se queda esperando (sin timeout). ❓ ¿Cómo lo arreglarías *sin* tocar `SLD.py`? ¿Y tocándolo?
  - [ ] Fallo: compresor que no da presión → el cilindro no empuja → la comprobación de la rampa da error.
- **Medir:** tiempo de ciclo virtual frente a una grabación real (`datosPieza*.json`). Es el primer dato para F7.

## F0.9 · Cierre de la fase · ⏳
- [ ] Diario de la sesión.
- [ ] Actualizar [[Plan de trabajo]] y [[00 Contexto para Claude]].
- [ ] Apuntar lo aprendido y detallar la guía de F1.
