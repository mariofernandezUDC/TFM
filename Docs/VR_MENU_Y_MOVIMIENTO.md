# Menú y navegación VR — 23/09/2026

Proyecto: `C:/Users/Mario Fernández/Desktop/UNITY-TFM/AdrianTFM/Unity`.
Escena: `Assets/Scenes/GemeloDigital_LearningFactory.unity`.
Se ha trabajado en la rama existente `adaptacion-vr`, conservando los cambios anteriores. Copia previa de la escena y scripts de interfaz: `Docs/Backup_20260923/`.

## Uso

Iniciar SteamVR / VIVE Streaming, conectar las XR Elite y los mandos, y entrar en Play en Unity. El visor debe estar activo: sin visor, la aplicación conserva la cámara y el menú de escritorio. La configuración continúa siendo Windows; no se ha generado un ejecutable nuevo en esta revisión.

| Control | Acción |
|---|---|
| Joystick izquierdo | Vuelo adelante/atrás siguiendo la mirada y desplazamiento lateral |
| Joystick derecho arriba/abajo | Subir/bajar en vuelo libre |
| Joystick derecho izquierda/derecha | Giro continuo, hasta 45 grados/s |
| Gatillo | Pulsar o arrastrar los controles mediante el rayo de XRI |
| X / botón primario izquierdo | Cerrar el panel de pedido |
| Y / botón secundario izquierdo | Abrir/cerrar el panel de tres piezas delante de la mirada |
| B / botón secundario derecho | Vista siguiente (lista circular) |
| A / botón primario derecho | Vista anterior (lista circular) |

El vuelo se mantiene por petición expresa del usuario. Mientras el panel de pedido está abierto se reserva la interacción a elegir una pieza; al cerrarlo vuelve el vuelo. El límite inferior comprueba la altura real de los ojos, no solo el origen del rig: suelo en Y=0 y margen de 0,15 m. En Unity el eje vertical es Y. Este límite también se aplica después de cambiar de vista y de recibir seguimiento. Velocidades: 0,65 m/s y 0,45 m/s vertical, giro continuo de 45 grados/s, iguales a la revisión previa. A/B saltan entre las vistas existentes sin animación de viaje.

En VR solo aparece el nuevo panel con tres botones, sin scroll. Y lo abre delante del visor y lo cierra. Texto TMP SDF grande, fondo oscuro opaco, botones de alto contraste; panel de 1,2 m a 1,25 m de distancia. Los menús antiguos quedan invisibles y sin interacción, conservando sus componentes de lógica; el escritorio mantiene su interfaz. El pedido usa UI_ControladorMenu.PedirPiezaSimulacion, como antes, y cierra el panel. Durante un pedido se bloquean los tres botones y se muestra «Pieza en proceso…».

## Cambios anteriores a la simplificación VR

- Corregida la altura fija de ContenidoSimulacion: ahora se calcula con sus controles. Las tres tarjetas de pedido tienen una fila uniforme y botones más altos.
- Scroll lateral limitado al contenido, sin elasticidad ni inercia; rueda más útil y arrastre correctamente inicializado. La posición del scroll se conserva cuando el pedido recarga la escena.
- `VRPlantRig` ofrece vuelo, giro y controles de menú. Los proveedores de locomoción anteriores permanecen desactivados para evitar movimientos duplicados; los gestores de mandos están en modo continuo y mantienen los rayos e interacción UI.
- Un único XRUIInputModule procesa los mandos y el ratón; el módulo de entrada anterior queda desactivado.
- El Canvas raíz pasa a World Space cuando el visor está activo, con tamaño lógico 2200 × 1200, ancho físico 2,8 m y distancia de 1,8 m. Los paneles secundarios heredan ese espacio.
- La escena actual contiene un Canvas raíz, no siete Canvas independientes. Los desplegables TMP y calendarios generan Canvas adicionales en ejecución. Se les asigna cámara y TrackedDeviceGraphicRaycaster, incluidos sus bloqueadores y listas.
- Materiales de interfaz con profundidad siempre visible en VR para impedir que el modelo oculte botones; conservan recorte, máscaras y soporte estéreo. Las referencias a los shaders se guardan en la escena para su inclusión en futuras compilaciones.
- Ocultar el menú cambia su CanvasGroup: no desactiva el controlador, suscripciones, relojes ni simulación.
- Las vistas predefinidas trasladan el XR Origin en VR; no sobrescriben la pose del visor. La pose virtual se conserva en las recargas de escena durante la sesión.
- Tooltip de stock junto al hueco señalado en VR, sin perseguir coordenadas de ratón ni interceptar el rayo.

