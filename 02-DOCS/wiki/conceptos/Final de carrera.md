---
tags: [concepto, componente]
---

# Final de carrera (*mini switch*)

Pulsador que se cierra cuando un mecanismo llega a una posición. Sirve para **referenciar** ejes (ir a "cero") y para detectar posiciones fijas.

- API: `is_closed()`, `is_open()`, `get_state()`.
- Hay 17 en la planta + 1 usado como [[Contador por pulsos]] (SLD).

## Referenciado típico
El motor gira hacia el final de carrera **hasta que se cierra**, para y pone el contador a 0. Ver [[Hilos y sondeo (polling)]].

## Modelo virtual
🔧 Un *trigger* en la posición del final de carrera. ❓ ¿Tiene histéresis? ¿Se cierra justo en el tope o un poco antes?

Fallo típico a simular (F4): "el final de carrera no actúa" → el eje choca con el tope.
