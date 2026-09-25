using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using CommonUsages = UnityEngine.XR.CommonUsages;

// All diagnostics remain in the editor; no production or machine-control code is changed.
public static class VRValidationTools
{
    private const string ReportPath = "Logs/VR-validation.txt";
    private static double sampleEnd, nextSample;
    private static StringBuilder samples;

    [MenuItem("Tools/VR/Checkpoint scene and restart editor")]
    public static void CheckpointAndRestart()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GemeloDigital_LearningFactory.unity") throw new InvalidOperationException("Unexpected scene.");
        Directory.CreateDirectory("Assets/_Recovery");
        string copy = "Assets/_Recovery/VR_before_restart_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, copy, true)) throw new IOException("Scene checkpoint failed.");
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
        AssetDatabase.SaveAssets();
        Debug.Log("[TFM VR] Scene checkpoint: " + copy);
        EditorApplication.delayCall += () => EditorApplication.OpenProject(Path.GetFullPath("."));
    }

    [MenuItem("Tools/VR/Reimport HTC package scripts")]
    public static void ReimportHtc()
    {
        AssetDatabase.ImportAsset("Packages/com.htc.upm.vive.openxr/Runtime", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        AssetDatabase.ImportAsset("Packages/com.htc.upm.vive.openxr/Editor", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
    }

    [MenuItem("Tools/VR/Record 20 seconds of tracking")]
    public static void RecordTracking()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Start Play Mode first.");
        samples = new StringBuilder("time,node,valid,tracked,px,py,pz,qx,qy,qz,qw,trigger,axisX,axisY\n");
        sampleEnd = EditorApplication.timeSinceStartup + 20;
        nextSample = 0;
        EditorApplication.update -= Sample;
        EditorApplication.update += Sample;
    }

    private static void Sample()
    {
        double now = EditorApplication.timeSinceStartup;
        if (!Application.isPlaying || now >= sampleEnd)
        {
            EditorApplication.update -= Sample;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/VR-tracking.csv", samples.ToString());
            Inspect();
            Debug.Log("[TFM VR] Tracking sample complete: Logs/VR-tracking.csv");
            return;
        }
        if (now < nextSample) return;
        nextSample = now + 0.2;
        foreach (XRNode node in new[] { XRNode.Head, XRNode.LeftHand, XRNode.RightHand })
        {
            var d = InputDevices.GetDeviceAtXRNode(node);
            d.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked);
            d.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p);
            d.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion q);
            d.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
            d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis);
            samples.AppendLine(FormattableString.Invariant($"{now:F3},{node},{d.isValid},{tracked},{p.x},{p.y},{p.z},{q.x},{q.y},{q.z},{q.w},{trigger},{axis.x},{axis.y}"));
        }
    }

    [MenuItem("Tools/VR/Configure XR Elite Windows input")]
    public static void ConfigureInput()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before configuring OpenXR.");
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        var profile = settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>()
            .FirstOrDefault(f => f != null && f.GetType().FullName == "VIVE.OpenXR.VIVECosmosProfile");
        if (profile == null) throw new InvalidOperationException("VIVE Cosmos profile is not registered yet.");
        Undo.RecordObject(profile, "Enable XR Elite streaming controller profile");
        profile.enabled = true;
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);

        var simulator = AssetDatabase.LoadMainAssetAtPath("Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset");
        var serialized = new SerializedObject(simulator);
        serialized.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = false;
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(simulator);
        Inspect();
    }

    [MenuItem("Tools/VR/Write diagnostic report")]
    public static void Inspect()
    {
        var b = new StringBuilder();
        b.AppendLine("TFM VR diagnostic " + DateTime.Now.ToString("s"));
        b.AppendLine("Project: " + Application.dataPath);
        b.AppendLine("Unity: " + Application.unityVersion + " Play: " + Application.isPlaying);
        b.AppendLine("Build target: " + EditorUserBuildSettings.activeBuildTarget);
        foreach (var assembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies())
            if (assembly.name.Contains("VIVE") || assembly.name.Contains("Hands"))
                b.AppendLine("COMPILE ASSEMBLY: " + assembly.name + " sources=" + assembly.sourceFiles.Length);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (assembly.GetName().Name.Contains("VIVE") || assembly.GetName().Name.Contains("Hands"))
                b.AppendLine("LOADED ASSEMBLY: " + assembly.GetName().Name);
        var scene = SceneManager.GetActiveScene();
        b.AppendLine("Scene: " + scene.path + " Unsaved: " + scene.isDirty);
        b.AppendLine("GPU: " + SystemInfo.graphicsDeviceName + " VRAM MB: " + SystemInfo.graphicsMemorySize);
        b.AppendLine("API: " + SystemInfo.graphicsDeviceType + " Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        b.AppendLine("OpenXR runtime: " + OpenXRRuntime.name);
        b.AppendLine("XR eye texture: " + XRSettings.eyeTextureWidth + " x " + XRSettings.eyeTextureHeight);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        foreach (var f in settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>())
            if (f != null && f.enabled) b.AppendLine("Enabled OpenXR feature: " + f.GetType().FullName);
        foreach (var root in scene.GetRootGameObjects())
        {
            b.AppendLine("ROOT: " + root.name + " active=" + root.activeSelf + " position=" + root.transform.position);
            if (!root.name.StartsWith("XR Origin", StringComparison.Ordinal)) continue;
            foreach (var c in root.GetComponentsInChildren<Behaviour>(true))
                if (c != null) b.AppendLine("  " + c.gameObject.name + " / " + c.GetType().Name + " enabled=" + c.enabled + " active=" + c.gameObject.activeInHierarchy);
        }
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.AppendLine("CAMERA: " + camera.name + " active=" + camera.isActiveAndEnabled + " parent=" + camera.transform.parent?.name);
        foreach (var d in InputSystem.devices)
            b.AppendLine("INPUT DEVICE: " + d.name + " layout=" + d.layout + " interface=" + d.description.interfaceName + " product=" + d.description.product + " usages=" + string.Join(",", d.usages));
        foreach (XRNode node in new[] { XRNode.Head, XRNode.LeftHand, XRNode.RightHand })
        {
            var d = InputDevices.GetDeviceAtXRNode(node);
            bool tracked; Vector3 position; Quaternion rotation; float trigger; Vector2 axis;
            d.TryGetFeatureValue(CommonUsages.isTracked, out tracked);
            d.TryGetFeatureValue(CommonUsages.devicePosition, out position);
            d.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
            d.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            d.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
            b.AppendLine("XR NODE: " + node + " valid=" + d.isValid + " name=" + d.name + " tracked=" + tracked + " position=" + position.ToString("F4") + " rotation=" + rotation.ToString("F4") + " trigger=" + trigger + " axis=" + axis);
        }
        foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>().Where(a => a.name.Contains("XRI")))
            foreach (var map in asset.actionMaps)
                foreach (var action in map.actions)
                    if (action.name == "Position" || action.name == "Rotation" || action.name == "Select" || action.name == "Move" || action.name == "Snap Turn")
                        b.AppendLine("ACTION: " + map.name + "/" + action.name + " enabled=" + action.enabled + " controls=" + string.Join(",", action.controls.Select(c => c.path)));
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, b.ToString());
        Debug.Log("[TFM VR] Diagnostic written to " + Path.GetFullPath(ReportPath));
    }
}
