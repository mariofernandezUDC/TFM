---
tags: [decisión]
---

# D007 · Quién mueve la escena

- **Fecha:** 2026-11-06 · **Estado:** ✅ Aceptada (de momento solo la [[SLD - Clasificadora]])

## Contexto
`ControladorCintaSLD_mqtt.cs` y `ControladorCilindrosSLD_mqtt.cs` son **marionetas** (ver [[Marioneta vs planta]]):
- mueven la cinta, la pieza y los cilindros según los mensajes `dt/sld/*` ([[Contrato MQTT]]);
- **pintan** la pieza con el color que dice el mensaje;
- **inventan piezas de repuesto** cuando "debería haber una".

Si la planta virtual también mueve la escena, hay **dos fuentes de movimiento** para los mismos objetos: "Doble fuente de movimiento" en [[Riesgos]]. Es el paso F0.7 de [[F0 - Prueba de concepto SLD]].

## Opciones
- A. **En modo virtual manda la planta.** `SLD_Planta.cs` mueve la cinta, la pieza y los cilindros a partir de las salidas de `vio` (`M1`, `O5`–`O8`). Y **calcula** las entradas (`I1`, `I2`, `I3`, `C1`, `I6`–`I8`) a partir de lo que pasa en la escena ([[Protocolo vio]]).
- B. **La planta solo calcula E/S**, y la animación sigue entrando por `dt/*`, con las marionetas como hoy.

## Decisión
**Opción A.** En modo virtual, la planta es la única dueña de la cinta, la pieza y los cilindros de la SLD.

## Por qué
- **Causalidad al derecho.** La pieza existe y por eso el sensor la ve. Con la marioneta va al revés: el mensaje dice "hay pieza" y Unity dibuja una. Ver [[Marioneta vs planta]].
- **Los fallos del control se ven.** Con la A, si la lógica cuenta mal los pulsos de `C1`, la pieza se pasa de su cilindro y cae donde no debe. Con la marioneta, el fallo queda escondido: la pieza se pinta y se coloca en la rampa que diga el mensaje. Para el alumno, la A es la que enseña ([[D002 Modo operador y modo programador]]).
- **Una sola verdad.** La B exigiría arrancar también `Digital_Twin.py` para generar `dt/*`, y mantener sincronizadas dos verdades: la de la planta (E/S por `vio`) y la de la animación (`dt/*`). Cualquier diferencia entre las dos, aunque sea un retraso, produce una escena que no se corresponde con lo que ven los sensores.

## Consecuencias
- La escena **`PlantaVirtual.unity`** (nueva y propia, [[D005 Gestión del repositorio]]) **no lleva** los scripts marioneta en la SLD: ni `ControladorCintaSLD_mqtt` ni `ControladorCilindrosSLD_mqtt`.
- La escena de Adrián (`GemeloDigital_LearningFactory.unity`) **no se toca**. Sus marionetas siguen igual y el gemelo de la planta real sigue funcionando.
- `SLD_Planta.cs` (F0.6) tiene que hacer todo lo que hoy hacen las marionetas, pero a partir de las salidas: mover la cinta, empujar con los cilindros y llevar la pieza a la rampa. Lo que no hace es pintar ni inventar piezas.
- Con la A, la escena virtual no usa `dt/*` en la SLD. `Digital_Twin.py` no hace falta en modo virtual.

## Pendientes
- [ ] **F2 (VGR):** el VGR lee propiedades de `ControladorCilindrosSLD_mqtt` (`ControladorVGR_mqtt.cs`, líneas ~232 y ~425):
  - `IsWhiteSensorActivo`, `IsRedSensorActivo` e `IsBlueSensorActivo`, para comprobar si el agarre en la rampa falló;
  - `spawnPointBlanco`, `spawnPointRojo` y `spawnPointAzul`, para saber de qué rampa viene la pieza.

  En `PlantaVirtual.unity` la planta tendrá que ofrecer algo equivalente, por ejemplo las barreras `I6`–`I8` calculadas y las posiciones de las rampas. ⚠️ El VGR busca la marioneta con `FindFirstObjectByType`, y si no la encuentra **no da error: se salta la comprobación sin avisar**. Hay que resolverlo antes de que el VGR coja piezas de la SLD virtual.

Relacionado: [[Marioneta vs planta]], [[Riesgos]], [[Arquitectura de la planta virtual]], [[D006 Protocolo vio]], [[SLD - Clasificadora]], [[F0 - Prueba de concepto SLD]] (F0.6 y F0.7).
