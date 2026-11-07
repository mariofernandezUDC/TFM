# Pista · Nivel 3

**Hueco 1.** El sensor lee sin parar mientras la pieza pasa por debajo. Las primeras lecturas pueden pillar la pieza a medias, mezclada con la cinta. La lectura buena es la **más baja**, porque es cuando la pieza tapa del todo el sensor. En cada vuelta del bucle:

```python
minimo = min(minimo, <lo que lee el sensor ahora>)
```

**Hueco 2.** Compara `minimo` con los umbrales, de uno en uno, con `if` / `elif`. En Python se puede escribir un rango así:

```python
if UMBRAL_MINIMO <= minimo < UMBRAL_BLANCO_ROJO:
  color = "blanca"
```

Haz lo mismo para la roja y la azul. Si no encaja en ningún rango, deja `color = None`.
