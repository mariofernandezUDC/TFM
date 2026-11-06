// VirtualIO_TXT.cs · Puente de E/S de un TXT con la librería fischertechnik virtual (F0.5).
//
// Implementa el lado Unity del protocolo vio (02-DOCS/wiki/decisiones/D006 Protocolo vio.md):
//
// - D006.1  Publica en vio/txtN/in la imagen COMPLETA de entradas y recibe por
//           vio/txtN/out la imagen completa de salidas.
// - D006.2  vio/txtN/in sale a 100 Hz REALES, desde un hilo propio. No depende
//           de los FPS del editor ni de Update().
// - D006.3  Si pasan más de 0,5 s sin un out válido: estado seguro (motor parado
//  D006.5   y válvulas cerradas).
// - D006.4  Los valores viajan tal cual los da la API real, sin conversión.
// - D006.8  Si el hilo principal de Unity lleva más de 0,5 s sin avanzar (pausa
//           o cuelgue), deja de enviarse in. Un out inválido se descarta, con
//           aviso limitado, y no renueva la hora.
//
// Este script SOLO comunica. No mueve nada de la escena: mover la cinta y los
// cilindros a partir de las salidas es F0.6 (diseño de Mario).
//
// Usa la misma librería MQTT que el resto del proyecto (Assets/Plugins/M2Mqtt.Net.dll),
// pero con su propia conexión: MQTTClient.cs de Adrián no se toca (D005).

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Debug = UnityEngine.Debug;

namespace PlantaVirtual
{
    /// <summary>
    /// Imagen de entradas del TXT 5 (vio/txt5/in). Los nombres de los campos son
    /// las claves del JSON, por eso se llaman I1, I2... (D006.4).
    /// </summary>
    [Serializable]
    public class EntradasTXT5
    {
        [Tooltip("Barrera de entrada. true = oscuro (hay pieza)")] public bool I1 = false;
        [Tooltip("Sensor de color, en la escala de get_voltage(). 2500 = sin pieza")]
        [Range(0, 3000)] public int I2 = 2500;
        [Tooltip("Barrera de los cilindros. true = oscuro")] public bool I3 = false;
        [Tooltip("Barrera de la rampa blanca. true = oscuro")] public bool I6 = false;
        [Tooltip("Barrera de la rampa roja. true = oscuro")] public bool I7 = false;
        [Tooltip("Barrera de la rampa azul. true = oscuro")] public bool I8 = false;
        [Tooltip("Pulsador contador de la cinta (estado, no cuenta: D006.2)")] public bool C1 = false;
    }

    /// <summary>Motor M1 tal como viaja en vio/txt5/out (D006.4).</summary>
    [Serializable]
    public class MotorVio
    {
        public int speed;        // 0–512
        public string dir = "CW"; // "CW" o "CCW"
        public bool running;
    }

    /// <summary>
    /// Imagen de salidas del TXT 5. Inmutable una vez creada: el hilo de red la
    /// crea entera y la sustituye de golpe, así nadie ve media imagen.
    /// </summary>
    public sealed class SalidasTXT5
    {
        public readonly int M1_speed;
        public readonly string M1_dir;
        public readonly bool M1_running;
        public readonly bool O5, O6, O7, O8;

        public SalidasTXT5(int speed, string dir, bool running, bool o5, bool o6, bool o7, bool o8)
        {
            M1_speed = speed; M1_dir = dir; M1_running = running;
            O5 = o5; O6 = o6; O7 = o7; O8 = o8;
        }

        /// <summary>Estado seguro (D006.3): motor parado y todas las válvulas cerradas.</summary>
        public static readonly SalidasTXT5 Segura = new SalidasTXT5(0, "CW", false, false, false, false, false);
    }

    /// <summary>Hace que el Inspector enseñe un campo sin dejar editarlo (ver Editor/SoloLecturaDrawer.cs).</summary>
    public class SoloLecturaAttribute : PropertyAttribute { }

    public class VirtualIO_TXT : MonoBehaviour
    {
        // --- Reglas de D006. Constantes a propósito: D006.6 dice que no se desactivan ni en pruebas. ---
        private const double CADUCIDAD_OUT_S = 0.5;        // D006.3 / D006.5
        private const double HOMBRE_MUERTO_UNITY_S = 0.5;  // D006.8
        private const double INTERVALO_AVISO_S = 2.0;      // Avisos limitados a uno cada 2 s
        private const int HZ_IN = 100;                     // D006.2