## Validación realizada

- Compilación de scripts en Unity 6000.3.6f1 sin errores de compilación tras la importación final.
- Play en escritorio: tres botones completamente dentro del viewport al final del scroll.
- Pedido BLUE desde el botón: arranque, recarga de escena, reproducción de 965 eventos y registro de finalización. La secuencia grabada dura aproximadamente 142 s. Segunda prueba de pedido para comprobar la persistencia del scroll: valor normalizado tras recargar aproximadamente 0,0000008 (se conserva el final de la lista), con los tres botones visibles; esta segunda ejecución se detuvo al terminar esa comprobación.
- Prueba sintética de rayos XR sobre todos los controles de las seis secciones: vistas, cámara/PTU, pedidos, sensores, histórico y simulación. Todos los controles evaluados recibieron el rayo.
- Ocho desplegables: creación y detección del rayo sobre sus opciones. Dos calendarios: Canvas World Space y detección de un botón de día.
- Ocultar la interfaz mantiene activo UI_ControladorMenu.
- Revisión visual mediante capturas del menú de escritorio y del panel World Space. El panel no queda oculto por la planta ni se solapan las cabeceras tras el ajuste.
- Verificación con Git: MQTTClient.cs, MQTT_InterfaceClient.cs, InfluxDBClient.cs, MovimientoDelGemelo/ y Simulacion/ sin cambios respecto a HEAD. También se mantiene UI_ControladorMenu.cs sin cambios.
- Escena guardada y sin cambios pendientes de guardar tras las pruebas automatizadas. Se salió de Play; el usuario volvió a iniciarlo a las 19:08 para conectar el visor, y se respetó esa ejecución.

Evidencias en `Docs/Validacion_VR_20260923/`: `VR-world-ray-check.txt`, `VR-scroll-check.txt`, `VR-world-menu.png` y `VR-menu.png`.

Herramientas reproducibles del editor en Tools > VR: Report UI and flight, Check simulation scroll bounds, Test world UI rays without headset, Test offline blue request y Capture menu. La prueba sin visor activa una presentación World Space temporal, lanza rayos sintéticos sin accionar maquinaria y vuelve a escritorio. No sustituye una prueba con los mandos físicos. Apply plant UI and flight permite repetir la configuración en una escena guardada, fuera de Play.

## Pendiente de validación real

Durante las pruebas automatizadas no había visor activo. Posteriormente se pudo registrar el visor real (véase la ampliación de diagnóstico). Quedan por comprobar los valores físicos de ambos joysticks, X/Y/B, arrastre con gatillo, scroll, seguimiento continuo, comodidad, legibilidad binocular y rendimiento dentro del visor. Tampoco se ha validado una compilación ejecutable Windows en esta sesión.

La configuración existente usa el host MQTT `CHANGE_ME`: se registran errores de resolución de ese host. No se han cambiado credenciales ni probado conectividad real, vídeo PTU o consultas InfluxDB. La prueba de UI verifica acceso a sus controles, no sus servicios externos.

