"""Pantalla del TXT, virtual: `display.set_attr()` solo imprime.

En el TXT real `lib.display` lo pone el sistema y no está en el repositorio.
Este archivo está en `virtual/lib/` SIN `__init__.py`, igual que `vscode/lib/`.
Así las dos carpetas forman un único paquete `lib` (namespace package):
`lib.display` sale de aquí y `lib.SLD` sale de `vscode/lib/`.
Si algún día se añade `vscode/lib/__init__.py`, esto deja de funcionar
(ver virtual/README.md).
"""


class _Pantalla:
  def set_attr(self, clave, valor):
    print("[display] {} = {}".format(clave, valor), flush=True)


display = _Pantalla()
