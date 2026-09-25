using System.Collections.Generic;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Windows desktop/VR presentation and free flight. No factory or network logic.</summary>
[DefaultExecutionOrder(-100)]
public class VRPlantRig : MonoBehaviour
{
    public static VRPlantRig Instance { get; private set; }
    public XROrigin origin;
    public Camera desktopCamera;
    public Canvas interfaceCanvas;
    public Shader overlayUIShader;
    public Shader overlayTextShader;
    public UniversalRenderPipelineAsset vrRenderPipeline;
    public float moveSpeed = 0.65f;
    public float verticalSpeed = 0.45f;
    public float snapAngle = 30f;
    public bool smoothTurn = true;
    public bool flyAlongView = true;
    public float turnSpeed = 45f;
    public float accelerationTime = 0.15f;
    public float panelDistance = 1.8f;
    public float panelWidth = 2.8f;
    public float floorHeight = 0f;
    public float minimumEyeHeight = 0.15f;
    VRPiecePopup piecePopup;
    UI_ViewController views;
    static int currentView;
    public bool IsVR { get; private set; }

    readonly List<Canvas> canvases = new List<Canvas>();
    readonly List<Graphic> graphics = new List<Graphic>();
    readonly Dictionary<Material, Material> overlayMaterials = new Dictionary<Material, Material>();
    readonly Dictionary<Material, Material> originalMaterials = new Dictionary<Material, Material>();
    NearFarInteractor[] rays;
    CanvasScaler scaler;
    CanvasGroup visibility;
#if UNITY_EDITOR
    [System.NonSerialized] public bool editorPreview;
#endif
    Vector3 homePosition;
    Quaternion homeRotation;
    bool menuHeld, recenterHeld, homeHeld, previousHeld, turnLatched;
    bool menuVisible;
    bool initialized;
    bool startupReady, awaitingHead;
    int trackedStartupFrames;
    Vector3 velocity;
    RenderPipelineAsset desktopPipeline;
    static bool hasSavedPose;
    static Vector3 savedPosition;
    static Quaternion savedRotation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { Instance = null; hasSavedPose = false; currentView = 0; }

    void Awake()
    {
        Instance = this;
        // El visitante vuela y apunta a UI: su cuerpo/mandos no deben empujar
        // las piezas ni disparar los sensores físicos de la maqueta.
        foreach (var collider in origin.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in origin.GetComponentsInChildren<Rigidbody>(true)) body.detectCollisions = false;
        desktopPipeline = QualitySettings.renderPipeline;
        homePosition = origin.transform.position;
        homeRotation = origin.transform.rotation;
        if (hasSavedPose) origin.transform.SetPositionAndRotation(savedPosition, savedRotation);
        rays = origin.GetComponentsInChildren<NearFarInteractor>(true);
        scaler = interfaceCanvas.GetComponent<CanvasScaler>();
        visibility = interfaceCanvas.GetComponent<CanvasGroup>();
        if (visibility == null) visibility = interfaceCanvas.gameObject.AddComponent<CanvasGroup>();
        views = FindFirstObjectByType<UI_ViewController>(FindObjectsInactive.Include);
        piecePopup = gameObject.AddComponent<VRPiecePopup>();
        piecePopup.Initialize(this);
    }

    IEnumerator Start()
    {
        // UI_ControladorMenu.Start restores the desktop menu state. Open VR after it.
        yield return null;
        startupReady = true;
        SetPresentation(XRSettings.isDeviceActive);
    }

    void Update()
    {
        if (!startupReady) return;
        bool vrActive = XRSettings.isDeviceActive;
#if UNITY_EDITOR
        vrActive |= editorPreview;
#endif
        if (!initialized || IsVR != vrActive) SetPresentation(vrActive);
        if (!IsVR) return;

        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool leftTracked = Tracked(left), rightTracked = Tracked(right);
        bool menu = leftTracked && Button(left, CommonUsages.primaryButton);
        bool recenter = leftTracked && Button(left, CommonUsages.secondaryButton);
        bool home = rightTracked && Button(right, CommonUsages.secondaryButton);
        bool previous = rightTracked && Button(right, CommonUsages.primaryButton);
        if (menu && !menuHeld) ShowMenu(false);
        if (recenter && !recenterHeld) ShowMenu(!menuVisible);
        if (home && !homeHeld) CycleView(1);
        if (previous && !previousHeld) CycleView(-1);
        menuHeld = menu; recenterHeld = recenter; homeHeld = home; previousHeld = previous;

        if (menuVisible) { velocity = Vector3.zero; return; }

        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!Tracked(head)) { velocity = Vector3.zero; return; }
        // A desktop focus change must not stop a headset application.
        // Only an actual scroll area reserves the sticks, not the transparent UI backdrop.
        foreach (var ray in rays)
            if (ray.isActiveAndEnabled && ray.TryGetCurrentUIRaycastResult(out var hit) &&
                hit.gameObject != null && hit.gameObject.GetComponentInParent<ScrollRect>() != null)
            { velocity = Vector3.zero; return; }

