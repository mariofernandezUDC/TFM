// SLD_Planta.cs · La planta virtual de la clasificadora SLD (F0.6, D007: en modo virtual manda la planta).
//
// Lee las salidas del TXT 5 de VirtualIO_TXT (lo recibido por vio/txt5/out, o el
// estado seguro) y mueve la planta con ellas. Después mide los sensores y, si
// VirtualIO_TXT está en modo Planta, escribe las entradas del TXT 5.
// Pasos 1 a 3: cinta y piezas, sensores, cilindros, compresor y rampas.
//
// Las piezas SOLO aparecen al principio de la cinta, con los botones del
// Inspector o con las teclas 1 (blanca), 2 (roja) y 3 (azul). La tecla 0 las
// quita todas. Nunca aparecen de la nada a mitad de proceso.
//
// La simulación avanza en pasos de 5 ms como máximo. Y cada frame simula 20 ms
// como mucho: si el editor se atasca (una consulta por la CLI, el recolector de
// basura...), la planta va más despacio durante el atasco en vez de dar un salto.
// Un salto de más de 6 mm haría que C1 cambiara dos veces en una sola imagen in,
// y SLD.py perdería cuentas. SLD.py cuenta estados, no tiempo: frenar la planta
// no le afecta. framesRecortados y peorFrame_ms dicen cuántas veces pasó.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantaVirtual
{
    [DefaultExecutionOrder(-50)] // antes que VirtualIO_TXT, que hace la foto de las entradas en su Update
    public class SLD_Planta : MonoBehaviour
    {
        private const float PasoMaximo_s = 0.005f;
        private const float AvanceMaximoPorFrame_s = 0.020f;

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

        [Header("Sensores (lo que la planta escribe en las entradas del TXT 5)")]
        public BarreraLuzVirtual I1 = new BarreraLuzVirtual("I1 entrada");
        public SensorColorVirtual I2 = new SensorColorVirtual("I2 color");
        public BarreraLuzVirtual I3 = new BarreraLuzVirtual("I3 cilindros");
        public ContadorPulsosVirtual C1 = new ContadorPulsosVirtual("C1 contador");
        [SoloLectura, Tooltip("Barreras de las rampas blanca, roja y azul")] public bool I6, I7, I8;

        [Header("Cilindros y rampas")]
        public EstacionExpulsion blanca = new EstacionExpulsion();
        [Tooltip("I7 puede quedar libre tras el paso: el centro de la placa roja está a 15 mm del haz y el radio actual de la pieza es 12,5 mm.")]
        public EstacionExpulsion roja = new EstacionExpulsion();
        public EstacionExpulsion azul = new EstacionExpulsion();
        [SoloLectura] public bool hayPresion;
        private float tiempoCompresor;
        private EstacionExpulsion[] estaciones;

        [Header("Diagnóstico")]
        [SoloLectura, Tooltip("Tiempo simulado de la planta, s")] public double tiempoPlanta_s;
        [SoloLectura, Tooltip("Frames que duraron más de 20 ms y se simularon como 20 ms")] public int framesRecortados;
        [SoloLectura, Tooltip("Frame más largo desde el inicio de Play, ms")] public float peorFrame_ms;
        public RegistroPlanta registro = new RegistroPlanta();

        private readonly List<Pieza> _piezas = new List<Pieza>();
        private Transform _contenedor;
        private int _contadorPiezas;
        private bool _cintaEnMarchaAntes;

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
            estaciones = new[] { blanca, roja, azul };
            foreach (var e in estaciones)
            {
                if (!e.vastago || !e.placa || !e.lampara || !e.fototransistor)
                {
                    Debug.LogError("[SLD_Planta] Faltan enlaces de cilindro, placa o barrera.");
                    enabled = false;
                    return;
                }
                e.Configurar(cinta, parametros);
            }
            ColocarSensores();
        }

        /// <summary>Haces de I1 e I3 de lado a lado de la cinta, a la altura de la escena.</summary>
        private void ColocarSensores()
        {
            float mitad = parametros.anchoCinta * 0.5f + parametros.margenHazCinta;
            float h = parametros.alturaHazCinta;
            I1.Colocar(cinta.PuntoMundo(parametros.posI1, -mitad, h), cinta.PuntoMundo(parametros.posI1, mitad, h));
            I3.Colocar(cinta.PuntoMundo(parametros.posI3, -mitad, h), cinta.PuntoMundo(parametros.posI3, mitad, h));
        }

        /// <summary>Mide todos los sensores con la escena ya colocada y escribe las entradas.</summary>
        private void MedirSensores()
        {
            Physics.SyncTransforms(); // los Transform se han movido a mano en este frame
            I1.Medir();
            I3.Medir();
            I2.Medir(_piezas, parametros.posI2, parametros);
            C1.Medir(cinta.recorrido_mm, parametros.mmPorCambioC1);
            I6 = blanca.barrera.Medir();
            I7 = roja.barrera.Medir();
            I8 = azul.barrera.Medir();
            io.EscribirEntradas(I1.oscura, I2.tension, I3.oscura, I6, I7, I8, C1.estado);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            I1.DibujarGizmo();
            I3.DibujarGizmo();
            if (estaciones != null)
                foreach (var e in estaciones) e.barrera.DibujarGizmo();
        }

        void Update()
        {
            LeerTeclas();

            SalidasTXT5 s = io.Salidas;
            float dtReal = Time.unscaledDeltaTime;
            peorFrame_ms = Mathf.Max(peorFrame_ms, dtReal * 1000f);
            float dt = Mathf.Min(Time.deltaTime, AvanceMaximoPorFrame_s);
            if (Time.deltaTime > AvanceMaximoPorFrame_s) framesRecortados++;
            tiempoPlanta_s += dt;
            int pasos = Mathf.Max(1, Mathf.CeilToInt(dt / PasoMaximo_s));
            float h = dt / pasos;
            for (int i = 0; i < pasos; i++)
            {
                cinta.Avanzar(h, s.M1_speed, s.M1_dir, s.M1_running, _piezas, parametros.RadioPieza);
                tiempoCompresor = s.O8 ? Mathf.Min(tiempoCompresor + h, parametros.tiempoPresion) : 0f;
                hayPresion = s.O8 && tiempoCompresor >= parametros.tiempoPresion;
                blanca.Avanzar(h, hayPresion && s.O5, ColorPieza.Blanca, parametros.posCilindroBlanco, this);
                roja.Avanzar(h, hayPresion && s.O6, ColorPieza.Roja, parametros.posCilindroRojo, this);
                azul.Avanzar(h, hayPresion && s.O7, ColorPieza.Azul, parametros.posCilindroAzul, this);
                foreach (var p in _piezas)
                    if (p != null && p.estado == EstadoPieza.EnRampa)
                        estaciones[(int)p.cilindroIntentado].Bajar(p, parametros.velocidadRampa * h);
            }

            _piezas.RemoveAll(p => p == null);
            foreach (var p in _piezas) ColocarEnMundo(p);
            piezasEnEscena = _piezas.Count;
            MedirSensores();
            Registrar(s);
        }

        // ── Registro ──────────────────────────────────────────────────────────

        private void Registrar(SalidasTXT5 s)
        {
            registro.Muestrear(tiempoPlanta_s, io.entradas, s, io.estadoSeguro);
            bool enMarcha = cinta.velocidad_mm_s != 0f;
            if (_cintaEnMarchaAntes && !enMarcha)
            {
                foreach (var p in _piezas)
                    if (p != null && p.estado == EstadoPieza.EnCinta && p.posicion_mm > parametros.posI3)
                        Evento($"cinta parada: {p.name} en {p.posicion_mm:F1} mm, descentrado {Descentrado(p):+0.0;-0.0} mm respecto a su cilindro");
            }
            _cintaEnMarchaAntes = enMarcha;
        }

        internal void Evento(string detalle) =>
            registro.Evento(tiempoPlanta_s, io.entradas, io.Salidas, io.estadoSeguro, detalle);

        /// <summary>Posición (mm desde el inicio de la cinta) del cilindro que corresponde a un color.</summary>
        public float PosicionCilindro(ColorPieza c) => c switch
        {
            ColorPieza.Blanca => parametros.posCilindroBlanco,
            ColorPieza.Roja => parametros.posCilindroRojo,
            _ => parametros.posCilindroAzul,
        };

        /// <summary>Descentrado de la pieza respecto al cilindro de su color, en mm (+ = se ha pasado).</summary>
        public float Descentrado(Pieza p) => p.posicion_mm - PosicionCilindro(p.ColorReal);

        /// <summary>Vuelca el registro a un CSV. Sin ruta: en la carpeta de datos de la aplicación.</summary>
        public string VolcarRegistro(string ruta = null)
        {
            if (string.IsNullOrEmpty(ruta))
                ruta = System.IO.Path.Combine(Application.persistentDataPath, "PlantaVirtual",
                                              $"registro_sld_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
            return registro.Volcar(ruta);
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
            Evento($"colocada {go.name} en {s0:F1} mm");
            return true;
        }

        public void QuitarTodas()
        {
            foreach (var p in _piezas) if (p != null) Destroy(p.gameObject);
            _piezas.Clear();
            Evento("quitar todas");
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
