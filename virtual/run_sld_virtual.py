"""Arranca solo la SLD (`thread_SLD` de vscode/lib/SLD.py) sobre la librería virtual.

Se mantiene para no cambiar los comandos del hito 1. Hace lo mismo que
`run_virtual.py` sin `--programa` (todo el trabajo está allí).

Uso:  py -3.12 virtual/run_sld_virtual.py [--broker localhost] [--puerto 1883] [--log INFO]
"""

import sys

from run_virtual import main

if __name__ == "__main__":
  if any(a == "--programa" or a.startswith("--programa=") for a in sys.argv[1:]):
    sys.exit("run_sld_virtual.py solo lanza SLD.py. Para tu programa usa run_virtual.py --programa ruta.py")
  sys.exit(main())