        [Header("Conexión")]
        [Tooltip("Broker MQTT local (Mosquitto)")] public string broker = "localhost";
        public int puerto = 1883;
        [Tooltip("Número de TXT. De momento solo el 5 (D006)")] public int numeroTxt = 5;

        public enum OrigenEntradas { Manual, Planta }

        [Header("Entradas (salen por vio/txtN/in a 100 Hz)")]
        [Tooltip("Manual: las cambias tú en el Inspector. Planta: las escribe la planta virtual (SLD_Planta) en cada frame y aquí solo se ven (F0.6).")]
        public OrigenEntradas origenEntradas = OrigenEntradas.Manual;
        public EntradasTXT5 entradas = new EntradasTXT5();

        /// <summary>
        /// La planta virtual escribe aquí las entradas (F0.6). Solo tiene efecto en modo Planta:
        /// en modo Manual mandan las del Inspector. Llamar antes del Update de este componente.
        /// </summary>
        public void EscribirEntradas(bool i1, int i2, bool i3, bool i6, bool i7, bool i8, bool c1)
        {
            if (origenEntradas != OrigenEntradas.Planta) return;
            entradas.I1 = i1; entradas.I2 = i2; entradas.I3 = i3;
            entradas.I6 = i6; entradas.I7 = i7; entradas.I8 = i8; entradas.C1 = c1;
        }

        [Header("Salidas que se aplican (lo recibido, o el estado seguro)")]
        [SoloLectura] public int M1_speed;
        [SoloLectura] public string M1_dir = "CW";
        [SoloLectura] public bool M1_running;
        [SoloLectura] public bool O5;
        [SoloLectura] public bool O6;
        [SoloLectura] public bool O7;
        [SoloLectura] public bool O8;

        [Header("Diagnóstico")]
        [SoloLectura] public bool conectado;
        [SoloLectura] public bool estadoSeguro = true;
        [SoloLectura, Tooltip("Mensajes in publicados en el último segundo")] public int hzIn;
        [SoloLectura, Tooltip("Mensajes out válidos recibidos en el último segundo")] public int outPorSegundo;
        [SoloLectura, Tooltip("Edad del último out válido, en ms. -1 = todavía ninguno")] public int edadUltimoOutMs = -1;
        [SoloLectura, Tooltip("Edad del último out al entrar en estado seguro (criterio c de F0.5)")] public int edadAlEntrarEnSeguroMs = -1;
        [SoloLectura, Tooltip("false si el hilo principal de Unity lleva más de 0,5 s parado (D006.8)")] public bool enviandoIn;
        [SoloLectura] public int outDescartados;

        /// <summary>
        /// Salidas que hay que aplicar a la planta: lo último recibido, o el estado
        /// seguro. Es lo que leerán los modelos de la cinta y de los cilindros en F0.6.
        /// </summary>
        public SalidasTXT5 Salidas => _salidasAplicadas;

        // --- Estado compartido entre hilos. Cada referencia se sustituye entera (nunca se modifica). ---
        private volatile string _cargaIn;                 // JSON de entradas, escrito por Update()
        private volatile SalidasTXT5 _ultimaOut;          // último out válido, escrito por el hilo de red
        private volatile SalidasTXT5 _salidasAplicadas = SalidasTXT5.Segura;
        private long _ticksUltimoUpdate;                  // reloj del último Update() (D006.8)
        private long _ticksUltimoOut = -1;                // reloj del último out válido

        private readonly Stopwatch _reloj = Stopwatch.StartNew();
        private Thread _hilo;
        private volatile bool _parar;
        private MqttClient _cliente;
        private string _topicIn, _topicOut;

        // Contadores para el diagnóstico (los escribe un hilo, los lee Update)
        private volatile int _hzIn, _outPorSegundo, _edadUltimoOutMs = -1, _edadAlEntrarEnSeguroMs = -1, _outDescartados;
        private volatile bool _conectado, _estadoSeguro = true, _enviandoIn;
        private int _outEsteSegundo;                      // solo lo toca el hilo de red (Interlocked)
        private double _tAvisoDescarte = -10;

