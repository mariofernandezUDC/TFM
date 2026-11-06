---
tags: [guía, F0]
---

# F0 · Prueba de concepto: la clasificadora (SLD)

> **Meta:** el `SLD.py` **original, sin cambiar una línea**, clasifica piezas virtuales en Unity. Blanca → rampa blanca, roja → roja, azul → azul.
> Estación: [[SLD - Clasificadora]] · Arquitectura: [[Arquitectura de la planta virtual]]

---

## F0.1 · Entender el circuito actual · ✅ 2026-11-06
- **Objetivo:** saber explicar de memoria qué hace la SLD y qué E/S usa.
- **Qué hacer:**
  1. Lee `vscode/lib/SLD.py`, función `thread_SLD` (líneas ~39–127) y las funciones `eject*`, `is*`, `readColorValue`.
  2. 🔧 Dibuja el **diagrama de estados** de la SLD (reposo → clasificando → expulsando → comprobando → reposo / error). Hazlo a mano o con Mermaid en esta nota.
  3. Para cada línea que toque hardware, anota qué E/S es y qué significa físicamente.
  4. Lee `Digital_Twin.py` (bloques `dt/sld/belt` y `dt/sld/cylinder`) y `ControladorCintaSLD_mqtt.cs` para ver cómo llega a Unity.
- **❓ Comprobación:** ¿por qué la SLD se queda con el **mínimo** de tensión del sensor de color y no con la primera lectura? ¿Qué pasa si la pieza nunca llega a `I3`?
  - **Mínimo:** el sensor lee sin parar mientras la pieza pasa por debajo, desde `I1` hasta `I3`. La primera lectura puede tomarse con la pieza solo a medias bajo el sensor, y entonces mezcla la pieza con el fondo de la cinta. Una pieza más clara da menos tensión, así que el valor más bajo corresponde al momento en que la pieza tapa el sensor del todo. Por eso el mínimo es la lectura más fiable, venga como venga colocada la pieza.
  - **Si nunca llega a `I3`:** `while not isEjectionTriggered()` no tiene tiempo límite. La cinta sigue a 512 para siempre, el estado se queda en 2 y no se marca ningún error. El bucle tampoco tiene `sleep`, así que lee el color sin descanso y ocupa la CPU. Solo se sale reiniciando el programa. Caso para F0.8 y F4.
- **Entregable:** ✅ el diagrama de estados está en [[SLD - Clasificadora]] (sección "Diagrama de estados"), con dos comportamientos reales que no son los que parecen.

## F0.2 · Preparar el entorno · ⏳
- **Qué hacer:**
  1. Unity Hub con la versión **6000.3.6f1** → abrir `Github/TFM/Unity`.
  2. Comprobar que el **modo Simulación** actual funciona (pedir una pieza roja y verla pasar por la SLD).
  3. Instalar Python 3.11+ en el PC, `pip install paho-mqtt`.
  4. Mosquitto local (ya está en `GUIA_CONEXION.md`) y **MQTT Explorer**.
  5. 💻 Git: seguir la [[Rutina Git]], apartado A (comprobar identidad, crear la rama `planta-virtual`). ✅ La rama ya existe (en local y en `origin`). Reglas: [[D005 Gestión del repositorio]]. Tu escena de trabajo será `PlantaVirtual.unity`: no se toca la de Adrián.
- **Comprobación:** con el modo Simulación en marcha, ves los mensajes `dt/sld/*` en MQTT Explorer… ❓ ¿Los ves? ¿Por qué sí o por qué no? (Pista: `SimuladorOffline` inyecta los mensajes *dentro* de Unity.)
- **Entregable:** commit "rama planta-virtual creada" + diario.

