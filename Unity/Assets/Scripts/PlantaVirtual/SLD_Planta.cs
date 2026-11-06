// SLD_Planta.cs · La planta virtual de la clasificadora SLD (F0.6, D007: en modo virtual manda la planta).
//
// Lee las salidas del TXT 5 de VirtualIO_TXT (lo recibido por vio/txt5/out, o el
// estado seguro) y mueve la planta con ellas. Paso 1: la cinta y las piezas.
// Los sensores, C1, los cilindros y las rampas llegan en los pasos siguientes.
//
// Las piezas SOLO aparecen al principio de la cinta, con los botones del
// Inspector o con las teclas 1 (blanca), 2 (roja) y 3 (azul). La tecla 0 las
// quita todas. Nunca aparecen de la nada a mitad de proceso.
//
// La simulación avanza en pasos de 5 ms como máximo, aunque un frame dure más,
// para que una pieza no se salte un sensor cuando el editor va lento.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantaVirtual
{
    [DefaultExecutionOrder(-50)] // antes que VirtualIO_TXT, que hace la foto de las entradas en su Update
    public class SLD_Planta : MonoBehaviour
    {
        private const float PasoMaximo_s = 0.005f;

        [Header("Referencias")]
        public ParametrosSLD parametros;
        public VirtualIO_TXT io;
        public CintaVirtual cinta;

        [Header("Aspecto de las piezas (de Assets/Prefabs/PiezaBase.prefab)")]
        public Mesh mallaPieza;
        public Material materialPieza;
        [Tooltip("Escala de la malla de PiezaBase (sus mallas vienen 100 veces más pequeñas)")]
        public float escalaMallaPieza = 100f;

        [Header("Estado")]
        [SoloLectura] public int piezasEnEscena;

        private readonly List<Pieza> _piezas = new List<Pieza>();
        private Transform _contenedor;
        private int _contadorPiezas;

        public IReadOnlyList<Pieza> Piezas => _piezas;

        void Start()
        {
            if (parametros == null || io == null || cinta == null)
            {
                Debug.LogError("[SLD_Planta] Faltan referencias (parámetros, io o cinta).");
                enabled = false;
                return;
            }
            double fase = parametros.fijarFaseC1 ? parametros.faseInicialC1.valor : 0.0;
            cinta.Configurar(parametros.longitudCinta, parametros.anchoCinta, parametros.vMax, fase);
            _contenedor = new GameObject("PiezasPlanta").transform;
            _contenedor.SetParent(transform, false);
        }

        void Update()
        {
            LeerTeclas();

            SalidasTXT5 s = io.Salidas;
            float dt = Time.deltaTime;
            int pasos = Mathf.Max(1, Mathf.CeilToInt(dt / PasoMaximo_s));
            float h = dt / pasos;
            for (int i = 0; i < pasos; i++)
                cinta.Avanzar(h, s.M1_speed, s.M1_dir, s.M1_running, _piezas, parametros.RadioPieza);

            _piezas.RemoveAll(p => p == null);
            foreach (var p in _piezas) ColocarEnMundo(p);
            piezasEnEscena = _piezas.Count;
        }

        // ── Piezas ────────────────────────────────────────────────────────────

        /// <summary>Pone una pieza al principio de la cinta. false si el sitio está ocupado.</summary>
        public bool ColocarPieza(ColorPieza color)
        {
            if (!Application.isPlaying || _contenedor == null) return false;
            float s0 = parametros.puntoColocacion;
            foreach (var otra in _piezas)
            {
                if (otra != null && otra.estado == EstadoPieza.EnCinta &&
                    Mathf.Abs(otra.posicion_mm - s0) < parametros.diametroPieza)
                {
                    Debug.LogWarning("[SLD_Planta] Ya hay una pieza al principio de la cinta.");
                    return false;
                }
            }

            var go = new GameObject($"Pieza{color}_{++_contadorPiezas}");
            go.transform.SetParent(_contenedor, true);
            go.transform.localScale = Vector3.one * escalaMallaPieza;
            go.AddComponent<MeshFilter>().sharedMesh = mallaPieza;
            go.AddComponent<MeshRenderer>();
            go.AddComponent<BoxCollider>(); // se ajusta a la malla; lo usarán los raycasts de las barreras

            var pieza = go.AddComponent<Pieza>();
            pieza.Inicializar(color, materialPieza);
            pieza.posicion_mm = s0;
            pieza.lateral_mm = 0f;
            pieza.altura_mm = parametros.alturaPieza * 0.5f;
            _piezas.Add(pieza);
            ColocarEnMundo(pieza);
            return true;
        }

        public void QuitarTodas()
        {
            foreach (var p in _piezas) if (p != null) Destroy(p.gameObject);
            _piezas.Clear();
        }

        private void ColocarEnMundo(Pieza p)
        {
            Vector3 centro = cinta.PuntoMundo(p.posicion_mm, p.lateral_mm, p.altura_mm);
            Transform t = p.transform;
            t.rotation = Quaternion.identity;
            // La malla no tiene el origen en su centro: se corrige con el centro de sus bounds.
            Vector3 desfase = mallaPieza != null ? t.TransformVector(mallaPieza.bounds.center) : Vector3.zero;
            t.position = centro - desfase;
        }

        private void LeerTeclas()
        {
            var k = Keyboard.current;
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame) ColocarPieza(ColorPieza.Blanca);
            if (k.digit2Key.wasPressedThisFrame) ColocarPieza(ColorPieza.Roja);
            if (k.digit3Key.wasPressedThisFrame) ColocarPieza(ColorPieza.Azul);
            if (k.digit0Key.wasPressedThisFrame) QuitarTodas();
        }
    }
}
