---
tags: [decisión, validación]
---

# D011 · Validación con la planta real

- **Fecha:** 2026-11-17 · **Estado:** ✅ Aceptada (método y criterios). Datos reales capturados; la comparación con la virtual y la calibración quedan para otra sesión, sin maqueta.

## Contexto
El tutor (reunión 2026-11-17) pide demostrar que lo que pasa en la planta virtual es lo mismo que en la real, con un porcentaje de acierto medido. Hay que fijar **cómo se mide** y **qué es "igual"** antes de medir.

La planta real publica la SLD por `dt/sld/belt` y `dt/sld/cylinder` (`vscode/lib/Digital_Twin.py`). La virtual no publica `dt/*`: su única fuente es `vio` ([[D007 Quién mueve la escena]]).

## Decisión

### 1. Dos brokers a la vez
- **1883** (servicio de Windows, `localhost`): planta virtual (`vio`). No se toca.
- **1884** (`0.0.0.0`, anónimo): planta real. Se arranca a mano en una ventana de Administrador con `TFM-UNITY\mosquitto_1884.conf` (fuera del repo), que además escribe un log para ver quién se conecta. Regla de firewall «Mosquitto 1884».
- **No** se usa el `net stop mosquitto` de la guía de conexión: apagaría el 1883.
- El host del TXT es la IP del PC en la wifi GII, que **cambia de un día para otro**. No va al repo: va en `lib/MQTT.py` del proyecto del TXT y, para los scripts, en `virtual/validacion/config_local.json` (en `.gitignore`).

### 2. De dónde sale el tiempo
- **Real:** el `ts` del payload, que pone el TXT. Así la latencia de la wifi no entra en los tramos. Medida hoy: ida y vuelta 109,5 ms de media, 259,7 ms de máximo (`medir_ping.py`, 10/10).
- **Virtual:** `t_pc` al recibir `vio/txt5/in` y `vio/txt5/out` (localhost, sin wifi).
- **Resolución de la traza real ≥ 0,2 s:** `dt/sld/*` sale del hilo lento de `Digital_Twin.py:325` (`wait(0.2)` más lo que tarda en leer todas las estaciones).

### 3. Secuencia real de un ciclo (de `vscode/lib/SLD.py:63-112`)
I1 → cinta ON (512) → I3 → cinta OFF → **cinta ON (350, contando C1)** → cinta OFF → cilindro (0,5 s) → barrera de la rampa.
- En la traza real la parada entre 512 y 350 casi nunca se ve (0,2 s): la fase 2 se detecta como "primer cambio de velocidad tras I3".
- El ciclo empieza en el **I1 que arranca la cinta**. Si la mano tapa I1 al colocar la pieza, ese primer toque no cuenta (visto hoy, blanca 4; corregido en `a_eventos.py`, commit `f3d7f9a`).

### 4. Criterios de "igual"
| Métrica | Cálculo | Objetivo |
|---|---|---|
| Clasificación | % piezas en la rampa que corresponde a **la decisión de la planta** con esa lectura de color | igual en ambas |
| Secuencia | % ciclos con el orden del punto 3 | 100 % |
| Tiempos | error de la **media** por tramo (I1→I3, I3→cilindro, I1→rampa), 5 piezas por color | ≤ 10 % **o** ≤ 2 × periodo de muestreo (0,4 s) si el tramo es corto |
| Control | vídeo a 60 fps de 2 piezas por color | confirma los tiempos de la traza |

- **Sin métrica de C1:** el contador solo va a `logging.DEBUG` (`SLD.py:92`), no sale por la consola. Se usa la duración de la fase 2 (I3→cilindro), que depende de los pulsos de C1.
- **Rampa:** si la barrera de la rampa no aparece en la traza, se valida a ojo o con vídeo. Hoy apareció en los 21 ciclos.
- **Clasificación:** "100 % en ambas" no sirve. La planta real se equivoca con piezas rojas en el límite (abajo). Lo que se exige a la virtual es que **decida lo mismo que la real con la misma tensión**, con los mismos umbrales.