## F0.3 · Diseñar el protocolo de E/S (solo TXT 5) · ✅ 2026-11-05
- **Objetivo:** definir los mensajes entre la [[Librería fischertechnik virtual]] y Unity. Ver [[Imagen de E-S]].
- **Resultado:** [[D006 Protocolo vio]] y [[Protocolo vio]].
  - Topics: `vio/txt5/in` (Unity → Python, imagen completa de entradas: I1, I2, I3, I6, I7, I8 y el estado de C1, periódico a 100 Hz configurable) y `vio/txt5/out` (Python → Unity, imagen completa de salidas: M1 `{speed, dir, running}` y O5–O8, se envía cuando cambia y además cada 100 ms como latido).
  - Valores iguales a los de la API real, sin conversión. `I2` en la escala de `get_voltage()` (unidad pendiente de confirmar).
  - Datos de más de 0,5 s → la librería espera y avisa. Más de 0,5 s sin `out` → Unity pone las salidas en estado seguro (D006.5).
- **❓** ¿Un mensaje con toda la imagen o un topic por cada E/S? → Imagen completa: con un topic por borne, las entradas llegarían sueltas y de instantes distintos (D006.1).
- **Quedan pendientes** (en [[D006 Protocolo vio]]): medidas de la cinta y de `C1` (🔧), validar los 100 Hz con las grabaciones reales, la unidad de `get_voltage()` y la autoría de `SLD.py` (licencia).
- **Entregable:** ✅ [[Protocolo vio]] + ✅ [[D006 Protocolo vio]].

## F0.4 · Librería virtual mínima · ✅ 2026-11-06
- **Objetivo:** que `import lib.controller` funcione en el PC sin hardware.
- **Qué hacer:**
  1. 💻 Crear `Github/TFM/virtual/fischertechnik/…` con las fábricas (`init_*`, `create_*`). Todas las E/S de los 6 TXT se crean, pero **solo las de la SLD se conectan** a MQTT; el resto son "tontas".
  2. Simular lo que importa `SLD.py`: `fischertechnik.controller.Motor`, `fischertechnik.mqtt.Constants` y `FTCloudClient` (desactivado), `lib.display` y los niveles de log propios (`TRACE`, `TRACE0`…).
  3. Script `run_sld_virtual.py` que solo lanza `thread_SLD`.
  4. 💻 Herramienta `maqueta_manual.py`, junto a `run_sld_virtual.py` ([[D006 Protocolo vio]], D006.6): publica `vio/txt5/in` a 100 Hz, permite cambiar `I1`, `I3`, `I6`–`I8`, `C1` e `I2` desde el teclado y muestra lo que llega por `vio/txt5/out`. Sustituye a MQTT Explorer para las pruebas sin Unity, porque un mensaje publicado a mano caduca a los 0,5 s.
- **Comprobación:** sin Unity, con `maqueta_manual.py` pones `I1` a oscuro y ves en la consola que la SLD arranca la cinta (`vio/txt5/out` con `M1` = `{"speed": 512, "dir": …, "running": true}`, ver [[Protocolo vio]]).
- **❓** ¿Por qué hay que crear también las E/S de los otros TXT aunque no se usen? (Pista: `lib/controller.py`.)
  - **Respuesta:** `SLD.py` hace `from lib.controller import *`, y `controller.py` crea las E/S de los 6 TXT en cuanto se importa. Si faltara un solo `create_*`, por ejemplo la cámara del TXT 0, la importación fallaría antes de llegar a la SLD. Por eso existen todos, aunque solo el TXT 5 esté conectado a `vio`.
