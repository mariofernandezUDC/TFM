// LanzadorPrograma.cs · Lanza el programa del alumno desde Unity (F5, paso P2 de D008).
//
// Unity ejecuta virtual/run_virtual.py --programa <ruta> como un PROCESO APARTE
// (02-DOCS/wiki/decisiones/D008 Lanzador desde Unity.md, D008-A). Entre los dos
// procesos solo hay:
//   - vio por MQTT (D006), que ya lleva VirtualIO_TXT;
//   - la salida estándar del proceso, que aquí se lee y se pasa a la consola de Unity.
//
// P2: un programa fijo y una tecla (L) para lanzarlo. Parar, el panel y los
// errores en rojo llegan en P3–P5.
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

        [Header("Estado")]
        [SoloLectura] public bool enMarcha;
        [SoloLectura] public int pid = -1;

        private Process _proceso;

        // Las líneas llegan en hilos de .NET, no en el de Unity. Se guardan aquí
        // y se pasan a Debug.Log desde Update(). esError = vino por stderr.
        private readonly ConcurrentQueue<(bool esError, string texto)> _salida =
            new ConcurrentQueue<(bool, string)>();

        /// <summary>Raíz del repositorio: Unity/Assets → ../.. (en la build cambia, F6).</summary>
        private static string RaizRepo => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        private void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.lKey.wasPressedThisFrame) Lanzar();

            VaciarSalida();
            enMarcha = SigueVivo();
        }

        private void OnDisable() => Matar();
        private void OnApplicationQuit() => Matar();

        /// <summary>Lanza el programa fijo. Si ya hay uno en marcha, no hace nada (Parar llega en P3).</summary>
        public void Lanzar()
        {
            if (SigueVivo())
            {
                Debug.LogWarning("[Lanzador] Ya hay un programa en marcha (PID " + pid + "). Sal de Play para pararlo.");
                return;
            }

            if (!ResolverPython()) return;

            string raiz = RaizRepo;
            string rutaPrograma = Path.GetFullPath(Path.Combine(raiz, "virtual", "alumno", programa));
            if (!File.Exists(rutaPrograma))
            {
                Debug.LogError("[Lanzador] No encuentro el programa: " + rutaPrograma);
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

            var p = new Process { StartInfo = psi };
            // Estos dos eventos llegan en OTRO hilo: aquí solo se encola, sin tocar nada de Unity.
            p.OutputDataReceived += (_, e) => { if (e.Data != null) _salida.Enqueue((false, e.Data)); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) _salida.Enqueue((true, e.Data)); };

            try
            {
                p.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Lanzador] No he podido arrancar Python (" + rutaPython + "): " + ex.Message);
                p.Dispose();
                return;
            }
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            _proceso = p;
            pid = p.Id;
            enMarcha = true;
            Debug.Log($"[Lanzador] En marcha: {programa} (PID {pid}, broker {broker}:{puerto})");
        }

        /// <summary>Mata el proceso si sigue vivo, para que no quede ningún Python huérfano.</summary>
        private void Matar()
        {
            if (_proceso == null) return;
            try
            {
                if (!_proceso.HasExited)
                {
                    _proceso.Kill();
                    _proceso.WaitForExit(1000);
                    Debug.Log("[Lanzador] Programa parado (PID " + pid + ").");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Lanzador] Al parar el programa: " + ex.Message);
            }
            VaciarSalida();
            _proceso.Dispose();
            _proceso = null;
            pid = -1;
            enMarcha = false;
        }

        private bool SigueVivo()
        {
            try { return _proceso != null && !_proceso.HasExited; }
            catch (InvalidOperationException) { return false; }
        }

        /// <summary>Pasa a la consola de Unity lo que ha escrito el programa. Solo desde el hilo de Unity.</summary>
        private void VaciarSalida()
        {
            while (_salida.TryDequeue(out var linea))
            {
                if (linea.esError) Debug.LogError("[programa] " + linea.texto);
                else Debug.Log("[programa] " + linea.texto);
            }
        }

        /// <summary>
        /// Si rutaPython está vacía, pregunta una vez a py.exe dónde está el python.exe real
        /// (py -3.12 -c "import sys; print(sys.executable)") y lo guarda en el campo.
        /// </summary>
        private bool ResolverPython()
        {
            if (!string.IsNullOrWhiteSpace(rutaPython))
            {
                if (File.Exists(rutaPython)) return true;
                Debug.LogError("[Lanzador] No existe rutaPython: " + rutaPython + ". Déjala vacía para buscarla con py " + versionPy + ".");
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
                    Debug.LogError($"[Lanzador] «py {versionPy}» no ha dado una ruta válida (código {p.ExitCode}): «{ruta}»");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Lanzador] No he podido ejecutar «py " + versionPy + "» para encontrar Python: " + ex.Message);
            }
            return false;
        }
    }
}