        Vector2 move = leftTracked ? Axis(left) : Vector2.zero;
        Vector2 turn = rightTracked ? Axis(right) : Vector2.zero;
        Vector3 forward = flyAlongView ? origin.Camera.transform.forward :
            Vector3.ProjectOnPlane(origin.Camera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = origin.transform.forward;
        Vector3 rightDirection = Vector3.ProjectOnPlane(origin.Camera.transform.right, Vector3.up).normalized;
        Vector3 direction = forward * move.y + rightDirection * move.x;
        float delta = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        Vector3 targetVelocity = Vector3.ClampMagnitude(direction * moveSpeed + Vector3.up * turn.y * verticalSpeed, moveSpeed);
        velocity = Vector3.Lerp(velocity, targetVelocity, 1f - Mathf.Exp(-delta / Mathf.Max(0.01f, accelerationTime)));
        origin.transform.position += velocity * delta;
        ClampAboveFloor();
        if (smoothTurn)
        {
            origin.transform.RotateAround(origin.Camera.transform.position, Vector3.up, turn.x * turnSpeed * delta);
            return;
        }
        if (Mathf.Abs(turn.x) < 0.25f) turnLatched = false;
        if (!turnLatched && Mathf.Abs(turn.x) > 0.7f)
        {
            origin.transform.RotateAround(origin.Camera.transform.position, Vector3.up, Mathf.Sign(turn.x) * snapAngle);
            turnLatched = true;
        }
    }