- **Resultado:** carpeta `virtual/` (cómo usarla: `virtual/README.md`). Qué hay y qué se aprendió: [[Librería fischertechnik virtual]]. Decisión nueva: D006.7 en [[D006 Protocolo vio]] (sin entradas frescas no se envía `out`).
- **Criterio de hecho** (con los dos avisos del plan), demostrado con `py -3.12 -m unittest discover -s virtual/tests -v` (11 tests, 5 pasadas seguidas sin fallos):
  - a) `I1` oscuro → `M1` speed 512, running true.
  - b) `I2` en blanco **antes** de `I3`, porque el color solo se lee entre `I1` e `I3`. `I3` oscuro → `M1` speed **0 con running true** (`setConvBeltSpeedSLD(0)` hace `set_speed(0)` + `start()`), `O8` true, y después `M1` speed 350 running true.
  - c) 6 cambios de `C1` → `M1` running false, `O5` true durante 0,500 s y después false, y `O8` false (en un segundo mensaje).
  - d) Cierras la maqueta → la librería avisa y `out` deja de enviarse (D006.7). La vuelves a abrir → se recupera sola y la cinta vuelve a arrancar con `I1`.
  - Además: latido de unos 10 mensajes por segundo y `in` a 100 Hz reales.
- **Entregable:** ✅ commit + [[Librería fischertechnik virtual]] (sección "Lo que se aprendió al hacerla").

### 🏁 Hito 1 · ✅ 2026-11-06 (con el test automático; falta que Mario lo pruebe a mano con el teclado)
> **`SLD.py` original piensa en mi PC sin maqueta, probado a mano con `maqueta_manual.py`.**
>
> Se alcanza al terminar F0.4: con `maqueta_manual.py` cambias las entradas desde el teclado (se publican a 100 Hz en `vio/txt5/in`) y ves la respuesta de `SLD.py` en `vio/txt5/out`, sin Unity y sin cambiar una línea de `SLD.py`. Formato de los mensajes: [[Protocolo vio]].
>
> Antes se iba a probar con MQTT Explorer. Se cambió porque un mensaje publicado a mano caduca a los 0,5 s (D006.3), y esa regla no se desactiva en pruebas ([[D006 Protocolo vio]], D006.6). MQTT Explorer sigue sirviendo para mirar los mensajes.

## F0.5 · Puente de E/S en Unity · 🟡 2026-11-06 (código hecho y medido; falta la prueba a mano de Mario)
- **Objetivo:** un componente de Unity que reciba `vio/txt5/out` y publique `vio/txt5/in`.
- **Dónde:** carpeta nueva `Assets/Scripts/PlantaVirtual/` → `VirtualIO_TXT.cs`. `MQTTClient.cs` de Adrián no se puede copiar ni envolver: su conexión es privada, se suscribe a una lista fija de `dt/*` y entrega los mensajes en `Update()`, al ritmo de los FPS. Se reutiliza **su librería**, `Assets/Plugins/M2Mqtt.Net.dll`, con una conexión propia.
- **Comprobación:** en el Inspector se ven las salidas cambiar cuando la SLD manda, y las entradas se pueden cambiar a mano. Si paras Python con la cinta en marcha, en menos de 0,5 s Unity para el motor y cierra las válvulas ([[D006 Protocolo vio]], D006.5). Si dejas de publicar `in`, Python deja de enviar `out` (D006.7), y en menos de 1 s Unity también pasa a estado seguro.
- ⚠️ `in` tiene que salir a **100 Hz reales**, no solo configurados. En F0.4, a 40–60 Hz se perdían cambios de `C1` (ver [[Librería fischertechnik virtual]]). Mide la frecuencia real en Unity.
- **Escena:** `Assets/Scenes/PlantaVirtual.unity`, una copia de la escena de Adrián con toda la fábrica (`AssetDatabase.CopyAsset`, GUID nuevo). Lleva `ControladorCintaSLD_mqtt` y `ControladorCilindrosSLD_mqtt` desactivados (`enabled = false`, [[D007 Quién mueve la escena]]) y el objeto `VirtualIO_TXT5`.
- **Comprobado que ningún código de las marionetas de la SLD sigue vivo:**
  - Los dos se suscriben a `OnBeltUpdateEvent` y `OnCylinderUpdateEvent` en `Start()`, con `InvokeRepeating`. Un componente desactivado nunca ejecuta `Start()` ni `Update()`. No tienen `Awake()`, constructores estáticos ni `[RuntimeInitializeOnLoadMethod]`.
  - Nadie más escucha esos dos eventos. `SimuladorOffline` e `InfluxDBClient` inyectan mensajes por `MQTTClient.ProcesarMensajeExterno`, que solo llegan a esos dos eventos: sin suscriptores, no mueven nada de la SLD.
  - Dentro de la SLD no hay más scripts, solo 2 `BoxCollider`. Ningún otro componente de la escena apunta a objetos de la SLD.
  - **Queda para F2:** el VGR sigue leyendo `IsWhite/Red/BlueSensorActivo` y `spawnPoint*` del controlador desactivado (siempre `false` y las posiciones de las rampas).
