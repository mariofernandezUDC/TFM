// LanzadorPrograma.cs · Lanza y para el programa del alumno desde Unity (F5, D008).
//
// Unity ejecuta virtual/run_virtual.py --programa <ruta> como un PROCESO APARTE
// (02-DOCS/wiki/decisiones/D008 Lanzador desde Unity.md, D008-A). Entre los dos
// procesos solo hay:
//   - vio por MQTT (D006), que ya lleva VirtualIO_TXT;
//   - la salida estándar del proceso, que aquí se lee y se pasa a la consola de Unity.
//
// P2: un programa fijo y una tecla (L) para lanzarlo.
// P3: Parar (tecla P) = matar el proceso (D008-D). Sin latido, VirtualIO_TXT pasa a
//     estado seguro en 0,5 s (D006.5). Cargar con un programa en marcha para antes
//     el anterior. Al terminar solo, se apunta el código de salida.
// P4: el dashboard (DashboardPlanta) usa Lanzar(), Parar() y LineaSalida.
// P5: NoArranca avisa al dashboard cuando el programa no llega a arrancar.
//     Las teclas L y P quedan solo en el editor.
//
// Se lanza python.exe directamente, NO py.exe: py.exe abre python.exe como hijo,
// y al matar py el hijo puede quedarse vivo con la cinta en marcha.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace PlantaVirtual
{
    public class LanzadorPrograma : MonoBehaviour
    {
        /// <summary>Código que se apunta en ultimoCodigoSalida cuando el programa se para desde Unity.</summary>
        public const int CodigoParadoDesdeUnity = -1;
        /// <summary>Código que se apunta mientras no ha terminado ningún programa.</summary>
        public const int SinCodigo = int.MinValue;
        /// <summary>Código que se apunta cuando el programa no llega a arrancar (Python o programa no encontrado, P5).</summary>
        public const int CodigoNoArranca = -2;

        [Header("Python")]
        [Tooltip("Ruta del python.exe real (no py.exe). Si está vacía, se pregunta a «py -3.12» al primer Cargar de cada Play (cada PC tiene la suya)")]
        public string rutaPython = "";
        [Tooltip("Versión que se pide a py.exe para encontrar python.exe")]
        public string versionPy = "-3.12";

        [Header("Programa (P2: fijo)")]
        [Tooltip("Ruta relativa a virtual/alumno/")]
        public string programa = "ejercicios/soluciones/nivel4.py";

        [Header("Conexión")]
        [Tooltip("De aquí salen el broker y el puerto que se pasan a run_virtual.py. Vacío = se busca en la escena")]
        public VirtualIO_TXT io;

        [Header("Parada")]
        [Tooltip("Tiempo máximo que Parar() espera a que el proceso termine después de Kill(), ms")]
        public int esperaParada_ms = 2000;

        [Header("Estado")]
        [SoloLectura] public bool enMarcha;
        [SoloLectura] public int pid = -1;
        [SoloLectura, Tooltip("0 = bien, 1 = error del alumno, 2 = no encuentra el programa, -1 = parado desde Unity, -2 = no ha llegado a arrancar")]
        public int ultimoCodigoSalida = SinCodigo;

        /// <summary>Un programa lanzado. Cada lanzamiento tiene el suyo, así el aviso de
        /// fin de un programa viejo no se confunde con el del nuevo.</summary>
        private sealed class Ejecucion
        {
            public Process proceso;
            public int pid;
            public string programa;
            public volatile bool paradaPedida; // la pone Parar() antes de Kill()
        }

        private Ejecucion _actual;

        /// <summary>Cada línea que escribe el programa (texto, esError = vino por stderr).
        /// Se lanza desde Update(), en el hilo de Unity: se puede tocar la UI.</summary>
        public event Action<string, bool> LineaSalida;

        /// <summary>El programa no ha llegado a arrancar (P5). El texto está pensado para el
        /// alumno: el dashboard lo enseña en rojo en «Salida». Se lanza en el hilo de Unity.</summary>
        public event Action<string> NoArranca;

        /// <summary>Apunta el fallo en la consola y avisa al dashboard. ultimoCodigoSalida = -2.</summary>
        private void FalloAlArrancar(string paraLaConsola, string paraElAlumno)
        {
            Debug.LogError("[Lanzador] " + paraLaConsola);
            ultimoCodigoSalida = CodigoNoArranca;
            NoArranca?.Invoke(paraElAlumno);
        }

        // Las líneas y los avisos de fin llegan en hilos de .NET, no en el de Unity.
        // Se guardan aquí y se tratan desde Update(). esError = vino por stderr.
        private readonly ConcurrentQueue<(bool esError, string texto)> _salida =
            new ConcurrentQueue<(bool, string)>();
        private readonly ConcurrentQueue<(Ejecucion ejecucion, int codigo)> _terminados =
            new ConcurrentQueue<(Ejecucion, int)>();

        /// <summary>Raíz del repositorio: Unity/Assets → ../.. (en la build cambia, F6).</summary>
        private static string RaizRepo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        private void Update()
        {
#if UNITY_EDITOR
            // Teclas provisionales de P2/P3, solo en el editor: el alumno usa el dashboard.
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.lKey.wasPressedThisFrame) Lanzar();
                if (k.pKey.wasPressedThisFrame) Parar();
            }
#endif

            VaciarSalida();
            VaciarTerminados();
            enMarcha = SigueVivo();
        }

        // Al salir de Play (o al desactivar el objeto) no puede quedar ningún Python huérfano.
        private void OnDisable()
        {
            Parar();
            VaciarSalida();
        }

        private void OnApplicationQuit() => Parar();

        /// <summary>Lanza el programa. Si ya hay uno en marcha, primero lo para.</summary>
        public void Lanzar()
        {
            if (SigueVivo())
            {
                Debug.Log("[Lanzador] Hay un programa en marcha: lo paro antes de cargar el nuevo.");
                Parar();
            }

            if (!ResolverPython(out string porQue))
            {
                FalloAlArrancar(porQue, "No encuentro Python " + versionPy.TrimStart('-') + ". Instálalo, o escribe la ruta de python.exe en LanzadorPrograma → rutaPython. (" + porQue + ")");
                return;
            }

            string raiz = RaizRepo;
            string rutaPrograma = Path.GetFullPath(Path.Combine(raiz, "virtual", "alumno", programa));
            if (!File.Exists(rutaPrograma))
            {
                FalloAlArrancar("No encuentro el programa: " + rutaPrograma,
                    "No encuentro el programa " + programa + ". ¿Lo has borrado o cambiado de nombre? Pulsa ↻ para volver a leer la carpeta.");
                return;
            }

            if (io == null) io = FindFirstObjectByType<VirtualIO_TXT>();
            string broker = io != null ? io.broker : "localhost";
            int puerto = io != null ? io.puerto : 1883;

            var psi = new ProcessStartInfo
            {
                FileName = rutaPython,
                Arguments = $"virtual/run_virtual.py --programa \"{rutaPrograma}\" --broker {broker} --puerto {puerto}",
                WorkingDirectory = raiz,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                // Los mensajes llevan tildes: Python escribe en UTF-8 y aquí se lee en UTF-8.
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
            };
            // Sin búfer: cada print llega en el momento, no al final.
            psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var ej = new Ejecucion { proceso = p, programa = programa };
            // Estos tres eventos llegan en OTRO hilo: aquí solo se encola, sin tocar nada de Unity.
            p.OutputDataReceived += (_, e) => { if (e.Data != null) _salida.Enqueue((false, e.Data)); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) _salida.Enqueue((true, e.Data)); };
            p.Exited += (_, __) => AlTerminar(ej);

            try
            {
                p.Start();
            }
            catch (Exception ex)
            {
                FalloAlArrancar("No he podido arrancar Python (" + rutaPython + "): " + ex.Message,
                    "No he podido arrancar Python (" + rutaPython + "): " + ex.Message);
                return;
            }
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            ej.pid = p.Id;
            _actual = ej;
            pid = ej.pid;
            enMarcha = true;
            ultimoCodigoSalida = SinCodigo;
            Debug.Log($"[Lanzador] En marcha: {programa} (PID {pid}, broker {broker}:{puerto})");
        }

        /// <summary>
        /// Para el programa matando el proceso (D008-D). Espera como mucho esperaParada_ms
        /// a que termine. Sin latido, Unity pasa a estado seguro en 0,5 s (D006.5).
        /// </summary>
        public void Parar()
        {
            var ej = _actual;
            if (ej == null) return;
            _actual = null;
            pid = -1;
            enMarcha = false;

            try
            {
                if (ej.proceso.HasExited) return; // ya había terminado solo: su código llega por AlTerminar
                ej.paradaPedida = true;
                ej.proceso.Kill();
                if (ej.proceso.WaitForExit(esperaParada_ms))
                    Debug.Log($"[Lanzador] Programa parado (PID {ej.pid}).");
                else
                    Debug.LogWarning($"[Lanzador] He matado el PID {ej.pid}, pero no ha terminado en {esperaParada_ms} ms.");
                ultimoCodigoSalida = CodigoParadoDesdeUnity;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Lanzador] Al parar el programa: " + ex.Message);
            }
        }

        /// <summary>
        /// Evento Exited: corre en OTRO hilo. Espera a que llegue toda la salida
        /// (así el aviso de fin sale después de la última línea) y lo encola.
        /// </summary>
        private void AlTerminar(Ejecucion ej)
        {
            int codigo;
            try
            {
                ej.proceso.WaitForExit(); // sin tiempo: espera también a que se vacíe la salida
                codigo = ej.proceso.ExitCode;
            }
            catch (Exception)
            {
                codigo = SinCodigo;
            }
            // El Process no se cierra con Dispose(): Parar() puede estar usándolo a la vez
            // desde el hilo de Unity. Se libera solo cuando nadie lo referencia.
            _terminados.Enqueue((ej, codigo));
        }

        /// <summary>Apunta en la consola cómo ha terminado cada programa. Solo desde el hilo de Unity.</summary>
        private void VaciarTerminados()
        {
            while (_terminados.TryDequeue(out var fin))
            {
                if (fin.ejecucion.paradaPedida) continue; // ya lo ha dicho Parar()
                ultimoCodigoSalida = fin.codigo;
                string queEs = fin.codigo switch
                {
                    0 => "ha terminado bien",
                    1 => "ha terminado con un error (mira el bloque entre líneas de =)",
                    2 => "no se ha podido cargar (programa no encontrado)",
                    _ => "ha terminado",
                };
                string texto = $"[Lanzador] {fin.ejecucion.programa} {queEs}. Código de salida {fin.codigo} (PID {fin.ejecucion.pid}).";
                if (fin.codigo == 0) Debug.Log(texto);
                else Debug.LogWarning(texto); // es un error del programa del alumno, no de Unity
            }
        }

        private bool SigueVivo()
        {
            try { return _actual != null && !_actual.proceso.HasExited; }
            catch (InvalidOperationException) { return false; }
        }

        /// <summary>Pasa a la consola de Unity lo que ha escrito el programa. Solo desde el hilo de Unity.</summary>
        private void VaciarSalida()
        {
            while (_salida.TryDequeue(out var linea))
            {
                if (linea.esError) Debug.LogError("[programa] " + linea.texto);
                else Debug.Log("[programa] " + linea.texto);
                LineaSalida?.Invoke(linea.texto, linea.esError);
            }
        }

        /// <summary>
        /// Si rutaPython está vacía, pregunta una vez a py.exe dónde está el python.exe real
        /// (py -3.12 -c "import sys; print(sys.executable)") y lo guarda en el campo.
        /// </summary>
        private bool ResolverPython(out string porQue)
        {
            porQue = "";
            if (!string.IsNullOrWhiteSpace(rutaPython))
            {
                if (File.Exists(rutaPython)) return true;
                porQue = "No existe rutaPython: " + rutaPython + ". Déjala vacía para buscarla con py " + versionPy + ".";
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = versionPy + " -c \"import sys; print(sys.executable)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };
                using (var p = Process.Start(psi))
                {
                    string ruta = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(5000);
                    if (p.ExitCode == 0 && File.Exists(ruta))
                    {
                        rutaPython = ruta;
                        Debug.Log("[Lanzador] Python encontrado: " + rutaPython);
                        return true;
                    }
                    porQue = $"«py {versionPy}» no ha dado una ruta válida (código {p.ExitCode}): «{ruta}»";
                }
            }
            catch (Exception ex)
            {
                porQue = "No he podido ejecutar «py " + versionPy + "» para encontrar Python: " + ex.Message;
            }
            return false;
        }
    }
}
