# V2 · Resultados en la planta real (2026-11-17)

Maqueta Fischertechnik, SLD (extensión 5). Broker 1884 en el PC. Piezas metidas a mano.

## 1. FactoryMain-DigitalTwin (programa de la fábrica), con trazas `dt/sld/*`

Trazas: `virtual/validacion/trazas/real_{blanca,roja,azul}_2026111*.jsonl`. Ciclos sacados con `a_eventos.py`.

| Color metido | Ciclos | Secuencia OK | En su rampa |
|---|---:|---:|---:|
| Blanca | 5 | 5/5 | 5/5 |
| Roja | 11 | 11/11 | **7/11** (4 a la azul) |
| Azul | 5 | 5/5 | 5/5 |

Las 4 rojas que fueron a la azul: según Mario, una pieza roja «se rayaba» (lectura de color mala) y otra estaba en el límite. La planta decidió `BLUE` al llegar a I3.

### Tiempos por rampa (s, media ± desviación, tiempo del `ts` del TXT, muestreo ≥ 0,2 s)

t = 0 en el I1 que arranca la cinta.

| Rampa | n | I1→cinta | I1→I3 | I3→cilindro | I1→rampa |
|---|---:|---:|---:|---:|---:|
| blanca | 5 | 0.27 ± 0.08 | 4.84 ± 0.19 | 1.26 ± 0.10 | 6.32 ± 0.31 |
| roja | 7 | 0.33 ± 0.17 | 4.95 ± 0.66 | 2.79 ± 0.16 | 8.06 ± 0.65 |
| azul | 9 | 0.36 ± 0.18 | 4.66 ± 0.25 | 4.83 ± 0.14 | 9.78 ± 0.25 |

La rampa azul tiene n = 9: 5 azules + 4 rojas que cayeron en ella.
El I1→I3 de la roja 1 (6,37 s) es el que dispara su desviación; sin él, 4,71 ± 0,24 (n = 6).

## 2. Programa del alumno (`soluciones/nivel4.py` sin cambiar ninguna línea)

Proyecto `SLD_alumno_real.ft` (fuera del repo). Sin trazas: el TXT no se conectó al broker
durante la prueba (ningún cliente desde el TXT en el log de Mosquitto). Resultado contado por Mario
mirando las rampas.

| Color metido | Piezas | En su rampa |
|---|---:|---:|
| Blanca | 3 | 3/3 |
| Azul | 3 | 3/3 |
| Roja | 3 | **0/3** (las 3 a la azul) |

Causa: `nivel4.py` usa el umbral rojo/azul **1400** (por defecto de `SLD.py`). FactoryMain, al cargar
la calibración, llama a `set_calib_data_SLD()` (`vscode/lib/SLD.py:155-170`), que **fija 1460** e ignora
el archivo. Una roja que lee entre 1400 y 1460 va a la roja con FactoryMain y a la azul con el alumno.