- **Medido (2026-11-06, con la CLI de Unity):**
  - b) `virtual/medir_vio.py`, 60 s: **100,00 Hz**, hueco medio 10,00 ms (desviación 0,27 ms), hueco más largo 14,5 ms, ningún inválido. Fue con el editor en segundo plano. Falta repetirlo con el editor en primer plano.
  - a) Ciclo blanco cambiando las entradas por la CLI: 512 en marcha → speed 0 con running true → `O8` → 350 → running false → `O5` durante **0,500 s** → `O5` y `O8` apagados.
  - c) Python parado con la cinta a 512: **estado seguro a los 505 ms** del último `out`. El criterio es "edad del último `out` ≤ 0,51 s" porque Unity cuenta desde el último latido, no desde que se para Python.
  - d) Al volver a arrancar Python, Unity sale solo del estado seguro.
  - D006.8: con Unity en pausa 3 s, Python ve las entradas caducadas, deja de enviar `out` y Unity pasa a estado seguro a los 503 ms. Al quitar la pausa, todo se recupera.
- **Falta (Mario):** repetir a) a mano desde el Inspector, y medir b) con el editor en primer plano.
- **Cómo probarlo:** abre `PlantaVirtual.unity` y dale a Play. Arranca `py -3.12 virtual/run_sld_virtual.py` y selecciona `VirtualIO_TXT5`. Cambia las entradas en el Inspector con los mismos pasos que en la tabla del [README de virtual/](../../../virtual/README.md). Para medir: `py -3.12 virtual/medir_vio.py`.
- **Avisos:**
  - Los errores `Could not resolve host 'CHANGE_ME'` son de los clientes MQTT de Adrián, que no tienen broker configurado. Son normales.
  - El menú de la interfaz recarga la escena por su número en la lista de compilación (`UI_ControladorMenu.cs:882`). `PlantaVirtual.unity` no está en esa lista (añadirla toca `ProjectSettings/`, regla 3 de [[D005 Gestión del repositorio]]). Según el código, en esta escena el número es -1 y la recarga fallaría. No está probado. Mientras tanto, **no uses los botones de modo del menú** en esta escena.

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

## F0.7 · Decidir quién mueve la escena · ✅ 2026-11-06
- **Problema:** `ControladorCintaSLD_mqtt.cs` y `ControladorCilindrosSLD_mqtt.cs` también mueven la cinta y las piezas (son marionetas, ver [[Marioneta vs planta]]).
- **Opciones:** (a) en modo virtual se desactivan y manda la planta; (b) la planta solo calcula E/S y la animación sigue entrando por `dt/*`.
- **❓** ¿Qué pasa con las piezas "de repuesto" que crean estos scripts si los dejas activos?
  - **Respuesta:** aparecerían de la nada y falsearían la planta. En la planta virtual, una pieza solo puede estar donde la han llevado la cinta y los cilindros. Si la marioneta crea una de repuesto porque "debería haber una", los sensores calculados (`I1`, `I3`, `I6`–`I8`) verían una pieza que nadie ha movido. `SLD.py` reaccionaría a ella como si fuera real, y un fallo del control (una pieza perdida o mal expulsada) quedaría tapado.
- **Decisión:** (a). En modo virtual manda la planta. Ver [[D007 Quién mueve la escena]].
- **Entregable:** ✅ [[D007 Quién mueve la escena]].

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