### 5. Umbral rojo/azul: 1460, no 1400
- FactoryMain carga la calibración y `set_calib_data_SLD()` (`vscode/lib/SLD.py:155-170`) **ignora el archivo y fija `thresh_red_blue = 1460`**. Los 1400 de `SLD.py:54` y de `data/FactoryCalibDefault.json` solo valen hasta que se carga la calibración.
- `virtual/alumno/ejercicios/soluciones/nivel4.py` usa **1400** («los mismos que usa la fábrica»): no es verdad para la planta real.
- **Confirmado en la maqueta:** `nivel4.py` con 1400 → rojas 0/3; el mismo `nivel4.py` con 1460 (solo en la copia del TXT) → **rojas 3/3**.
- La virtual tiene que usar el mismo umbral que el programa que se compara. Propuesta: [[D012 Umbral de color y sensor de color virtual realista]].

## Resultados de hoy (detalle en `02-DOCS/attachments/validacion/2026-11-17_V2_resultados_reales.md`)
- **FactoryMain:** blancas 5/5, azules 5/5, **rojas 7/11** en su rampa (4 a la azul: una pieza que leía mal y otra en el límite, según Mario). Secuencia OK en **21/21** ciclos.
- **Tiempos reales** (media): I1→I3 ≈ 4,7-4,9 s; I3→cilindro 1,26 / 2,79 / 4,83 s (blanca / roja / azul); I1→rampa 6,3 / 8,1 / 9,8 s.
- **Programa del alumno** (`nivel4.py` sin cambiar ninguna línea, proyecto `SLD_alumno_real.ft` fuera del repo): blancas 3/3, azules 3/3, **rojas 0/3** (todas a la azul) con el umbral 1400 de los ejercicios. **Causa confirmada:** con 1460, **rojas 3/3** (9/9 en total). Vídeos en `TFM-UNITY\planta_real\videos\` (fuera del repo). Sin trazas: el TXT no se conectó al broker durante esta prueba; resultado contado mirando las rampas.
- **BME680:** para que arrancara el proyecto del alumno, Mario envolvió en `try/except` la línea 58 de `lib/controller.py` (el sensor ambiental de la SSC no responde en el bus I2C), **solo en la copia del TXT**. El repo y el código del alumno no cambian.

## Por qué
- `ts` del TXT y dos brokers: separan la planta real de la virtual sin tocar el código de Adrián ni el de la virtual.
- Criterio de tiempos por la media y con suelo de 2 × periodo: con 0,2 s de muestreo, un tramo corto (I3→cilindro de la blanca, ≈ 1,3 s) no se puede medir al 10 % pieza a pieza.
- Clasificación "como la real": si la real se equivoca con una pieza, una virtual que acierta no es "igual".

## Pendientes
- [ ] V3: misma prueba en la virtual (5 + 5 + 5, `run_virtual.py`, broker 1883). Antes, repetir la batería de `virtual/tests` con Unity parado (hoy 9 fallos por tener Unity en Play en el 1883 a la vez).
- [ ] V5: `comparar_trazas.py` real vs virtual → `proyecto/Validación planta real.md`.
- [ ] Medidas físicas y vídeo (`virtual/validacion/plantillas/medidas_fisicas.csv`, `video_real.csv`).
- [ ] Umbral 1460 en la virtual y en los ejercicios: [[D012 Umbral de color y sensor de color virtual realista]] (propuesta, decide Mario).
- [ ] Por qué el proyecto del alumno no se conectó al broker (falta la línea de consola `MQTT con el PC: ...`). Sin eso, V7 no tiene traza.
- [ ] `a_eventos.py`: modo alumno (rampa sin evento de cilindro: `nivel4.py` no escribe `sld_cylinder_state`).
- [ ] Copia de seguridad del FactoryMain que hay en el TXT (`TFM-UNITY\FactoryMain_TXT_2026-11-17.ft`): confirmar que se exportó.

Relacionado: [[D006 Protocolo vio]], [[D007 Quién mueve la escena]], [[SLD - Clasificadora]], [[F0 - Prueba de concepto SLD]].
