// EncuadreCamara.cs · Encuadra la cámara para que la clasificadora quede en el
// hueco central del dashboard (D008, P4a).
//
// No mueve nada de la planta: solo la cámara. Mantiene una orientación fija y
// calcula la distancia y el desplazamiento para que la caja de los objetivos quepa en
// el elemento «hueco» del dashboard. Se recalcula cuando el hueco cambia de
// tamaño (al plegar un panel o cambiar la ventana).

using UnityEngine;
using UnityEngine.UIElements;

namespace PlantaVirtual
{
    public class EncuadreCamara : MonoBehaviour
    {
        [Tooltip("Cámara que se encuadra. Vacío = Camera.main")]
        public Camera camara;
        [Tooltip("Lo que tiene que verse entero en el hueco: la SLD completa (cinta, 3 cilindros y 3 rampas)")]
        public Transform[] objetivos;
        [Tooltip("Dashboard con el elemento del hueco central")]
        public UIDocument dashboard;
        [Tooltip("Nombre del elemento del hueco central en el UXML")]
        public string nombreHueco = "hueco";
        [Tooltip("Orientación de la cámara (grados). 55° hacia abajo, desde el lado de los cilindros: la cinta queda horizontal, los cilindros delante y las rampas detrás. Con el rumbo de la vista «Estación SLD» (162°) se mira a lo largo de la cinta y queda en escorzo")]
        public Vector3 angulos = new Vector3(55f, 90f, 0f);
        [Tooltip("Aire alrededor de los objetivos (1 = justo)")]
        [Range(1f, 2f)] public float margen = 1.12f;
        [Tooltip("Plano de recorte cercano, m. El de la escena (0,3 m) no deja acercar la cámara a una maqueta de 1 m")]
        public float planoCercano = 0.02f;

        [Header("Estado")]
        [SoloLectura] public Vector3 centroObjetivo;
        [SoloLectura] public float distancia;

        private VisualElement _hueco;
        private bool _pendiente = true;
        private int _anchoPantalla, _altoPantalla;

        private void OnEnable()
        {
            if (camara == null) camara = Camera.main;
            if (dashboard != null)
            {
                _hueco = dashboard.rootVisualElement.Q(nombreHueco);
                _hueco?.RegisterCallback<GeometryChangedEvent>(AlCambiarHueco);
            }
            _pendiente = true;
        }

        private void OnDisable() => _hueco?.UnregisterCallback<GeometryChangedEvent>(AlCambiarHueco);

        private void AlCambiarHueco(GeometryChangedEvent e) => _pendiente = true;

        // En LateUpdate para ir después de cualquier script que coloque la cámara en Start.
        private void LateUpdate()
        {
            if (Screen.width != _anchoPantalla || Screen.height != _altoPantalla) _pendiente = true;
            if (_pendiente) Encuadrar();
        }

        /// <summary>Coloca la cámara para que los objetivos queden dentro del hueco central.</summary>
        public void Encuadrar()
        {
            if (camara == null || objetivos == null) return;
            bool hay = false;
            Bounds caja = default;
            foreach (var o in objetivos)
            {
                if (o == null) continue;
                foreach (var r in o.GetComponentsInChildren<Renderer>())
                {
                    if (!hay) { caja = r.bounds; hay = true; }
                    else caja.Encapsulate(r.bounds);
                }
            }
            if (!hay) return;

            // Hueco en fracciones de pantalla (0–1). Sin dashboard: toda la pantalla.
            Rect hueco = new Rect(0, 0, 1, 1);
            if (_hueco != null && _hueco.panel != null)
            {
                Rect raiz = _hueco.panel.visualTree.worldBound;
                Rect h = _hueco.worldBound;
                if (raiz.width <= 0 || h.width <= 0 || h.height <= 0) return; // aún sin maquetar
                hueco = new Rect(h.x / raiz.width, h.y / raiz.height, h.width / raiz.width, h.height / raiz.height);
            }

            camara.nearClipPlane = planoCercano;
            Quaternion rot = Quaternion.Euler(angulos);
            float tanV = Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * camara.aspect;

            // Centro del hueco en coordenadas normalizadas (−1…1; en la UI la y crece hacia abajo)
            // y medio tamaño del hueco, reducido por el margen.
            Vector3 c = caja.center, e = caja.extents;
            float nx = (hueco.center.x * 2f) - 1f;
            float ny = 1f - (hueco.center.y * 2f);
            float mx = hueco.width / margen, my = hueco.height / margen;

            // Esquinas de la caja respecto a su centro, en ejes de la cámara.
            var esquinas = new Vector3[8];
            Quaternion inv = Quaternion.Inverse(rot);
            for (int i = 0; i < 8; i++)
                esquinas[i] = inv * new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);

            // Con la cámara a distancia d y el centro de la caja en (tx, ty), rectángulo que
            // ocupan las 8 esquinas en pantalla. En perspectiva no queda centrado en (tx, ty).
            bool Proyectar(float d, float tx, float ty, out Rect r)
            {
                var centro = new Vector3(tx * d * tanH, ty * d * tanV, d);
                float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
                r = default;
                foreach (var q in esquinas)
                {
                    Vector3 p = centro + q;
                    if (p.z <= camara.nearClipPlane) return false;
                    float px = p.x / (p.z * tanH), py = p.y / (p.z * tanV);
                    x0 = Mathf.Min(x0, px); x1 = Mathf.Max(x1, px);
                    y0 = Mathf.Min(y0, py); y1 = Mathf.Max(y1, py);
                }
                r = Rect.MinMaxRect(x0, y0, x1, y1);
                return true;
            }

            // Se alternan dos ajustes: la distancia mínima a la que la caja cabe en el
            // hueco (bisección) y el desplazamiento que centra su proyección en el hueco.
            float tx = nx, ty = ny, d = 1f;
            for (int iter = 0; iter < 4; iter++)
            {
                bool Cabe(float dist) => Proyectar(dist, tx, ty, out Rect r) && r.width <= 2f * mx && r.height <= 2f * my;
                float cerca = 0.01f, lejos = 50f;
                if (!Cabe(lejos)) return;
                for (int i = 0; i < 40; i++)
                {
                    float medio = 0.5f * (cerca + lejos);
                    if (Cabe(medio)) lejos = medio; else cerca = medio;
                }
                d = lejos;
                Proyectar(d, tx, ty, out Rect ocupado);
                tx += nx - ocupado.center.x;
                ty += ny - ocupado.center.y;
            }

            Transform t = camara.transform;
            t.rotation = rot;
            t.position = c - t.forward * d - t.right * (tx * d * tanH) - t.up * (ty * d * tanV);

            centroObjetivo = c;
            distancia = d;
            _anchoPantalla = Screen.width;
            _altoPantalla = Screen.height;
            _pendiente = false;
        }
    }
}