        // ------------------------------------------------------------------
        // Reloj de 1 ms en Windows (lo mismo que reloj_de_1_ms() de F0.4).
        // Sin esto, Thread.Sleep(1) duerme unos 15,6 ms y no se llega a 100 Hz.
        // ------------------------------------------------------------------
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint ms);
        [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint ms);
#else
        private static uint timeBeginPeriod(uint ms) { return 0; }
        private static uint timeEndPeriod(uint ms) { return 0; }
#endif

        // ------------------------------------------------------------------
        // Ciclo de vida (hilo principal de Unity)
        // ------------------------------------------------------------------

        void OnEnable()
        {
            _topicIn = $"vio/txt{numeroTxt}/in";
            _topicOut = $"vio/txt{numeroTxt}/out";
            AnotarUpdate();
            _parar = false;
            _hilo = new Thread(BucleVio) { Name = $"vio-txt{numeroTxt}", IsBackground = true };
            _hilo.Start();
        }

        void OnDisable()
        {
            // Al salir de Play se para el hilo. Python dejará de recibir in y,
            // por D006.7, dejará de enviar out.
            _parar = true;
            if (_hilo != null && !_hilo.Join(1000))
                Debug.LogWarning("[vio] El hilo no se ha parado en 1 s.");
            _hilo = null;
        }

        void Update()
        {
            // 1. El hilo principal está vivo (D006.8).
            AnotarUpdate();

            // 2. Foto de las entradas del Inspector. Es un string nuevo cada vez:
            //    el hilo de 100 Hz siempre publica una imagen entera (D006.1).
            _cargaIn = JsonUtility.ToJson(entradas);

            // 3. Copia al Inspector de lo que pasa en los otros hilos.
            SalidasTXT5 s = _salidasAplicadas;
            M1_speed = s.M1_speed; M1_dir = s.M1_dir; M1_running = s.M1_running;
            O5 = s.O5; O6 = s.O6; O7 = s.O7; O8 = s.O8;
            conectado = _conectado;
            estadoSeguro = _estadoSeguro;
            hzIn = _hzIn;
            outPorSegundo = _outPorSegundo;
            edadUltimoOutMs = _edadUltimoOutMs;
            edadAlEntrarEnSeguroMs = _edadAlEntrarEnSeguroMs;
            enviandoIn = _enviandoIn;
            outDescartados = _outDescartados;
        }

        private void AnotarUpdate() => Interlocked.Exchange(ref _ticksUltimoUpdate, _reloj.ElapsedTicks);

        private double Segundos(long ticks) => (double)ticks / Stopwatch.Frequency;

        // ------------------------------------------------------------------
        // Hilo propio a 100 Hz: publica in, vigila out y gestiona la conexión.
        // ------------------------------------------------------------------