    static bool Tracked(InputDevice d) => d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked, out bool value) && value;
    static bool Button(InputDevice d, InputFeatureUsage<bool> usage) => d.TryGetFeatureValue(usage, out bool value) && value;
    static Vector2 Axis(InputDevice d)
    {
        d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value);
        if (value.magnitude < 0.2f) return Vector2.zero;
        return Vector2.ClampMagnitude(value.normalized * ((value.magnitude - 0.2f) / 0.8f), 1f);
    }

    void SetPresentation(bool vr)
    {
        initialized = true;
        IsVR = vr;
        velocity = Vector3.zero;
        awaitingHead = vr;
        trackedStartupFrames = 0;
        if (vrRenderPipeline != null) QualitySettings.renderPipeline = vr ? vrRenderPipeline : desktopPipeline;
        origin.Camera.enabled = vr;
        var xrAudio = origin.Camera.GetComponent<AudioListener>();
        if (xrAudio != null) xrAudio.enabled = vr;
        if (desktopCamera != null) desktopCamera.gameObject.SetActive(!vr);
        if (scaler != null) scaler.enabled = !vr;
        interfaceCanvas.gameObject.SetActive(true);
        interfaceCanvas.renderMode = vr ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
        interfaceCanvas.worldCamera = vr ? origin.Camera : desktopCamera;
        if (vr)
        {
            var rect = (RectTransform)interfaceCanvas.transform;
            rect.sizeDelta = new Vector2(2200, 1200);
            rect.localScale = Vector3.one * (panelWidth / 2200f);
            ShowMenu(false);
            RecenterPanel();
        }
        else
        {
            interfaceCanvas.transform.localScale = Vector3.one;
            visibility.alpha = 1; visibility.blocksRaycasts = true; visibility.interactable = true;
            piecePopup.SetVisible(false);
        }
        if (vr) { visibility.alpha = 0; visibility.blocksRaycasts = false; visibility.interactable = false; }
        ConfigureCanvases();
    }

    public void ShowMenu(bool show)
    {
        menuVisible = show;
        if (IsVR)
        {
            visibility.alpha = 0; visibility.blocksRaycasts = false; visibility.interactable = false;
            piecePopup.SetVisible(show);
            return;
        }
        // Hiding the panel must not disable simulation, clocks or MQTT UI subscriptions.
        visibility.alpha = show ? 1 : 0;
        visibility.blocksRaycasts = show;
        visibility.interactable = show;
        if (!show) return;
        RecenterPanel();
        var menu = UI_ControladorMenu.Instance;
        if (menu != null && menu.panelLateral != null && !menu.panelLateral.activeSelf) menu.ToggleMenu();
    }

    public void RecenterPanel()
    {
        if (!IsVR) return;
        if (piecePopup != null) piecePopup.Recenter();
        var cameraTransform = origin.Camera.transform;
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = origin.transform.forward;
        interfaceCanvas.transform.SetPositionAndRotation(cameraTransform.position + forward * panelDistance,
            Quaternion.LookRotation(forward, Vector3.up));
    }

    public void GoToView(Transform target)
    {
        if (!IsVR || target == null) return;
        float yaw = Mathf.DeltaAngle(origin.Camera.transform.eulerAngles.y, target.eulerAngles.y);
        origin.transform.RotateAround(origin.Camera.transform.position, Vector3.up, yaw);
        origin.transform.position += target.position - origin.Camera.transform.position;
        velocity = Vector3.zero;
        ClampAboveFloor();
        RecenterPanel();
    }

    public void CycleView(int step)
    {
        if (views == null || views.listaVistas == null || views.listaVistas.Length == 0) return;
        int count = views.listaVistas.Length;
        for (int i = 0; i < count; i++)
        {
            currentView = (currentView + (step < 0 ? -1 : 1) + count) % count;
            var view = views.listaVistas[currentView];
            if (view == null || view.transformObjetivo == null) continue;
            GoToView(view.transformObjetivo);
            return;
        }
    }

    public void ClampAboveFloor()
    {
        if (!IsVR || origin.Camera == null) return;
        float correction = floorHeight + minimumEyeHeight - origin.Camera.transform.position.y;
        if (correction > 0) origin.transform.position += Vector3.up * correction;
        if (correction > 0 && velocity.y < 0) velocity.y = 0;
    }

    void LateUpdate()
    {
        if (awaitingHead && Tracked(InputDevices.GetDeviceAtXRNode(XRNode.Head)))
        {
            if (++trackedStartupFrames >= 3)
            {
                awaitingHead = false;
                ClampAboveFloor();
            }
        }
        // TMP creates new canvases for dropdown lists and blockers at runtime.
        // Configure them in the same frame, including inactive templates.
        if (initialized && IsVR) ClampAboveFloor();
        if (initialized && !IsVR && interfaceCanvas.gameObject.activeInHierarchy) ConfigureCanvases();
        if (initialized && IsVR && XRSettings.isDeviceActive)
        {
            savedPosition = origin.transform.position;
            savedRotation = origin.transform.rotation;
            hasSavedPose = true;
        }
    }

    void ConfigureCanvases()
    {
        // The compact VR popup owns its raycaster/materials; no full-menu scans per frame.
        if (IsVR) return;
        interfaceCanvas.GetComponentsInChildren(true, canvases);
        foreach (var canvas in canvases)
        {
            canvas.worldCamera = IsVR ? origin.Camera : desktopCamera;
            var tracked = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (tracked == null) tracked = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            tracked.enabled = IsVR;
            tracked.checkFor3DOcclusion = false;
            tracked.checkFor2DOcclusion = false;
            // Keep the regular raycaster: XRUIInputModule also accepts desktop mouse input.
        }
        interfaceCanvas.GetComponentsInChildren(true, graphics);
        foreach (var graphic in graphics)
        {
            var text = graphic as TMPro.TMP_Text;
            Material current = text != null ? text.fontSharedMaterial : graphic.material;
            if (current == null) continue;
            if (!IsVR)
            {
                if (originalMaterials.TryGetValue(current, out var original))
                {
                    if (text != null) text.fontSharedMaterial = original;
                    else graphic.material = original;
                }
                continue;
            }
            if (originalMaterials.ContainsKey(current)) continue;
            Shader shader = text != null ? overlayTextShader : overlayUIShader;
            if (shader == null) continue;
            // Custom non-UI materials are kept intact.
            if (text == null && current.shader.name != "UI/Default" && current.shader.name != "TextMeshPro/Sprite") continue;
            if (!overlayMaterials.TryGetValue(current, out var overlay))
            {
                overlay = new Material(current) { shader = shader, renderQueue = 4000, name = current.name + " (VR overlay)" };
                overlayMaterials.Add(current, overlay);
                originalMaterials.Add(overlay, current);
            }
            if (text != null) text.fontSharedMaterial = overlay;
            else graphic.material = overlay;
        }
    }

    void OnDestroy()
    {
        if (origin != null && initialized && IsVR && XRSettings.isDeviceActive)
        {
            savedPosition = origin.transform.position;
            savedRotation = origin.transform.rotation;
            hasSavedPose = true;
        }
        if (Instance == this) Instance = null;
        if (vrRenderPipeline != null && QualitySettings.renderPipeline == vrRenderPipeline)
            QualitySettings.renderPipeline = desktopPipeline;
        foreach (var material in overlayMaterials.Values) Destroy(material);
    }
}
