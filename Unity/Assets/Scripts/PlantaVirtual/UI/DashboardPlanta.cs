// DashboardPlanta.cs · Dashboard de la planta virtual (D008, P4a: esqueleto).
//
// UI Toolkit (Dashboard.uxml + Dashboard.uss). Barra superior: programa,
// Cargar, Parar y estado. Abajo: lo que imprime el programa (~4 líneas; el log de
// la librería, tras «Ver detalle»). Columnas con
// tarjetas plegables, vacías hasta P4b/P4c. En el hueco central se ve la planta 3D
// (lo encuadra EncuadreCamara).
//
// Este script solo maneja la UI. Lanzar y parar lo hace LanzadorPrograma (P2–P3).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public string programaInicial = "ejercicios/nivel1_arrancar.py";
        [Tooltip("Líneas que guarda la caja de salida")]
        public int maxLineasSalida = 300;

        [Header("Escala")]
        [Tooltip("Resolución para la que está diseñado el dashboard (escala 1)")]
        public Vector2 resolucionReferencia = new Vector2(1920, 1080);
        [Tooltip("Ningún texto baja de este tamaño en pantalla, px")]
        public float letraMinimaPantalla = 12f;
        [Tooltip("Letra más pequeña del Dashboard.uss, px")]
        public float letraMinimaUss = 14f;

        [Header("Estado")]
        [SoloLectura] public string programaElegido = "";
        [SoloLectura] public int programasEncontrados;
        [SoloLectura] public float escala = 1f;

        public const string CarpetaAlumno = "virtual/alumno";

        // Lo que es del profesor y el alumno no ve en el desplegable: las soluciones
        // y la plantilla. Un futuro «modo profesor» (F4) los enseñará poniendo
        // _modoProfesor a true; en P4 no hay forma de activarlo.
        private static readonly string[] CarpetasProfesor = { "ejercicios/soluciones/" };
        private static readonly string[] ArchivosProfesor = { "plantilla_sld.py" };
        private bool _modoProfesor = false;

        /// <summary>True si el programa (ruta relativa a virtual/alumno, con /) es solo del profesor.</summary>
        public static bool EsDelProfesor(string rel) =>
            CarpetasProfesor.Any(c => rel.StartsWith(c, StringComparison.OrdinalIgnoreCase)) ||
            ArchivosProfesor.Any(a => string.Equals(rel, a, StringComparison.OrdinalIgnoreCase));

        private VisualElement _raiz, _lista, _estado, _tarjetaSalida;
        private ScrollView _salida, _listaScroll;
        private Label _programaTexto, _estadoTexto;
        private Button _selector, _recargar, _cargar, _parar, _botonDetalle, _ampliarSalida;
        private readonly List<string> _programas = new List<string>();

        private bool _enMarchaAntes;
        private float _inicioMarcha;
        private string _textoEstado = "";

        /// <summary>Se lanza al pulsar Cargar, con el programa elegido (ResultadoDashboard pone a cero sus cuentas).</summary>
        public event Action<string> ProgramaCargado;

        /// <summary>Programas encontrados en virtual/alumno (rutas relativas con /).</summary>
        public IReadOnlyList<string> Programas => _programas;

        private static string RaizRepo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        private void OnEnable()
        {
            if (lanzador == null) lanzador = FindFirstObjectByType<LanzadorPrograma>();
            UsarPanelPropio();
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
            _tarjetaSalida = _raiz.Q("tarjetaSalida");
            _botonDetalle = _raiz.Q<Button>("verDetalle");
            _ampliarSalida = _raiz.Q<Button>("ampliarSalida");

            _raiz.Q("programaFlecha").Add(new IconoDibujado(IconoDibujado.Forma.Flecha));
            _raiz.Q("recargarIcono").Add(new IconoDibujado(IconoDibujado.Forma.Recargar));

            _selector.clicked += AlternarLista;
            _recargar.clicked += LeerProgramas;
            _cargar.clicked += Cargar;
            _parar.clicked += Parar;
            _botonDetalle.clicked += AlternarDetalle;
            _ampliarSalida.clicked += AlternarAmpliarSalida;
            foreach (var b in _raiz.Query<Button>(className: "boton-plegar").ToList())
                if (b != _ampliarSalida) b.clicked += () => Plegar(b);
            // Un clic fuera de la lista la cierra.
            _raiz.RegisterCallback<PointerDownEvent>(AlPulsarFuera, TrickleDown.TrickleDown);

            if (lanzador != null)
            {
                lanzador.LineaSalida += AnadirLinea;
                lanzador.NoArranca += AlNoArrancar;
            }

            LeerProgramas();
            Elegir(_programas.Contains(programaInicial) ? programaInicial : _programas.FirstOrDefault());
            ActualizarEstado(forzar: true);
        }

        private void OnDestroy()
        {
            if (_panelPropio != null) Destroy(_panelPropio);
        }

        private void OnDisable()
        {
            if (lanzador != null)
            {
                lanzador.LineaSalida -= AnadirLinea;
                lanzador.NoArranca -= AlNoArrancar;
            }
            if (_raiz != null) _raiz.UnregisterCallback<PointerDownEvent>(AlPulsarFuera, TrickleDown.TrickleDown);
        }

        private void Update()
        {
            ActualizarEscala();
            ActualizarEstado(forzar: false);
        }

        // ── Escala (texto legible en un portátil de 1366×768) ────────────────

        private PanelSettings _panelPropio;

        // El PanelSettings del asset escala con la pantalla sin límite inferior: a 1366×768
        // queda en 0,71 y la letra de 14 px se ve de 10 px. Se usa una copia en tiempo de
        // ejecución con tamaño de píxel constante, y la escala la pone ActualizarEscala().
        // Así el asset no cambia al jugar en el editor.
        private void UsarPanelPropio()
        {
            var doc = GetComponent<UIDocument>();
            if (_panelPropio == null && doc.panelSettings != null)
            {
                _panelPropio = Instantiate(doc.panelSettings);
                _panelPropio.name = doc.panelSettings.name + " (escala propia)";
                _panelPropio.scaleMode = PanelScaleMode.ConstantPixelSize;
                doc.panelSettings = _panelPropio;
            }
            ActualizarEscala();
        }

        /// <summary>
        /// Escala = la que cabe en la pantalla respecto a la referencia, pero nunca menor que
        /// letraMinimaPantalla / letraMinimaUss (12/14 ≈ 0,86).
        /// </summary>
        public static float CalcularEscala(float ancho, float alto, Vector2 referencia, float letraMinimaPantalla, float letraMinimaUss)
        {
            float cabe = Mathf.Min(ancho / referencia.x, alto / referencia.y);
            return Mathf.Max(cabe, letraMinimaPantalla / letraMinimaUss);
        }

        private void ActualizarEscala()
        {
            if (_panelPropio == null) return;
            float e = CalcularEscala(Screen.width, Screen.height, resolucionReferencia, letraMinimaPantalla, letraMinimaUss);
            if (Mathf.Approximately(e, _panelPropio.scale)) return;
            _panelPropio.scale = e;
            escala = e;
        }

        // ── Programas (D008-C) ───────────────────────────────────────────────

        /// <summary>
        /// Busca los .py de virtual/alumno de forma recursiva, sin pistas/ ni __pycache__/
        /// y sin lo que es del profesor (EsDelProfesor).
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
                    if (!_modoProfesor && EsDelProfesor(rel)) continue;
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
            _enBloqueError = false;
            AnadirAviso("── Cargando " + programaElegido + " ──");
            ProgramaCargado?.Invoke(programaElegido);
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
            bool error = !enMarcha && (codigo == 1 || codigo == 2 || codigo == LanzadorPrograma.CodigoNoArranca);

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

        // Línea de log de la librería: «2026-11-11 13:54:35,262 [MainThread] WARNING …  #vio.py:115»
        // (FORMATO_LOG de run_virtual.py). El grupo 1 es el nivel.
        private static readonly Regex LineaLog =
            new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3} \[[^\]]*\] (\w+)", RegexOptions.Compiled);
        // Líneas del lanzador (run_virtual.py) que no son del programa del alumno.
        private static readonly string[] LineasLanzador = { "Ejecutando tu programa:", "Broker " };
        // run_virtual.py encierra el error del alumno entre dos rayas de 70 «=».
        private static readonly string Raya = new string('=', 70);

        private bool _enBloqueError;

        /// <summary>
        /// Clasifica una línea del programa. A la vista: lo que imprime el alumno y los
        /// errores (también los ERROR/CRITICAL de la librería). Detrás de «Ver detalle»:
        /// el resto del log de la librería, las rayas y las líneas del lanzador.
        /// </summary>
        public static string ClaseDeLinea(string texto, bool esError, ref bool enBloqueError)
        {
            if (texto.TrimEnd() == Raya) { enBloqueError = !enBloqueError; return ClaseDetalle; }
            if (enBloqueError) return ClaseError;
            Match log = LineaLog.Match(texto);
            if (log.Success)
                return log.Groups[1].Value == "ERROR" || log.Groups[1].Value == "CRITICAL" ? ClaseError : ClaseDetalle;
            if (LineasLanzador.Any(l => texto.StartsWith(l, StringComparison.Ordinal))) return ClaseDetalle;
            return esError ? ClaseError : null;
        }

        public const string ClaseDetalle = "salida-linea--detalle";
        public const string ClaseError = "salida-linea--error";

        private void AnadirLinea(string texto, bool esError)
        {
            bool antes = _enBloqueError;
            Anadir(texto, ClaseDeLinea(texto, esError, ref _enBloqueError));
            // P5: al empezar el bloque de error, la salida se amplía para que se lea entero.
            if (!antes && _enBloqueError) AmpliarSalida(true);
        }

        // P5: el programa no ha llegado a arrancar. Mensaje en rojo y estado «Error».
        private void AlNoArrancar(string texto)
        {
            Anadir(texto, ClaseError);
            ActualizarEstado(forzar: true);
        }

        private void AnadirAviso(string texto) => Anadir(texto, "salida-linea--aviso");

        private void Anadir(string texto, string clase)
        {
            var linea = new Label(texto);
            linea.AddToClassList("salida-linea");
            if (clase != null) linea.AddToClassList(clase);
            if (clase == ClaseDetalle) _lineasDetalle++;
            _salida.Add(linea);
            while (_salida.contentContainer.childCount > maxLineasSalida)
            {
                if (_salida.contentContainer[0].ClassListContains(ClaseDetalle)) _lineasDetalle--;
                _salida.contentContainer.RemoveAt(0);
            }
            ActualizarBotonDetalle();
            BajarAlFinal();
        }

        // Baja al final cuando la línea nueva (o la caja con su nuevo alto) ya tiene tamaño.
        private void BajarAlFinal() =>
            _salida.schedule.Execute(() => _salida.scrollOffset = new Vector2(0, float.MaxValue));

        private int _lineasDetalle;
        private bool _verDetalle;
        private bool _salidaAmpliada;

        private void AlternarDetalle()
        {
            _verDetalle = !_verDetalle;
            _tarjetaSalida.EnableInClassList("tarjeta-salida--detalle", _verDetalle);
            ActualizarBotonDetalle();
            BajarAlFinal();
        }

        private void ActualizarBotonDetalle()
        {
            _botonDetalle.text = _verDetalle ? "Ocultar detalle"
                : _lineasDetalle > 0 ? $"Ver detalle ({_lineasDetalle})" : "Ver detalle";
        }

        // La salida no se pliega del todo: de ~4 líneas pasa a ~12 y vuelve.
        private void AlternarAmpliarSalida() => AmpliarSalida(!_salidaAmpliada);

        private void AmpliarSalida(bool ampliada)
        {
            _salidaAmpliada = ampliada;
            _tarjetaSalida.EnableInClassList("tarjeta-salida--ampliada", _salidaAmpliada);
            _ampliarSalida.text = _salidaAmpliada ? "−" : "+";
            BajarAlFinal();
        }

        /// <summary>Texto de todas las líneas de la caja de salida, también las de detalle (para los tests).</summary>
        public string TextoSalida => string.Join("\n", _salida.contentContainer.Children().Select(l => ((Label)l).text));

        /// <summary>Texto de las líneas en rojo (errores), para los tests.</summary>
        public string TextoSalidaError => string.Join("\n", _salida.contentContainer.Children()
            .Where(l => l.ClassListContains(ClaseError)).Select(l => ((Label)l).text));

        /// <summary>True si la salida está ampliada (~12 líneas), para los tests.</summary>
        public bool SalidaAmpliada => _salidaAmpliada;

        /// <summary>Texto de las líneas que se ven sin pulsar «Ver detalle» (para los tests).</summary>
        public string TextoSalidaVisible => string.Join("\n", _salida.contentContainer.Children()
            .Where(l => !l.ClassListContains(ClaseDetalle)).Select(l => ((Label)l).text));

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
