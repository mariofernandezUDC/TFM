using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public static class VRPlantSetup
{
    [MenuItem("Tools/VR/Apply plant UI and flight")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GemeloDigital_LearningFactory.unity") throw new InvalidOperationException("Unexpected scene.");
        Directory.CreateDirectory("../Docs/Backup_20260923");
        if (scene.isDirty) throw new InvalidOperationException("Save the current scene before applying the migration.");
        string backup = "../Docs/Backup_20260923/Before_UI_flight.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
        var menu = UnityEngine.Object.FindFirstObjectByType<UI_ControladorMenu>(FindObjectsInactive.Include);
        var canvas = menu.GetComponentInParent<Canvas>().rootCanvas;
        var desktop = scene.GetRootGameObjects().First(g => g.name == "Main Camera").GetComponent<Camera>();
        var rig = origin.GetComponent<VRPlantRig>();
        if (rig == null) rig = Undo.AddComponent<VRPlantRig>(origin.gameObject);
        rig.origin = origin; rig.desktopCamera = desktop; rig.interfaceCanvas = canvas;
        rig.panelWidth = 2.8f;
        rig.overlayUIShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/VR_UIOverlay.shader");
        rig.overlayTextShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile Overlay.shader");
        rig.vrRenderPipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/VR_RPAsset.asset");
        rig.smoothTurn = true; rig.flyAlongView = true;
        rig.floorHeight = 0f; rig.minimumEyeHeight = 0.15f;
        EditorUtility.SetDirty(rig);
        // One motion implementation, with the existing XRI rays and UI actions retained.
        foreach (var manager in origin.GetComponentsInChildren<ControllerInputActionManager>(true))
        {
            manager.smoothMotionEnabled = true;
            manager.uiScrollingEnabled = true;
            EditorUtility.SetDirty(manager);
        }
        foreach (var ray in origin.GetComponentsInChildren<NearFarInteractor>(true))
        { ray.enableUIInteraction = true; EditorUtility.SetDirty(ray); }
        var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        foreach (var input in events.GetComponents<BaseInputModule>())
            if (!(input is XRUIInputModule)) { input.enabled = false; EditorUtility.SetDirty(input); }
        var xrInput = events.GetComponent<XRUIInputModule>();
        if (xrInput == null) xrInput = Undo.AddComponent<XRUIInputModule>(events.gameObject);
        xrInput.enabled = true; xrInput.enableXRInput = true; xrInput.enableMouseInput = true;
        xrInput.enableGamepadInput = false; xrInput.enableJoystickInput = false;
        EditorUtility.SetDirty(xrInput);
        foreach (var c in canvas.GetComponentsInChildren<Canvas>(true))
        {
            if (!c.GetComponent<TrackedDeviceGraphicRaycaster>()) Undo.AddComponent<TrackedDeviceGraphicRaycaster>(c.gameObject);
            // Nested canvases (including TMP templates) inherit their root's render mode.
        }
        FixSimulation(menu.seccionSimulacion);
        var scroll = menu.panelLateral.GetComponentInChildren<ScrollRect>(true);
        if (!scroll.GetComponent<UI_MenuScrollState>()) Undo.AddComponent<UI_MenuScrollState>(scroll.gameObject);
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false; scroll.scrollSensitivity = 25f;
        EditorUtility.SetDirty(scroll);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Report();
    }

    static void FixSimulation(UI_SeccionSimulacion simulation)
    {
        var section = simulation.GetComponent<VerticalLayoutGroup>();
        section.padding = new RectOffset(12, 12, 12, 12);
        section.spacing = 18; section.childControlWidth = true;
        section.childControlHeight = false;
        var accordion = simulation.GetComponent<UI_SeccionAcordeon>();
        var content = accordion.contenedorContenido.GetComponent<RectTransform>();
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 12, 12); layout.spacing = 18;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true; layout.childControlHeight = false;
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var toggle = content.GetComponentInChildren<Toggle>(true);
        ((RectTransform)toggle.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
        var row = content.Find("Btns_Pedidos").GetComponent<HorizontalLayoutGroup>();
        row.spacing = 10; row.padding = new RectOffset(0, 0, 0, 0);
        row.childControlWidth = true; row.childForceExpandWidth = true;
        row.childControlHeight = true; row.childForceExpandHeight = true;
        ((RectTransform)row.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 150);
        foreach (RectTransform card in row.transform)
        {
            var image = card.Find("Imagen") as RectTransform;
            var holder = card.Find("Boton") as RectTransform;
            SetRect(image, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(78, 78));
            SetRect(holder, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 4), new Vector2(116, 42));
            var button = holder.GetComponentInChildren<Button>(true);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var text = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (text != null) { text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = 19; }
            foreach (var graphic in image.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }
        foreach (var component in simulation.GetComponentsInChildren<Component>(true)) EditorUtility.SetDirty(component);
    }

    static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    { rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }

    [MenuItem("Tools/VR/Report UI and flight")]
    public static void Report()
    {
        var b = new StringBuilder("UI/flight report " + DateTime.Now.ToString("s") + "\n");
        var rig = UnityEngine.Object.FindFirstObjectByType<VRPlantRig>();
        b.AppendLine("Rig=" + (rig != null) + " Play=" + Application.isPlaying + " VR=" + (rig != null && rig.IsVR));
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.AppendLine("Canvas " + c.name + " root=" + c.rootCanvas.name + " mode=" + c.renderMode + " tracked=" + (c.GetComponent<TrackedDeviceGraphicRaycaster>() != null));
        foreach (var module in UnityEngine.Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.AppendLine("Input " + module.GetType().Name + " enabled=" + module.enabled);
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/VR-ui-flight.txt", b.ToString());
        Debug.Log(b.ToString());
    }

    [MenuItem("Tools/VR/Check simulation scroll bounds")]
    public static void CheckScroll()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Start Play first.");
        var menu = UI_ControladorMenu.Instance;
        if (!menu.panelLateral.activeSelf) menu.ToggleMenu();
        menu.seccionSimulacion.GetComponent<UI_SeccionAcordeon>().AbrirSeccionDirecto();
        Canvas.ForceUpdateCanvases();
        var scroll = menu.panelLateral.GetComponentInChildren<ScrollRect>(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Canvas.ForceUpdateCanvases();
        scroll.StopMovement(); scroll.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();
        var b = new StringBuilder();
        foreach (var button in new[] { menu.seccionSimulacion.btnSimPedirBlanca, menu.seccionSimulacion.btnSimPedirRoja, menu.seccionSimulacion.btnSimPedirAzul })
        {
            var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
            var rect = scroll.viewport.rect;
            bool inside = corners.All(p => rect.Contains((Vector2)scroll.viewport.InverseTransformPoint(p)));
            b.AppendLine(button.transform.parent.parent.name + " fullyInsideViewport=" + inside);
            if (!inside) Debug.LogError("Button outside viewport: " + button.name);
        }
        b.AppendLine("content=" + scroll.content.rect.height + " viewport=" + scroll.viewport.rect.height + " mode=" + scroll.movementType);
        File.WriteAllText("Logs/VR-scroll-check.txt", b.ToString()); Debug.Log(b.ToString());
    }

    [MenuItem("Tools/VR/Capture menu")]
    public static void Capture()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Start Play first.");
        ScreenCapture.CaptureScreenshot("Logs/VR-menu.png");
    }

    [MenuItem("Tools/VR/Test offline blue request")]
    public static void TestBlue()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Start Play first.");
        var menu = UI_ControladorMenu.Instance;
        menu.toggleModoSimulacion.isOn = true;
        menu.seccionSimulacion.btnSimPedirAzul.onClick.Invoke();
    }

    [MenuItem("Tools/VR/Test world UI rays without headset")]
    public static async void TestWorldUI()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Start Play first.");
        var rig = VRPlantRig.Instance;
        var report = new StringBuilder("Synthetic XR ray test (not a hardware test)\n");
        try
        {
            rig.editorPreview = true;
            await Task.Delay(300);
            rig.ShowMenu(true);
            report.AppendLine("WorldSpace=" + (rig.interfaceCanvas.renderMode == RenderMode.WorldSpace));
            var menu = UI_ControladorMenu.Instance;
            var scroll = menu.panelLateral.GetComponentInChildren<ScrollRect>(true);
            foreach (var section in menu.panelLateral.GetComponentsInChildren<UI_SeccionAcordeon>(true))
            {
                section.AbrirSeccionDirecto();
                await Task.Delay(100);
                foreach (var control in section.GetComponentsInChildren<Selectable>())
                {
                    bool hit = false;
                    for (int step = 0; step <= 4 && !hit; step++)
                    {
                        scroll.verticalNormalizedPosition = step / 4f;
                        Canvas.ForceUpdateCanvases();
                        hit = RayHits(control, rig.origin.Camera);
                    }
                    report.AppendLine(section.name + "/" + control.name + " " + control.GetType().Name + " ray=" + hit);
                }
                foreach (var dropdown in section.GetComponentsInChildren<TMPro.TMP_Dropdown>())
                {
                    bool enabled = dropdown.interactable;
                    dropdown.interactable = true;
                    dropdown.Show();
                    await Task.Delay(150);
                    var list = dropdown.transform.Find("Dropdown List");
                    var item = list != null ? list.GetComponentInChildren<Toggle>() : null;
                    report.AppendLine("Dropdown " + dropdown.name + " created=" + (list != null) + " itemRay=" + (item != null && RayHits(item, rig.origin.Camera)));
                    dropdown.Hide(); dropdown.interactable = enabled;
                    await Task.Delay(200);
                }
                foreach (var calendar in section.GetComponentsInChildren<UI_CalendarPicker>())
                {
                    calendar.ToggleCalendario();
                    await Task.Delay(150);
                    var overlay = rig.interfaceCanvas.transform.Find("Calendar_Overlay_Root");
                    // There can be separate start/end calendars. Use the active one.
                    var modal = rig.interfaceCanvas.GetComponentsInChildren<Canvas>().FirstOrDefault(c => c.name == "Calendar_Overlay_Root");
                    var day = modal != null ? modal.GetComponentsInChildren<Button>().LastOrDefault(b => b.interactable) : null;
                    report.AppendLine("Calendar " + calendar.name + " world=" + (modal != null && modal.renderMode == RenderMode.WorldSpace) + " dayRay=" + (day != null && RayHits(day, rig.origin.Camera)));
                    calendar.ToggleCalendario();
                    await Task.Delay(100);
                }
            }
            rig.ShowMenu(false);
            report.AppendLine("Hidden menu keeps controller active=" + menu.isActiveAndEnabled);
            rig.ShowMenu(true);
            CheckScroll();
            ScreenCapture.CaptureScreenshot("Logs/VR-world-menu.png");
            await Task.Delay(200);
        }
        catch (Exception ex) { report.AppendLine("ERROR " + ex); Debug.LogException(ex); }
        finally
        {
            if (rig != null) rig.editorPreview = false;
            File.WriteAllText("Logs/VR-world-ray-check.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }

    [MenuItem("Tools/VR/Validate compact piece popup")]
    public static async void ValidateCompactPopup()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play required.");
        var rig = UnityEngine.Object.FindFirstObjectByType<VRPlantRig>();
        var report = new StringBuilder("Compact VR popup validation\n");
        bool preview = rig.editorPreview;
        var position = rig.origin.transform.position;
        var rotation = rig.origin.transform.rotation;
        try
        {
            rig.editorPreview = true;
            await Task.Delay(200);
            rig.ShowMenu(true);
            await Task.Delay(250);
            Canvas.ForceUpdateCanvases();
            var popup = GameObject.Find("VR - Pedir pieza");
            var buttons = popup.GetComponentsInChildren<Button>();
            report.AppendLine("Exactly three order buttons=" + (buttons.Length == 3));
            foreach (var button in buttons)
                report.AppendLine(button.name + " tracked ray=" + RayHits(button, rig.origin.Camera));
            var old = rig.interfaceCanvas.GetComponent<CanvasGroup>();
            report.AppendLine("Other menus hidden and noninteractive=" + (old.alpha == 0 && !old.blocksRaycasts));
            rig.origin.transform.position += Vector3.down * 20;
            rig.ClampAboveFloor();
            report.AppendLine("Floor enforced=" + (rig.origin.Camera.transform.position.y >= rig.floorHeight + rig.minimumEyeHeight - 0.0001f));
            rig.origin.transform.SetPositionAndRotation(position, rotation);
            var views = UnityEngine.Object.FindFirstObjectByType<UI_ViewController>(FindObjectsInactive.Include);
            for (int i = 0; i < views.listaVistas.Length; i++) rig.CycleView(1);
            report.AppendLine("Full view cycle completed; eye above floor=" + (rig.origin.Camera.transform.position.y >= rig.floorHeight));
            rig.CycleView(1);
            var next = rig.origin.Camera.transform.position;
            rig.CycleView(-1); rig.CycleView(1);
            report.AppendLine("Previous/next returns to same view=" + (Vector3.Distance(next, rig.origin.Camera.transform.position) < 0.001f));
            rig.CycleView(-1);
            rig.origin.transform.SetPositionAndRotation(position, rotation);
            rig.RecenterPanel();
            // Render a front-on UI preview without moving the tracked headset camera.
            var cameraObject = new GameObject("Popup validation camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().allowXRRendering = false;
            camera.cullingMask = 1 << 5;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            camera.fieldOfView = 36;
            camera.transform.SetPositionAndRotation(popup.transform.position - popup.transform.forward * 1.25f, popup.transform.rotation);
            var target = new RenderTexture(1600, 900, 24);
            var previousTarget = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
            File.WriteAllBytes("Logs/VR-compact-popup.png", texture.EncodeToPNG());
            RenderTexture.active = previousTarget;
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(cameraObject);
        }
        catch (Exception ex) { report.AppendLine("ERROR " + ex); Debug.LogException(ex); }
        finally
        {
            rig.origin.transform.SetPositionAndRotation(position, rotation);
            rig.ShowMenu(false); rig.editorPreview = preview;
            File.WriteAllText("Logs/VR-compact-check.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }

    static bool RayHits(Selectable control, Camera camera)
    {
        var rect = (RectTransform)control.transform;
        Vector3 target = rect.TransformPoint(rect.rect.center);
        Vector3 start = camera.transform.position;
        var data = new TrackedDeviceEventData(EventSystem.current)
        {
            pointerId = -100,
            position = new Vector2(-10000, -10000),
            layerMask = ~0,
            rayPoints = new List<Vector3> { start, target + (target - start).normalized * 0.1f }
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(control.transform))
            Debug.Log("Ray diagnostic " + control.name + ": " + string.Join(", ", hits.Select(h => h.gameObject.name)));
        return hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(control.transform);
    }
}
