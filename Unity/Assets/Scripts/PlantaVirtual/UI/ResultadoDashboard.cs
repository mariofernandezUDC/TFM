// ResultadoDashboard.cs · Resultado, ciclo por pieza y retos del dashboard (D009, P4c).
//
// Cuenta cada pieza UNA vez, en el momento en que pasa a EnRampa (el evento de la
// pieza al llegar a una rampa). No usa I6–I8: esas barreras siguen tapadas mientras
// haya piezas en la rampa, y contar por su estado contaría la misma pieza muchas veces.
//
//  · Bien = la rampa en la que cae (cilindroIntentado) es la de su color real.
//  · Ciclo = tiempo de planta desde que aparece al principio de la cinta hasta que
//    llega a la rampa (SLD_Planta.tiempoPlanta_s; error de un frame como mucho).
//  · Al pulsar Cargar se ponen a cero las cuentas (las piezas que ya estaban en una
//    rampa no se vuelven a contar).
//
// Solo lee la planta: no cambia nada de SLD_Planta ni de las piezas.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantaVirtual
{
    [RequireComponent(typeof(UIDocument))]
    public class ResultadoDashboard : MonoBehaviour
    {
        [Tooltip("Planta de la SLD. Vacío = se busca en la escena")]
        public SLD_Planta planta;
        [Tooltip("Dashboard (para elegir el programa al pinchar un reto). Vacío = el del mismo GameObject")]
        public DashboardPlanta dashboard;
        [Tooltip("Barras que enseña «Ciclo por pieza» (las últimas piezas). Con más de 4, a 1366×768 la columna derecha llega a la salida")]
        public int barrasCiclo = 4;

        [Header("Estado")]
        [SoloLectura] public int bien;
        [SoloLectura] public int mal;
        [SoloLectura] public int enCinta;

        /// <summary>Una pieza que ha llegado a una rampa.</summary>
        public struct Llegada
        {
            public string nombre;
            public ColorPieza color, rampa;
            public float ciclo_s;
            public bool Bien => color == rampa;
        }

        private readonly List<Llegada> _llegadas = new List<Llegada>();
        private readonly Dictionary<int, double> _aparicion = new Dictionary<int, double>(); // id → tiempo de planta
        private readonly HashSet<int> _contadas = new HashSet<int>();

        private VisualElement _raiz, _resultado, _ciclo, _retos;
        private Label _porcentaje, _bienTxt, _malTxt, _cintaTxt, _media;
        private VisualElement _barraBien, _barrasCiclo;

        /// <summary>Piezas llegadas a una rampa desde el último Cargar, en orden.</summary>
        public IReadOnlyList<Llegada> Llegadas => _llegadas;

        // Retos: los ejercicios de virtual/alumno/ejercicios (LEEME.md).
        // Dificultad: Fácil = lima, Medio = cian, Avería = ámbar (D009). De momento no hay
        // ejercicios de avería: llegarán con F4.
        private static readonly (string ruta, string titulo, string detalle, string dificultad)[] Retos =
        {
            ("ejercicios/nivel1_arrancar.py", "Nivel 1 · Arrancar", "La cinta arranca cuando llega una pieza a I1", "Fácil"),
            ("ejercicios/nivel2_parar.py", "Nivel 2 · Parar", "La cinta se para delante de los cilindros (I3)", "Fácil"),
            ("ejercicios/nivel3_color.py", "Nivel 3 · Color", "Escribe si la pieza es blanca, roja o azul", "Medio"),
            ("ejercicios/nivel4_expulsar.py", "Nivel 4 · Expulsar", "La pieza acaba en su rampa", "Medio"),
        };

        private void OnEnable()
        {
            if (planta == null) planta = FindFirstObjectByType<SLD_Planta>();
            if (dashboard == null) dashboard = GetComponent<DashboardPlanta>();
            _raiz = GetComponent<UIDocument>().rootVisualElement;
            ConstruirResultado();
            ConstruirCiclo();
            if (dashboard != null) dashboard.ProgramaCargado += AlCargar;
            Pintar();
        }

        // En Start: DashboardPlanta ya ha leído los programas en su OnEnable.
        private void Start() => ConstruirRetos();

        private void OnDisable()
        {
            if (dashboard != null) dashboard.ProgramaCargado -= AlCargar;
        }

        private void AlCargar(string programa) => PonerACero();

        /// <summary>Pone a cero bien, mal y los ciclos. Las piezas ya en rampa no se vuelven a contar.</summary>
        public void PonerACero()
        {
            _llegadas.Clear();
            bien = mal = 0;
            Pintar();
        }

        // ── Contar ───────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (planta == null) return;
            double ahora = planta.tiempoPlanta_s;
            int nuevosEnCinta = 0;
            bool cambio = false;
            foreach (var p in planta.Piezas)
            {
                if (p == null) continue;
                int id = p.GetInstanceID();
                if (!_aparicion.ContainsKey(id)) _aparicion[id] = ahora; // primera vez que se ve: acaba de colocarse
                if (p.estado != EstadoPieza.EnRampa) { nuevosEnCinta++; continue; }
                if (!_contadas.Add(id)) continue; // ya contada: no se cuenta otra vez

                var llegada = new Llegada
                {
                    nombre = p.name, color = p.ColorReal, rampa = p.cilindroIntentado,
                    ciclo_s = (float)(ahora - _aparicion[id]),
                };
                _llegadas.Add(llegada);
                if (llegada.Bien) bien++; else mal++;
                cambio = true;
            }
            if (nuevosEnCinta != enCinta) { enCinta = nuevosEnCinta; cambio = true; }
            if (cambio) Pintar();
        }

        // ── Resultado ────────────────────────────────────────────────────────

        private void ConstruirResultado()
        {
            _resultado = _raiz.Q("resultado");
            _resultado.Clear();
            _porcentaje = new Label("—") { name = "porcentaje" };
            _porcentaje.AddToClassList("resultado-porcentaje");
            _porcentaje.AddToClassList("texto-fuerte");
            var sub = new Label("de acierto");
            sub.AddToClassList("resultado-sub");
            var fila = new VisualElement();
            fila.AddToClassList("resultado-cabeza");
            fila.Add(_porcentaje); fila.Add(sub);

            var barra = new VisualElement();
            barra.AddToClassList("resultado-barra");
            _barraBien = new VisualElement();
            _barraBien.AddToClassList("resultado-barra-bien");
            barra.Add(_barraBien);

            var cuentas = new VisualElement();
            cuentas.AddToClassList("resultado-cuentas");
            _bienTxt = Cuenta(cuentas, "Bien", "cuenta--bien");
            _malTxt = Cuenta(cuentas, "Mal", "cuenta--mal");
            _cintaTxt = Cuenta(cuentas, "En cinta", "cuenta--cinta");

            _resultado.Add(fila); _resultado.Add(barra); _resultado.Add(cuentas);
        }

        private static Label Cuenta(VisualElement padre, string titulo, string clase)
        {
            var caja = new VisualElement();
            caja.AddToClassList("cuenta");
            caja.AddToClassList(clase);
            var valor = new Label("0");
            valor.AddToClassList("cuenta-valor");
            valor.AddToClassList("texto-fuerte");
            var nombre = new Label(titulo);
            nombre.AddToClassList("cuenta-nombre");
            caja.Add(valor); caja.Add(nombre);
            padre.Add(caja);
            return valor;
        }

        // ── Ciclo por pieza ──────────────────────────────────────────────────

        private void ConstruirCiclo()
        {
            _ciclo = _raiz.Q("ciclo");
            _ciclo.Clear();
            var cabeza = new VisualElement();
            cabeza.AddToClassList("resultado-cabeza");
            _media = new Label("—") { name = "mediaCiclo" };
            _media.AddToClassList("resultado-porcentaje");
            _media.AddToClassList("texto-fuerte");
            var sub = new Label("de media, de la entrada a la rampa");
            sub.AddToClassList("resultado-sub");
            cabeza.Add(_media); cabeza.Add(sub);
            _barrasCiclo = new VisualElement();
            _barrasCiclo.AddToClassList("ciclo-barras");
            _ciclo.Add(cabeza); _ciclo.Add(_barrasCiclo);
        }

        // ── Retos ────────────────────────────────────────────────────────────

        private void ConstruirRetos()
        {
            _retos = _raiz.Q("retos");
            _retos.Clear();
            foreach (var (ruta, titulo, detalle, dificultad) in Retos)
            {
                if (dashboard != null && !dashboard.Programas.Contains(ruta)) continue;
                var tarjeta = new Button(() => dashboard?.Elegir(ruta)) { name = "reto:" + ruta, tooltip = "Elegir " + ruta };
                tarjeta.AddToClassList("reto");
                tarjeta.AddToClassList(dificultad switch { "Fácil" => "reto--facil", "Medio" => "reto--medio", _ => "reto--averia" });
                var cabeza = new VisualElement();
                cabeza.AddToClassList("reto-cabeza");
                cabeza.pickingMode = PickingMode.Ignore;
                var t = new Label(titulo) { pickingMode = PickingMode.Ignore };
                t.AddToClassList("reto-titulo");
                t.AddToClassList("texto-medio");
                var chip = new Label(dificultad) { pickingMode = PickingMode.Ignore };
                chip.AddToClassList("reto-chip");
                chip.AddToClassList("texto-medio");
                cabeza.Add(t); cabeza.Add(chip);
                var d = new Label(detalle) { pickingMode = PickingMode.Ignore };
                d.AddToClassList("reto-detalle");
                tarjeta.Add(cabeza); tarjeta.Add(d);
                _retos.Add(tarjeta);
            }
        }

        private void Update()
        {
            // La tarjeta del programa elegido se marca (cambia al elegir en el desplegable o en un reto).
            if (dashboard == null || _retos == null) return;
            foreach (var t in _retos.Children())
                t.EnableInClassList("reto--elegido", t.name == "reto:" + dashboard.programaElegido);
        }

        // ── Pintar ───────────────────────────────────────────────────────────

        private void Pintar()
        {
            if (_porcentaje == null) return;
            int total = bien + mal;
            _porcentaje.text = total == 0 ? "—" : $"{Mathf.RoundToInt(100f * bien / total)} %";
            _barraBien.style.width = Length.Percent(total == 0 ? 0f : 100f * bien / total);
            _bienTxt.text = bien.ToString();
            _malTxt.text = mal.ToString();
            _cintaTxt.text = enCinta.ToString();

            _media.text = _llegadas.Count == 0 ? "—" : $"{_llegadas.Average(l => l.ciclo_s):0.0} s";
            _barrasCiclo.Clear();
            var ultimas = _llegadas.Skip(Mathf.Max(0, _llegadas.Count - barrasCiclo)).ToList();
            float maximo = ultimas.Count == 0 ? 1f : Mathf.Max(1f, ultimas.Max(l => l.ciclo_s));
            foreach (var l in ultimas)
            {
                var fila = new VisualElement();
                fila.AddToClassList("ciclo-fila");
                var nombre = new Label(NombreColor(l.color));
                nombre.AddToClassList("ciclo-nombre");
                var pista = new VisualElement();
                pista.AddToClassList("ciclo-pista");
                var barra = new VisualElement();
                barra.AddToClassList("ciclo-barra");
                if (!l.Bien) barra.AddToClassList("ciclo-barra--mal");
                barra.style.width = Length.Percent(100f * l.ciclo_s / maximo);
                pista.Add(barra);
                var t = new Label($"{l.ciclo_s:0.0} s");
                t.AddToClassList("ciclo-tiempo");
                fila.Add(nombre); fila.Add(pista); fila.Add(t);
                _barrasCiclo.Add(fila);
            }
        }

        private static string NombreColor(ColorPieza c) => c switch
        {
            ColorPieza.Blanca => "Blanca",
            ColorPieza.Roja => "Roja",
            _ => "Azul",
        };
    }
}
