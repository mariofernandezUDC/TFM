---
tags: [concepto, componente]
---

# Barrera de luz (fototransistor)

Un LED emisor apunta a un fototransistor. Cuando una pieza **interrumpe el haz**, el fototransistor queda "a oscuras".

- API: `is_dark()`.
- Hay 10 en la planta (+1 usado como contador en el TXT 2).

## Modelo virtual
💻 Un *raycast* entre emisor y receptor: `is_dark = el rayo choca con una pieza`.
🔧 ❓ ¿Dónde está exactamente cada barrera? ¿A qué altura cruza la pieza?

Ejemplos: `I1` e `I3` de la [[SLD - Clasificadora]].