Durante la simulación aparecen errores de velocidades sobre cuerpos cinemáticos en código existente (por ejemplo SoporteHBW_proxy, ControladorHBWposition_mqtt y BrazoMPO_proxy). No se han modificado esos scripts, conforme al alcance indicado por el profesor. El pedido azul finalizó pese a esos errores; no se certifica con ello toda la física de la planta.


## Ampliación: conexión real a las 19:08–19:09

Mientras se investigaba la dificultad de conexión indicada por el usuario, este inició Play con SteamVR activo. Se confirmó el runtime OpenXR de SteamVR, perfil VIVE Cosmos, visor reconocido e interfaz automáticamente en World Space (`Rig=True Play=True VR=True`). No se cambiaron ajustes de streaming ni controladores del sistema.

La captura real de 20 segundos produjo 93 muestras por dispositivo:

| Dispositivo | Marcado tracked | Posiciones distintas | Rotaciones distintas | Gatillo máximo | Muestras con joystick distinto de cero |
|---|---:|---:|---:|---:|---:|
| Cabeza | 93/93 | 93 | 93 | — | — |
| Izquierdo | 93/93 | 1 | 1 | 0 | 0 |
| Derecho | 93/93 | 1 | 1 | 0 | 0 |

Ambos mandos notificaron posición (0,0,0) durante toda la captura. El indicador tracked por sí solo no acredita seguimiento útil de los mandos. No se puede distinguir con esta muestra entre mandos sin uso/en reposo y un fallo de entrada upstream. Se debe comprobar primero que ambos mandos se mueven y sus botones responden en SteamVR, y repetir la captura mientras se accionan.

Los registros de SteamVR anteriores a esta conexión incluyen `RequestData timeout`, `no pose data coming` y `AcquireSync FAILED with WAIT_TIMEOUT`: evidencian interrupciones previas de transporte/sincronización, sin establecer una causa única. Los avisos `no hand pose` se refieren al seguimiento de manos y no bastan para diagnosticar los mandos.

GPU confirmada dentro de Unity: NVIDIA GTX 1050, 2981 MB. Está por debajo de la GTX 1060 de 6 GB o equivalente exigida por HTC para VIVE Streaming. Es una limitación relevante, no una demostración de la causa de la desconexión. Requisitos oficiales: https://www.vive.com/eu/support/vs/category_howto/requirements.html

Evidencias adicionales: `VR-tracking-real.csv`, `tracking-summary.json`, `VR-hardware-real.txt` y `VR-ui-real.txt` en la carpeta de validación. El movimiento con joystick y los botones físicos siguen pendientes de una muestra con entradas reales.


## Corrección tras la prueba del usuario y perfil VR ligero

El usuario confirmó que encontró el menú y que los controles funcionaban. Durante el diagnóstico se detectó un defecto real de orden de inicio: UI_ControladorMenu.Start podía cerrar el panel después de VRPlantRig.Start. La apertura VR se realiza ahora después de los Start de la interfaz, con una recolocación tras recibir seguimiento de cabeza. También se eliminó la dependencia de Application.isFocused, que bloqueaba el vuelo al usar otra ventana del escritorio.

Para acercarse al vuelo de un modo creativo/espectador, el desplazamiento sigue la mirada (incluida la inclinación), con aceleración/deceleración progresiva de 0,15 s. El giro derecho es continuo a 45 grados/s. `flyAlongView` y `smoothTurn` permiten recuperar vuelo horizontal y giro por pasos desde el Inspector.

Nuevo perfil exclusivo de VR, `Assets/Settings/VR_RPAsset.asset`, y renderer independiente `VR_Renderer.asset`: Forward, MSAA 2x, escala de render 1, sin SSAO ni HDR, sin copias obligatorias de color/profundidad, una cascada de sombras, distancia 10 m, mapa principal 1024 y sin sombras de luces adicionales. El perfil PC original no se modifica; se restaura al salir de VR. Se busca reducir bordes dentados y coste de render, sin aumentar la resolución de streaming (1456 × 1456 por ojo en la lectura previa).

