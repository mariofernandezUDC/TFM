// SensoresDashboard.cs · Sensores visibles en el dashboard (D008, D009, P4b).
//
// Dos cosas, con los mismos datos de VirtualIO_TXT5:
//  1. Panel «Sensores y salidas»: una fila por E/S del TXT 5, con su punto
//     encendido (cian) o apagado y su valor.
//  2. Sobre la planta 3D: una etiqueta (I1, I2, I3, I6, I7, I8, C1) encima de cada
//     sensor, que se enciende en cian cuando el sensor se activa.
//
// Solo lee: no escribe entradas ni salidas. Las entradas son las que ve el programa
// del alumno (vio/txt5/in); las salidas, las que la planta aplica (lo recibido o el
// estado seguro).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantaVirtual
{
    [RequireComponent(typeof(UIDocument))]
    public class SensoresDashboard : MonoBehaviour
    {
        [Tooltip("E/S del TXT 5. Vacío = se busca en la escena")]
        public VirtualIO_TXT io;
        [Tooltip("Planta de la SLD: de aquí salen las posiciones de los sensores. Vacío = se busca en la escena")]
        public SLD_Planta planta;
        [Tooltip("Cámara que ve la planta. Vacío = Camera.main")]
        public Camera camara;
        [Tooltip("Pulsador contador C1 de la cinta (en el modelo: 9V_37783_Taster005_____FC_CintaTransportadora)")]
        public Transform contadorC1;
        [Tooltip("Altura de la etiqueta sobre el sensor, m")]
        public float alturaEtiqueta = 0.035f;

        public const string ClaseActiva = "es--activa";

        private VisualElement _raiz, _capa;
        private readonly Dictionary<string, Fila> _filas = new Dictionary<string, Fila>();
        private readonly Dictionary<string, Etiqueta> _etiquetas = new Dictionary<string, Etiqueta>();

        private class Fila { public VisualElement raiz; public Label valor; }
        private class Etiqueta { public VisualElement raiz; public Label texto; public string codigo; }

        // Filas del panel: código, nombre (como en el Inventario de E/S de la SLD).
        private static readonly (string codigo, string nombre)[] Entradas =
        {
            ("I1", "Barrera de entrada"), ("I2", "Sensor de color"), ("I3", "Barrera de cilindros"),
            ("I6", "Rampa blanca"), ("I7", "Rampa roja"), ("I8", "Rampa azul"), ("C1", "Contador de la cinta"),
        };
        private static readonly (string codigo, string nombre)[] Salidas =
        {
            ("M1", "Motor de la cinta"), ("O5", "Cilindro blanca"), ("O6", "Cilindro roja"),
            ("O7", "Cilindro azul"), ("O8", "Compresor"),
        };
        private static readonly string[] Sensores3D = { "I1", "I2", "I3", "I6", "I7", "I8", "C1" };

        private void OnEnable()
        {
            if (io == null) io = FindFirstObjectByType<VirtualIO_TXT>();
            if (planta == null) planta = FindFirstObjectByType<SLD_Planta>();
            if (camara == null) camara = Camera.main;
            _raiz = GetComponent<UIDocument>().rootVisualElement;
            _capa = _raiz.Q("capaEtiquetas");

            var lista = _raiz.Q("listaES");
            lista.Clear();
            _filas.Clear();
            AnadirGrupo(lista, "Entradas", Entradas);
            AnadirGrupo(lista, "Salidas", Salidas);

            _capa.Clear();
            _etiquetas.Clear();
            foreach (string codigo in Sensores3D)
            {
                var e = new Etiqueta { raiz = new VisualElement(), texto = new Label(codigo), codigo = codigo };
                e.raiz.AddToClassList("etiqueta-sensor");
                e.raiz.pickingMode = PickingMode.Ignore;
                e.texto.AddToClassList("etiqueta-sensor-texto");
                e.texto.AddToClassList("texto-fuerte");
                e.texto.pickingMode = PickingMode.Ignore;
                e.raiz.Add(e.texto);
                e.raiz.style.display = DisplayStyle.None; // hasta saber dónde va
                _capa.Add(e.raiz);
                _etiquetas[codigo] = e;
            }
        }

        private void AnadirGrupo(VisualElement lista, string titulo, (string codigo, string nombre)[] filas)
        {
            var cabecera = new Label(titulo);
            cabecera.AddToClassList("es-grupo");
            lista.Add(cabecera);
            foreach (var (codigo, nombre) in filas)
            {
                var fila = new VisualElement { name = "es:" + codigo };
                fila.AddToClassList("es-fila");
                var punto = new VisualElement();
                punto.AddToClassList("es-punto");
                var cod = new Label(codigo);
                cod.AddToClassList("es-codigo");
                cod.AddToClassList("texto-medio");
                var nom = new Label(nombre);
                nom.AddToClassList("es-nombre");
                var valor = new Label("");
                valor.AddToClassList("es-valor");
                fila.Add(punto); fila.Add(cod); fila.Add(nom); fila.Add(valor);
                lista.Add(fila);
                _filas[codigo] = new Fila { raiz = fila, valor = valor };
            }
        }

        // En LateUpdate: la cámara ya está colocada (EncuadreCamara) y la planta, movida.
        private void LateUpdate()
        {
            if (io == null) return;
            var en = io.entradas;
            bool i2Activa = planta != null && planta.parametros != null
                ? en.I2 < planta.parametros.tensionSinPieza.valor
                : en.I2 < 2500;

            Poner("I1", en.I1, en.I1 ? "pieza" : "");
            Poner("I2", i2Activa, en.I2.ToString());
            Poner("I3", en.I3, en.I3 ? "pieza" : "");
            Poner("I6", en.I6, en.I6 ? "pieza" : "");
            Poner("I7", en.I7, en.I7 ? "pieza" : "");
            Poner("I8", en.I8, en.I8 ? "pieza" : "");
            Poner("C1", en.C1, en.C1 ? "1" : "0");
            Poner("M1", io.M1_running, io.M1_running ? $"{io.M1_speed} · {io.M1_dir}" : "parado");
            Poner("O5", io.O5, io.O5 ? "fuera" : "");
            Poner("O6", io.O6, io.O6 ? "fuera" : "");
            Poner("O7", io.O7, io.O7 ? "fuera" : "");
            Poner("O8", io.O8, io.O8 ? "en marcha" : "");

            ColocarEtiquetas(i2Activa, en.I2);
        }

        private void Poner(string codigo, bool activa, string valor)
        {
            if (!_filas.TryGetValue(codigo, out var f)) return;
            f.raiz.EnableInClassList(ClaseActiva, activa);
            if (f.valor.text != valor) f.valor.text = valor;
            if (_etiquetas.TryGetValue(codigo, out var e)) e.raiz.EnableInClassList(ClaseActiva, activa);
        }

        // ── Etiquetas sobre la planta ────────────────────────────────────────

        private void ColocarEtiquetas(bool i2Activa, int i2)
        {
            if (camara == null || planta == null || _capa.panel == null) return;
            _etiquetas["I2"].texto.text = i2Activa ? "I2 · " + i2 : "I2";
            foreach (var e in _etiquetas.Values)
            {
                if (!PosicionSensor(e.codigo, out Vector3 mundo)) { e.raiz.style.display = DisplayStyle.None; continue; }
                mundo += Vector3.up * alturaEtiqueta;
                // Detrás de la cámara no se dibuja.
                if (camara.WorldToViewportPoint(mundo).z <= 0f) { e.raiz.style.display = DisplayStyle.None; continue; }
                Vector2 enPanel = RuntimePanelUtils.CameraTransformWorldToPanel(_capa.panel, mundo, camara);
                Vector2 local = _capa.WorldToLocal(enPanel);
                e.raiz.style.display = DisplayStyle.Flex;
                // La etiqueta queda centrada encima del punto (translate -50 % / -100 % en el USS).
                e.raiz.style.left = local.x;
                e.raiz.style.top = local.y;
            }
        }

        /// <summary>Punto del mundo donde está el sensor (centro del haz en las barreras).</summary>
        public bool PosicionSensor(string codigo, out Vector3 p)
        {
            p = default;
            switch (codigo)
            {
                case "I1": p = (planta.I1.emisor + planta.I1.receptor) * 0.5f; break;
                case "I3": p = (planta.I3.emisor + planta.I3.receptor) * 0.5f; break;
                case "I6": p = (planta.blanca.barrera.emisor + planta.blanca.barrera.receptor) * 0.5f; break;
                case "I7": p = (planta.roja.barrera.emisor + planta.roja.barrera.receptor) * 0.5f; break;
                case "I8": p = (planta.azul.barrera.emisor + planta.azul.barrera.receptor) * 0.5f; break;
                case "I2":
                    if (planta.cinta == null || planta.parametros == null) return false;
                    p = planta.cinta.PuntoMundo(planta.parametros.posI2, 0f, 0f);
                    break;
                case "C1":
                    if (contadorC1 == null) return false;
                    p = contadorC1.position;
                    break;
                default: return false;
            }
            return p != Vector3.zero; // las barreras están en cero hasta que la planta las coloca
        }

        /// <summary>True si la etiqueta 3D del sensor está encendida (para los tests).</summary>
        public bool EtiquetaEncendida(string codigo) =>
            _etiquetas.TryGetValue(codigo, out var e) && e.raiz.ClassListContains(ClaseActiva);

        /// <summary>True si la fila del panel está encendida (para los tests).</summary>
        public bool FilaEncendida(string codigo) =>
            _filas.TryGetValue(codigo, out var f) && f.raiz.ClassListContains(ClaseActiva);
    }
}
