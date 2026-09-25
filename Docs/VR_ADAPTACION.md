# Adaptación VR — VIVE XR Elite (Windows)

Trabajo en `adaptacion-vr`, Unity 6000.3.6f1. Revisión iniciada el 17/09/2026.

Actualización del 23/09/2026: menú World Space, controles de vuelo y validación de UI en [VR_MENU_Y_MOVIMIENTO.md](VR_MENU_Y_MOVIMIENTO.md). Los apartados siguientes documentan el estado de la revisión del 17/09; la locomoción se ha añadido posteriormente.

## Hardware y conexión confirmados

El usuario confirma VIVE XR Elite y sus mandos originales mediante SteamVR. El runtime OpenXR registrado en Windows es SteamVR. El registro de SteamVR identifica los mandos de streaming como `vive_cosmos_controller`. GPU leída dentro del editor: GTX 1050, 2981 MB, Direct3D11.

## Cambios

- Desactivada la creación automática de XR Device/Interaction Simulator.
- Incorporado VIVE OpenXR Plugin 2.5.1 oficial, fijado por etiqueta Git (commit registrado en packages-lock.json). Añadido XR Hands 1.5.1, referenciado por su ensamblado.
- Herramientas exclusivas del editor en `Tools > VR`: configurar el perfil, generar diagnóstico y registrar 20 segundos de seguimiento. No se incorporan a la aplicación compilada.
- Servidor MCP recuperado en `http://127.0.0.1:8080/mcp`; se ha leído el editor y confirmado la ruta exacta del proyecto.
- Escena abierta guardada con copia previa en `Assets/_Recovery/VR_before_restart_20260917_173945.unity`, incluyendo los cambios que estaban sin guardar.

No se ha aumentado la resolución ni modificado el renderizado de SteamVR. Los scripts existentes de MQTT, InfluxDB, MovimientoDelGemelo y el resto de Assets/Scripts se mantienen idénticos, verificados mediante SHA-256.

## Comprobado en el editor

- Cámara antigua inactiva; cámara VR activa con TrackedPoseDriver habilitado.
- Ambos mandos tienen TrackedPoseDriver y ControllerInputActionManager habilitados.
- InputActionManager contiene XRI Default Input Actions; posiciones, rotaciones y acciones de ambos lados tienen bindings XR.
- LearningFactory_modelo3D es raíz independiente: no depende de la cámara ni del XR Origin.
- Hay 13 componentes de locomoción desactivados. Para seguimiento físico no son necesarios. Para giro por pasos se necesitan XRBodyTransformer, LocomotionMediator y SnapTurnProvider; para desplazamiento virtual, además el proveedor elegido. No conviene activar conjuntamente todos los modos. Teletransporte exige superficies de destino; gravedad/desplazamiento exige comprobar suelo y colisiones.

## Resultado de la validación

La primera importación no cargó los ensamblados de HTC; después de guardar una copia de la escena y reiniciar Unity, VIVE.OpenXR y VIVE.OpenXR.Editor compilaron correctamente. El perfil VIVE Cosmos Controller Interaction está activado. SteamVR anuncia y habilita XR_HTC_vive_cosmos_controller_interaction; no hay extensiones solicitadas incompatibles.

Prueba en Play con runtime SteamVR/OpenXR, Single Pass Instanced, 1456 × 1456 por ojo, sin simulador. La segunda captura de 20 segundos registró:

| Dispositivo | Muestras | Seguimiento válido | Posiciones distintas con seguimiento | Gatillo máximo |
|---|---:|---:|---:|---:|
| Cabeza | 91 | 91 | 91 | No aplicable |
| Mando izquierdo | 91 | 80 | 80 | 1,00 |
| Mando derecho | 91 | 72 | 72 | 0,39 |

Esto confirma datos reales de cabeza, ambos mandos y ambos gatillos. Hubo pérdidas de seguimiento de mandos durante la captura; no equivale a una prueba de estabilidad completa. Una tercera captura obtuvo 96/96 muestras de cabeza, pero 0/96 de seguimiento de cada mando (archivo VR-tracking-loss-20260917.csv); la causa de esa pérdida requiere comprobar físicamente los mandos y el modo de seguimiento del visor. Las acciones XRI de posición/rotación están habilitadas y resuelven controles de cada mando. Las acciones Move y Snap Turn también resuelven el thumbstick correcto, pero la comprobación de valores de los joysticks sigue pendiente.

El fallo previo xrCreateInstance failed no reapareció en esta prueba. Se mantienen errores MQTT por el host CHANGE_ME, que ya estaba configurado así; no se ha cambiado ni probado la conectividad MQTT/InfluxDB. No se certifica resuelto el cierre gráfico D3D11 anterior a partir de una prueba corta.

Para repetir: entrar en Play y ejecutar `Tools > VR > Write diagnostic report`. `Logs/VR-validation.txt` muestra dispositivos, seguimiento y controles asociados. `Tools > VR > Record 20 seconds of tracking` registra cabeza, manos, gatillos y joysticks en `Logs/VR-tracking.csv`. Mantener los mandos delante de las cámaras del visor durante la prueba. No confundir seguimiento físico con desplazamiento virtual: los proveedores de locomoción siguen desactivados.

## Fuentes

- HTC: https://developer.vive.com/resources/openxr/unity/tutorials/basic-input-for-openxr/ (perfil Cosmos para Windows).
- Paquete oficial: https://github.com/ViveSoftware/VIVE-OpenXR-Unity/releases/tag/versions/2.5.1

