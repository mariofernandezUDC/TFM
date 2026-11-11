// DashboardPlanta.cs · Dashboard de la planta virtual (D008, P4a: esqueleto).
//
// UI Toolkit (Dashboard.uxml + Dashboard.uss). Barra superior: programa,
// Cargar, Parar y estado. Abajo: lo que imprime el programa. Columnas con
// tarjetas plegables, vacías hasta P4b/P4c. En el hueco central se ve la planta 3D
// (lo encuadra EncuadreCamara).
//
// Este script solo maneja la UI. Lanzar y parar lo hace LanzadorPrograma (P2–P3).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantaVirtual
{
    [RequireComponent(typeof(UIDocument))]
    public class DashboardPlanta : MonoBehaviour
    {
        [Tooltip("Lanzador de programas. Vacío = se busca en la escena")]
        public LanzadorPrograma lanzador;
        [Tooltip("Programa elegido al abrir el dashboard, relativo a virtual/alumno/")]
        public string programaInicial = "ejercicios/soluciones/nivel4.py";
        [Tooltip("Líneas que guarda la caja de salida")]
        public int maxLineasSalida = 300;

        [Header("Estado")]
        [SoloLectura] public string programaElegido = "";
        [SoloLectura] public int programasEncontrados;

        public const string CarpetaAlumno = "virtual/alumno";

        private VisualElement _raiz, _lista, _estado;
        private ScrollView _salida, _listaScroll;
        private Label _programaTexto, _estadoTexto;
        private Button _selector, _recargar, _cargar, _parar;
        private readonly List<string> _programas = new List<string>();

        private bool _enMarchaAntes;
        private float _inicioMarcha;
        private string _textoEstado = "";

        /// <summary>Programas encontrados en virtual/alumno (rutas relativas con /).</summary>
        public IReadOnlyList<string> Programas => _programas;

        private static string RaizRepo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        private void OnEnable()
        {
            if (lanzador == null) lanzador = FindFirstObjectByType<LanzadorPrograma>();
            _raiz = GetComponent<UIDocument>().rootVisualElement;

            _selector = _raiz.Q<Button>("programaSeleccionado");
            _programaTexto = _raiz.Q<Label>("programaTexto");
            _recargar = _raiz.Q<Button>("recargarProgramas");
            _cargar = _raiz.Q<Button>("cargar");
            _parar = _raiz.Q<Button>("parar");
            _estado = _raiz.Q("estado");
            _estadoTexto = _raiz.Q<Label>("estadoTexto");
            _salida = _raiz.Q<ScrollView>("salida");
            _lista = _raiz.Q("listaProgramas");
            _listaScroll = _raiz.Q<ScrollView>("listaProgramasScroll");

            _raiz.Q("programaFlecha").Add(new IconoDibujado(IconoDibujado.Forma.Flecha));
            _raiz.Q("recargarIcono").Add(new IconoDibujado(IconoDibujado.Forma.Recargar));

            _selector.clicked += AlternarLista;
            _recargar.clicked += LeerProgramas;
            _cargar.clicked += Cargar;
            _parar.clicked += Parar;
            foreach (var b in _raiz.Query<Button>(className: "boton-plegar").ToList())
                b.clicked += () => Plegar(b);
            // Un clic fuera de la lista la cierra.
            _raiz.RegisterCallback<PointerDownEvent>(AlPulsarFuera, TrickleDown.TrickleDown);

            if (lanzador != null) lanzador.LineaSalida += AnadirLinea;

            LeerProgramas();
            Elegir(_programas.Contains(programaInicial) ? programaInicial : _programas.FirstOrDefault());
            ActualizarEstado(forzar: true);
        }

        private void OnDisable()
        {
            if (lanzador != null) lanzador.LineaSalida -= AnadirLinea;
            if (_raiz != null) _raiz.UnregisterCallback<PointerDownEvent>(AlPulsarFuera, TrickleDown.TrickleDown);
        }

        private void Update() => ActualizarEstado(forzar: false);

        // ── Programas (D008-C) ───────────────────────────────────────────────

        /// <summary>
        /// Busca los .py de virtual/alumno de forma recursiva, sin pistas/ ni __pycache__/.
        /// En la lista se ve la ruta relativa, por ejemplo ejercicios/nivel1_arrancar.py.
        /// </summary>
        public void LeerProgramas()
        {
            _programas.Clear();
            string carpeta = Path.Combine(RaizRepo, CarpetaAlumno);
            if (Directory.Exists(carpeta))
            {
                foreach (string ruta in Directory.GetFiles(carpeta, "*.py", SearchOption.AllDirectories))
                {
                    string rel = Path.GetRelativePath(carpeta, ruta).Replace('\\', '/');
                    var partes = rel.Split('/');
                    if (partes.Contains("pistas") || partes.Contains("__pycache__")) continue;
                    _programas.Add(rel);
                }
            }
            else
            {
                AnadirAviso("No encuentro la carpeta de programas: " + carpeta);
            }
            _programas.Sort(StringComparer.OrdinalIgnoreCase);
            programasEncontrados = _programas.Count;

            _listaScroll.Clear();
            foreach (string p in _programas)
            {
                var item = new Button(() => { Elegir(p); CerrarLista(); }) { text = p, name = "programa:" + p };
                item.AddToClassList("lista-item");
                _listaScroll.Add(item);
            }
            if (!_programas.Contains(programaElegido)) Elegir(_programas.FirstOrDefault());
            else Elegir(programaElegido);
        }

        /// <summary>Elige un programa de la lista (lo mismo que pinchar en él).</summary>
        public void Elegir(string programa)
        {
            programaElegido = programa ?? "";
            _programaTexto.text = string.IsNullOrEmpty(programaElegido) ? "No hay programas" : programaElegido;
            foreach (var item in _listaScroll.Children())
                item.EnableInClassList("lista-item--elegido", item.name == "programa:" + programaElegido);
        }

        private void AlternarLista()
        {
            if (_lista.ClassListContains("lista-programas--oculta")) AbrirLista();
            else CerrarLista();
        }

        private void AbrirLista()
        {
            // La lista va al final de la raíz para quedar por encima de las tarjetas.
            // Se coloca justo debajo del selector (posición calculada, no de estilo).
            Rect sel = _selector.parent.worldBound;
            Vector2 local = _raiz.WorldToLocal(new Vector2(sel.xMin, sel.yMax));
            _lista.style.left = local.x;
            _lista.style.top = local.y + 8;
            _lista.RemoveFromClassList("lista-programas--oculta");
        }

        private void CerrarLista() => _lista.AddToClassList("lista-programas--oculta");

        private void AlPulsarFuera(PointerDownEvent e)
        {
            if (_lista.ClassListContains("lista-programas--oculta")) return;
            var destino = e.target as VisualElement;
            if (destino != null && (_lista.Contains(destino) || _selector.Contains(destino))) return;
            CerrarLista();
        }

        // ── Cargar y Parar ───────────────────────────────────────────────────

        private void Cargar()
        {
            CerrarLista();
            if (lanzador == null) { AnadirAviso("No hay LanzadorPrograma en la escena."); return; }
            if (string.IsNullOrEmpty(programaElegido)) { AnadirAviso("Elige un programa antes de cargar."); return; }
            AnadirAviso("── Cargando " + programaElegido + " ──");
            lanzador.programa = programaElegido;
            lanzador.Lanzar();
            ActualizarEstado(forzar: true);
        }

        private void Parar()
        {
            CerrarLista();
            if (lanzador == null) return;
            if (lanzador.enMarcha) AnadirAviso("── Parado ──");
            lanzador.Parar();
            ActualizarEstado(forzar: true);
        }

        // ── Estado: Parado / En marcha + tiempo / Error ──────────────────────

        private void ActualizarEstado(bool forzar)
        {
            bool enMarcha = lanzador != null && lanzador.enMarcha;
            if (enMarcha && !_enMarchaAntes) _inicioMarcha = Time.realtimeSinceStartup;
            _enMarchaAntes = enMarcha;

            int codigo = lanzador != null ? lanzador.ultimoCodigoSalida : LanzadorPrograma.SinCodigo;
            bool error = !enMarcha && (codigo == 1 || codigo == 2);

            string texto;
            if (enMarcha)
            {
                int s = (int)(Time.realtimeSinceStartup - _inicioMarcha);
                texto = $"En marcha · {s / 60:00}:{s % 60:00}";
            }
            else texto = error ? "Error" : "Parado";

            if (!forzar && texto == _textoEstado) return;
            _textoEstado = texto;
            _estadoTexto.text = texto;
            _estado.EnableInClassList("estado--en-marcha", enMarcha);
            _estado.EnableInClassList("estado--error", error);
        }

        // ── Salida del programa ──────────────────────────────────────────────

        private void AnadirLinea(string texto, bool esError) => Anadir(texto, null);

        private void AnadirAviso(string texto) => Anadir(texto, "salida-linea--aviso");

        private void Anadir(string texto, string clase)
        {
            var linea = new Label(texto);
            linea.AddToClassList("salida-linea");
            if (clase != null) linea.AddToClassList(clase);
            _salida.Add(linea);
            while (_salida.contentContainer.childCount > maxLineasSalida)
                _salida.contentContainer.RemoveAt(0);
            // Baja al final cuando la línea nueva ya tiene tamaño.
            _salida.schedule.Execute(() => _salida.scrollOffset = new Vector2(0, float.MaxValue));
        }

        /// <summary>Texto de todas las líneas de la caja de salida (para los tests).</summary>
        public string TextoSalida => string.Join("\n", _salida.contentContainer.Children().Select(l => ((Label)l).text));

        // ── Tarjetas plegables ───────────────────────────────────────────────

        private static void Plegar(Button boton)
        {
            var tarjeta = boton.parent;
            while (tarjeta != null && !tarjeta.ClassListContains("tarjeta")) tarjeta = tarjeta.parent;
            if (tarjeta == null) return;
            bool plegada = !tarjeta.ClassListContains("tarjeta--plegada");
            tarjeta.EnableInClassList("tarjeta--plegada", plegada);
            boton.text = plegada ? "+" : "−";
        }
    }

    /// <summary>
    /// Icono vectorial dibujado con Painter2D, del color del texto de su padre.
    /// Outfit no trae «↻» ni «▾», así que se dibujan.
    /// </summary>
    public class IconoDibujado : VisualElement
    {
        public enum Forma { Flecha, Recargar }
        private readonly Forma _forma;

        public IconoDibujado(Forma forma)
        {
            _forma = forma;
            pickingMode = PickingMode.Ignore;
            AddToClassList("icono");
            generateVisualContent += Dibujar;
        }

        private void Dibujar(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            float w = r.width, h = r.height;
            if (w <= 0 || h <= 0) return;
            var p = ctx.painter2D;
            p.strokeColor = resolvedStyle.color;
            p.fillColor = resolvedStyle.color;
            p.lineWidth = Mathf.Max(1.6f, w * 0.12f);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            if (_forma == Forma.Flecha)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(w * 0.2f, h * 0.38f));
                p.LineTo(new Vector2(w * 0.5f, h * 0.66f));
                p.LineTo(new Vector2(w * 0.8f, h * 0.38f));
                p.Stroke();
            }
            else
            {
                var c = new Vector2(w * 0.5f, h * 0.5f);
                float radio = w * 0.36f;
                p.BeginPath();
                p.Arc(c, radio, 40f, 330f);
                p.Stroke();
                // Punta de flecha al final del arco (330°).
                float a = 330f * Mathf.Deg2Rad;
                var fin = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio;
                float t = w * 0.22f;
                p.BeginPath();
                p.MoveTo(fin + new Vector2(t * 0.15f, -t));
                p.LineTo(fin + new Vector2(t * 0.25f, t * 0.15f));
                p.LineTo(fin + new Vector2(-t, -t * 0.1f));
                p.Stroke();
            }
        }
    }
}
