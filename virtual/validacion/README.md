# virtual/validacion/ · Planta virtual frente a planta real (SLD)

Demuestra con números que la SLD virtual se comporta como la real (petición del
tutor, reunión del 2026-11-17). Decisión: D011 «Validación con la planta real».

## Qué hay aquí

| Archivo | Para qué |
|---|---|
| `grabar_trazas.py` | Graba en JSONL los mensajes MQTT de la SLD, real (`dt/sld/#`, 1884) o virtual (`vio/txt5/*`, 1883) |
| `a_eventos.py` | Convierte una traza (o el CSV del vídeo) en ciclos de eventos, uno por pieza |
| `comparar_trazas.py` | Calcula clasificación, secuencia y tiempos, y dice CUMPLE / NO CUMPLE |
| `medir_ping.py` | Latencia PC ↔ TXT con `dt/ping` → `dt/pong` |
| `plantillas/*.csv` | Para apuntar a mano: vídeo, consola de ROBO Pro y medidas físicas. Separador `;` (Excel en español) |
| `config_local.example.json` | Copia a `config_local.json` si el broker real necesita otra IP o usuario. No va al repo |
| `trazas/` | Aquí se guardan las grabaciones |

Tests (sin broker): `py -3.12 -m unittest virtual.tests.test_validacion` o
`py -3.12 -m unittest discover -s virtual/tests -p "test_validacion.py" -v`

## Eventos de un ciclo (secuencia real de `vscode/lib/SLD.py`)

`I1_on` → `cinta_on_1` (512) → `I3_on` → `cinta_on_2` (350, contando C1) → `cilindro_on` → `rampa`

- En la traza real, `dt/sld/*` se publica desde un hilo que da una vuelta cada
  **0,2 s** como poco: cada evento puede llegar hasta 0,2 s tarde. Por eso el criterio
  de tiempos admite **≤ 10 % o ≤ 2 × 0,2 s**.
- El tiempo real sale del `ts` que pone el TXT en cada mensaje: así no cuenta el retraso de la wifi.
- La parada entre la fase 1 y la 2 dura milisegundos: en la traza real la velocidad pasa
  de 512 a 350 directamente. La fase 2 se detecta como el primer cambio de velocidad tras I3.
- C1 no viaja en `dt/sld/*` ni sale por consola: la "posición" se mide con la duración de la fase 2.

## Pasos con la maqueta

Todo en PowerShell, en la carpeta `Github/TFM`. Hacen falta **dos brokers**: el 1883 (planta
virtual, el servicio de siempre) y el 1884 (planta real). **No pares el 1883.**

### V0 · Conexión
1. Arranca el broker 1884 (ventana de Administrador, déjala abierta):
   ```
   "C:\Program Files\mosquitto\mosquitto.exe" -c C:\Users\Mario\Desktop\TFM-UNITY\mosquitto_1884.conf -v
   ```
   (Si no existe, crea `mosquitto_1884.conf` con dos líneas: `listener 1884 0.0.0.0` y `allow_anonymous true`,
   y abre el puerto: `netsh advfirewall firewall add rule name="Mosquitto 1884" dir=in action=allow protocol=TCP localport=1884`.)
2. Comprueba la IP del PC con `ipconfig` (wifi GII). Tiene que ser la del `host=` de `lib/MQTT.py`
   en el proyecto del TXT. **Puede cambiar de un día a otro.**
3. Lanza `FactoryMain-DigitalTwin.ft` desde ROBO Pro Coding. En la ventana del broker verás `New client connected from 10.x.x.x`.
4. Mira la SLD en directo y mete una pieza a mano:
   ```
   & "C:\Program Files\mosquitto\mosquitto_sub.exe" -h localhost -p 1884 -t "dt/sld/#" -v
   ```
5. Latencia: `py -3.12 virtual/validacion/medir_ping.py`

### V2 · Referencia real (5 piezas por color)
Antes: **exporta y guarda el `.ft` que hay en el TXT.**
```
py -3.12 virtual/validacion/grabar_trazas.py --origen real --color blanca
```
Mete las 5 blancas una detrás de otra (espera a que caiga cada una) y pulsa Ctrl+C. Repite con `roja` y `azul`.
Mientras tanto:
- copia los `Color:` de la consola de ROBO Pro en `plantillas/real_consola.csv`;
- graba con el móvil a 60 fps 2-3 piezas por color y apunta los fotogramas en `plantillas/video_real.csv`;
- mide lo de `plantillas/medidas_fisicas.csv` (regla + vídeo).

### V3 · La misma prueba en la planta virtual
Unity en Play con la SLD, y en otra ventana:
```
py -3.12 virtual/validacion/grabar_trazas.py --origen virtual --color blanca
py -3.12 virtual/run_virtual.py
```
Mete 5 piezas blancas (tecla 1), Ctrl+C en el grabador. Repite con roja (2) y azul (3).

### V4 · Ver los ciclos
```
py -3.12 virtual/validacion/a_eventos.py virtual/validacion/trazas/real_blanca_*.jsonl
```
Tiene que salir un ciclo por pieza con la secuencia `OK`. Si no, revisa esa pieza antes de comparar.

### V5 · Comparar
```
py -3.12 virtual/validacion/comparar_trazas.py `
  --real    (ls virtual/validacion/trazas/real_*.jsonl) `
  --virtual (ls virtual/validacion/trazas/virtual_*.jsonl) `
  --video   virtual/validacion/plantillas/video_real.csv `
  --salida  "02-DOCS/wiki/proyecto/Validación planta real.md" `
  --grafica "02-DOCS/attachments/validacion/tiempos_real_vs_virtual.png"
```

### V6 · Calibrar y repetir
Con las medidas físicas, ajusta la planta virtual (`ParametrosSLD.asset` en Unity), repite V3 y V5.
Guarda el informe de **antes** y el de **después**.

### V7 · El código del alumno en la planta real
Carga `virtual/alumno/ejercicios/soluciones/nivel4.py` como programa principal en ROBO Pro Coding,
sin cambiar ninguna línea (usa `lib.controller`, que ya está en el proyecto del TXT). Sin
FactoryMain no hay `dt/*`: se valida con 15 piezas en su rampa y vídeo. Después, vuelve a cargar el `.ft` original.