No se atribuye una mejora concreta de FPS sin medirla en una prueba comparable. La percepción de nitidez y fluidez también depende de la compresión y estabilidad de VIVE Streaming. Los registros de SteamVR mostraban timeouts de sincronización incluso fuera del código del menú.

## Validación del panel compacto y vistas (última revisión)

- Tres botones y tres rayos XR sintéticos con resultado correcto (AZUL, ROJA, BLANCA).
- Menús anteriores ocultos y sin interceptar rayos; límite inferior comprobado desplazando temporalmente el rig 20 m hacia abajo.
- Recorrido completo de vistas y prueba siguiente/anterior/siguiente con retorno a la misma posición.
- Vista previa renderizada y revisada: título, etiquetas y ayuda sin cortes ni solapamientos. La captura de PC no garantiza la nitidez percibida a través del streaming; pendiente valoración con el visor.
- Compilación sin errores. En Play persisten los avisos de conexión a CHANGE_ME de la configuración MQTT existente, sin cambios en esos clientes.
- Evidencias: VR-compact-check.txt y VR-compact-popup.png. No se ha repetido el recorrido completo de simulación: se mantiene la entrada de pedido y la lógica ya comprobada anteriormente.

## Reinicio de cada pedido y aislamiento físico del visitante VR

A petición del usuario, cada pedido válido de simulación pasa por el reinicio limpio ya existente en UI_ControladorMenu: se recarga la escena, se restaura el modo offline, se llena el almacén y se reproduce el color pedido. Los pedidos simultáneos se rechazan. VRPlantRig conserva la pose virtual entre recargas.

La comparación de los objetos serializados contra HEAD no encontró cambios en los colliders ni transformaciones mecánicas originales de la planta. Se desactivan los colliders y contactos de Rigidbody del XR Origin: el visitante de vuelo y sus mandos no necesitan empujar piezas ni activar sensores físicos para interactuar con el Canvas por rayos.

SimuladorOffline carga las grabaciones en Awake, antes del autoarranque del menú. La preparación espera a que terminen los Start y a que el spawner aplique el stock. La coroutine de preparación y reproducción tiene un único ciclo de vida cancelable. No se modifican JSON de grabaciones, temporización, calibraciones ni trayectorias.

En BrazoMPO_proxy, ControladorHBWposition_mqtt y SoporteHBW_proxy se anulan velocidades solo si el Rigidbody todavía es dinámico y después se hace cinemático, evitando las llamadas no admitidas por Unity 6. Son ajustes puntuales de transición, no cambios en las leyes de movimiento. MQTTClient, MQTT_InterfaceClient e InfluxDBClient permanecen sin cambios. Copias anteriores a esta revisión en Backup_20260923/Before_order_reset.

Prueba de integración: Tools/VR/Test three complete orders acciona los botones AZUL, ROJA y BLANCA en ese orden a velocidad normal. Comprueba nueva instancia de escena en cada pedido, 9/9 huecos inicialmente ocupados, llegada de un Renderer de pieza a la rampa correspondiente, finalización, número máximo de piezas visibles y ausencia de colliders habilitados en el visitante XR. Evidencia en VR-orders-validation.txt (se incorpora al terminar).

Resultado 20:01:23: PASS para BLUE, RED y WHITE consecutivos. Los tres pedidos iniciaron una escena nueva, empezaron con 9/9 huecos y alcanzaron físicamente su rampa. Máximo registrado: nueve renderizadores de pieza activos por ciclo. Sin avisos de velocidad sobre Rigidbody cinemático ni excepciones de simulación en la prueba; persisten errores/avisos MQTT de host CHANGE_ME y publicaciones sin conexión, ajenos al recorrido offline. La prueba verifica esos recorridos concretos, no garantiza ausencia de cualquier fallo futuro. Unity queda en Play para la comprobación del usuario con las gafas.