        private void BucleVio()
        {
            timeBeginPeriod(1);
            try
            {
                long periodo = Stopwatch.Frequency / HZ_IN;
                long siguiente = _reloj.ElapsedTicks;
                long inicioSegundo = siguiente;
                int inEsteSegundo = 0;
                double tProximoIntento = 0;
                bool avisoHombreMuertoDado = false;

                while (!_parar)
                {
                    long ahora = _reloj.ElapsedTicks;

                    // --- Conexión: si no hay, se reintenta cada 2 s sin frenar el resto ---
                    if (_cliente == null || !_cliente.IsConnected)
                    {
                        _conectado = false;
                        if (Segundos(ahora) >= tProximoIntento)
                        {
                            tProximoIntento = Segundos(ahora) + 2.0;
                            Conectar();
                        }
                    }

                    // --- D006.8: ¿el hilo principal de Unity sigue avanzando? ---
                    double edadUpdate = Segundos(ahora - Interlocked.Read(ref _ticksUltimoUpdate));
                    bool unityVivo = edadUpdate <= HOMBRE_MUERTO_UNITY_S;
                    if (!unityVivo && !avisoHombreMuertoDado)
                    {
                        Debug.LogWarning($"[vio] Unity lleva {edadUpdate:F2} s sin avanzar (pausa o cuelgue). " +
                                         $"Dejo de enviar {_topicIn} (D006.8).");
                        avisoHombreMuertoDado = true;
                    }
                    else if (unityVivo && avisoHombreMuertoDado)
                    {
                        Debug.Log($"[vio] Unity vuelve a avanzar. Reanudo {_topicIn}.");
                        avisoHombreMuertoDado = false;
                    }

                    // --- Publicar la imagen de entradas (D006.1, D006.2) ---
                    string carga = _cargaIn;
                    bool enviado = false;
                    if (unityVivo && carga != null && _conectado)
                    {
                        try
                        {
                            _cliente.Publish(_topicIn, Encoding.UTF8.GetBytes(carga),
                                             MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);  // QoS 0, sin retain
                            enviado = true;
                            inEsteSegundo++;
                        }
                        catch (Exception ex)
                        {
                            _conectado = false;
                            Debug.LogWarning($"[vio] Error al publicar {_topicIn}: {ex.Message}");
                        }
                    }
                    _enviandoIn = enviado;

                    // --- Estado seguro si no llega out (D006.3, D006.5) ---
                    VigilarOut(ahora);

                    // --- Contadores por segundo ---
                    if (ahora - inicioSegundo >= Stopwatch.Frequency)
                    {
                        _hzIn = inEsteSegundo;
                        _outPorSegundo = Interlocked.Exchange(ref _outEsteSegundo, 0);
                        inEsteSegundo = 0;
                        inicioSegundo = ahora;
                    }

                    // --- Esperar al siguiente plazo de 10 ms ---
                    // Plazos fijos (siguiente += periodo) para no acumular retraso.
                    siguiente += periodo;
                    long resta;
                    while (!_parar && (resta = siguiente - _reloj.ElapsedTicks) > 0)
                        Thread.Sleep(resta > Stopwatch.Frequency / 500 ? 1 : 0);
                    // Si el hilo se ha atrasado mucho (p. ej. el PC se ha parado), no
                    // recupera enviando ráfagas: vuelve a empezar desde ahora.
                    if (_reloj.ElapsedTicks - siguiente > 5 * periodo)
                        siguiente = _reloj.ElapsedTicks;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[vio] El hilo de 100 Hz se ha parado por un error: {ex}");
            }
            finally
            {
                Desconectar();
                _salidasAplicadas = SalidasTXT5.Segura;
                _estadoSeguro = true;
                _conectado = false;
                _enviandoIn = false;
                timeEndPeriod(1);
            }
        }

        private void VigilarOut(long ahora)
        {
            long tOut = Interlocked.Read(ref _ticksUltimoOut);
            double edad = tOut < 0 ? double.PositiveInfinity : Segundos(ahora - tOut);
            _edadUltimoOutMs = tOut < 0 ? -1 : (int)(edad * 1000);

            bool caducado = edad > CADUCIDAD_OUT_S;
            if (caducado)
            {
                if (!_estadoSeguro)
                {
                    _edadAlEntrarEnSeguroMs = (int)(edad * 1000);
                    Debug.LogWarning($"[vio] {edad * 1000:F0} ms sin {_topicOut}. Estado seguro: " +
                                     "motor parado y válvulas cerradas (D006.3).");
                }
                _estadoSeguro = true;
                _salidasAplicadas = SalidasTXT5.Segura;
            }
            else
            {
                if (_estadoSeguro)
                    Debug.Log($"[vio] Vuelve {_topicOut}. Salgo del estado seguro.");
                _estadoSeguro = false;
                _salidasAplicadas = _ultimaOut;
            }
        }

        // ------------------------------------------------------------------
        // MQTT (M2Mqtt, la misma DLL que usa MQTTClient.cs de Adrián)
        // ------------------------------------------------------------------

        private void Conectar()
        {
            try
            {
                Desconectar();
                var cliente = new MqttClient(broker, puerto, false, null, null, MqttSslProtocols.None);
                cliente.MqttMsgPublishReceived += AlRecibir;
                string id = $"vio-unity-txt{numeroTxt}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                cliente.Connect(id);
                if (!cliente.IsConnected) return;
                cliente.Subscribe(new[] { _topicOut }, new[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
                _cliente = cliente;
                _conectado = true;
                Debug.Log($"[vio] Conectado a {broker}:{puerto}. Publico {_topicIn} a {HZ_IN} Hz y escucho {_topicOut}.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[vio] No puedo conectar con {broker}:{puerto} ({ex.Message}). Reintento en 2 s.");
            }
        }

        private void Desconectar()
        {
            var c = _cliente;
            _cliente = null;
            if (c == null) return;
            c.MqttMsgPublishReceived -= AlRecibir;
            try { if (c.IsConnected) c.Disconnect(); } catch { }
        }

        // Lo llama M2Mqtt desde SU hilo de red. No toca nada de Unity salvo Debug.Log,
        // que se puede llamar desde cualquier hilo.
        private void AlRecibir(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic != _topicOut) return;
            string texto;
            try { texto = Encoding.UTF8.GetString(e.Message); }
            catch { Descartar("no es UTF-8"); return; }

            SalidasTXT5 s = LeerSalidas(texto, out string motivo);
            if (s == null) { Descartar(motivo); return; }   // D006.8: no renueva la hora

            _ultimaOut = s;
            Interlocked.Exchange(ref _ticksUltimoOut, _reloj.ElapsedTicks);
            Interlocked.Increment(ref _outEsteSegundo);
        }

        private void Descartar(string motivo)
        {
            _outDescartados++;
            double t = Segundos(_reloj.ElapsedTicks);
            if (t - _tAvisoDescarte >= INTERVALO_AVISO_S)
            {
                _tAvisoDescarte = t;
                Debug.LogWarning($"[vio] {_topicOut} descartado ({motivo}). Total descartados: {_outDescartados}. (D006.8)");
            }
        }

        // --- Validación estricta de out (D006.4 + D006.8) ---
        // JsonUtility rellena con valores por defecto las claves que faltan, así que
        // no sirve para validar. Se comprueba cada clave y su tipo con expresiones
        // regulares sobre el texto. Si algo no cuadra, se descarta la imagen entera.
        private static readonly Regex ReM1 = new Regex("\"M1\"\\s*:\\s*\\{([^{}]*)\\}", RegexOptions.Compiled);
        private static readonly Regex ReSpeed = new Regex("\"speed\"\\s*:\\s*(-?\\d+)\\s*(,|$)", RegexOptions.Compiled);
        private static readonly Regex ReDir = new Regex("\"dir\"\\s*:\\s*\"(CW|CCW)\"", RegexOptions.Compiled);
        private static readonly Regex ReRunning = new Regex("\"running\"\\s*:\\s*(true|false)\\b", RegexOptions.Compiled);
        private static readonly string[] ClavesValvula = { "O5", "O6", "O7", "O8" };
        private static readonly Regex[] ReValvulas = Array.ConvertAll(ClavesValvula,
            k => new Regex("\"" + k + "\"\\s*:\\s*(true|false)\\b", RegexOptions.Compiled));

        /// <summary>Devuelve la imagen de salidas, o null (con el motivo) si el mensaje no es válido.</summary>
        public static SalidasTXT5 LeerSalidas(string texto, out string motivo)
        {
            motivo = null;
            string t = texto?.Trim();
            if (string.IsNullOrEmpty(t) || t[0] != '{' || t[t.Length - 1] != '}') { motivo = "no es un objeto JSON"; return null; }

            Match m1 = ReM1.Match(t);
            if (!m1.Success) { motivo = "falta M1 o no es un objeto"; return null; }
            string cuerpo = m1.Groups[1].Value.Trim();
            Match sp = ReSpeed.Match(cuerpo), di = ReDir.Match(cuerpo), ru = ReRunning.Match(cuerpo);
            if (!sp.Success) { motivo = "M1.speed falta o no es entero"; return null; }
            if (!di.Success) { motivo = "M1.dir falta o no es \"CW\"/\"CCW\""; return null; }
            if (!ru.Success) { motivo = "M1.running falta o no es bool"; return null; }
            if (!int.TryParse(sp.Groups[1].Value, out int speed) || speed < 0 || speed > 512)
            { motivo = "M1.speed fuera de 0–512"; return null; }

            var v = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                Match mv = ReValvulas[i].Match(t);
                if (!mv.Success) { motivo = ClavesValvula[i] + " falta o no es bool"; return null; }
                v[i] = mv.Groups[1].Value == "true";
            }
            return new SalidasTXT5(speed, di.Groups[1].Value, ru.Groups[1].Value == "true", v[0], v[1], v[2], v[3]);
        }
    }
}
