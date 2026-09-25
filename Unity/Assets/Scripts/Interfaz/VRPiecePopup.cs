using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Compact VR presentation. Orders still use the existing simulation controller.</summary>
public class VRPiecePopup : MonoBehaviour
{
    VRPlantRig rig;
    Canvas canvas;
    TMP_FontAsset font;
    Material textMaterial, uiMaterial;
    TMP_Text status;
    readonly Button[] buttons = new Button[3];
    bool pending;
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    public void Initialize(VRPlantRig owner)
    {
        rig = owner;
        var source = rig.interfaceCanvas.GetComponentInChildren<TMP_Text>(true);
        font = source != null ? source.font : TMP_Settings.defaultFontAsset;
        textMaterial = new Material(font.material) { shader = rig.overlayTextShader, renderQueue = 4000 };
        uiMaterial = new Material(rig.overlayUIShader) { renderQueue = 4000 };
        var root = new GameObject("VR - Pedir pieza", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
        root.layer = 5;
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = rig.origin.Camera;
        canvas.sortingOrder = 100;
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(1000, 460);
        rect.localScale = Vector3.one * 0.0012f;
        var ray = root.GetComponent<TrackedDeviceGraphicRaycaster>();
        ray.checkFor2DOcclusion = false; ray.checkFor3DOcclusion = false;
        Box("Fondo", rect, Vector2.zero, new Vector2(1000, 460), new Color(0.025f, 0.04f, 0.07f));
        Label("Título", rect, "¿Qué pieza quieres?", new Vector2(0, 150), new Vector2(920, 90), 64);
        string[] labels = { "AZUL", "ROJA", "BLANCA" };
        string[] colors = { "BLUE", "RED", "WHITE" };
        Color[] fills = { new Color(0.035f, 0.19f, 0.55f), new Color(0.48f, 0.045f, 0.065f), new Color(0.94f, 0.96f, 1f) };
        for (int i = 0; i < buttons.Length; i++)
        {
            var image = Box(labels[i], rect, new Vector2((i - 1) * 310, 5), new Vector2(285, 155), fills[i]);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var palette = button.colors;
            palette.highlightedColor = new Color(0.8f, 0.9f, 1f);
            palette.pressedColor = new Color(0.6f, 0.7f, 0.8f);
            palette.disabledColor = new Color(0.35f, 0.35f, 0.35f);
            button.colors = palette;
            var label = Label("Texto", image.rectTransform, labels[i], Vector2.zero, new Vector2(270, 130), 55);
            if (i == 2) label.color = new Color(0.025f, 0.04f, 0.07f);
            string color = colors[i];
            button.onClick.AddListener(() => RequestPiece(color));
            buttons[i] = button;
        }
        status = Label("Estado", rect, "Apunta y pulsa el gatillo", new Vector2(0, -125), new Vector2(920, 65), 36);
        Label("Ayuda", rect, "Y · Cerrar     A / B · Cambiar vista", new Vector2(0, -190), new Vector2(920, 55), 30);
        SimuladorOffline.OnEstadoSimulacionOfflineCambiado += OnSimulationChanged;
        root.SetActive(false);
    }

    Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color; image.material = uiMaterial;
        return image;
    }

    TMP_Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSharedMaterial = textMaterial;
        text.text = value; text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; text.raycastTarget = false;
        return text;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    public void SetVisible(bool show)
    {
        canvas.gameObject.SetActive(show);
        if (show) { RefreshState(); Recenter(); }
    }

    public void Recenter()
    {
        if (canvas == null) return;
        var head = rig.origin.Camera.transform;
        var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = rig.origin.transform.forward;
        canvas.transform.SetPositionAndRotation(head.position + forward * 1.25f,
            Quaternion.LookRotation(forward, Vector3.up));
    }

    public void RequestPiece(string color)
    {
        if (color != "BLUE" && color != "RED" && color != "WHITE") return;
        if (pending || (SimuladorOffline.Instance != null && SimuladorOffline.Instance.EnEjecucion)) return;
        var menu = UI_ControladorMenu.Instance;
        if (menu == null) return;
        pending = true;
        rig.ShowMenu(false);
        menu.PedirPiezaSimulacion(color);
    }

    void OnSimulationChanged(bool running) { pending = false; RefreshState(); }

    void RefreshState()
    {
        bool busy = pending || (SimuladorOffline.Instance != null && SimuladorOffline.Instance.EnEjecucion);
        foreach (var button in buttons) button.interactable = !busy;
        status.text = busy ? "Pieza en proceso…" : "Apunta y pulsa el gatillo";
    }

    void OnDestroy()
    {
        SimuladorOffline.OnEstadoSimulacionOfflineCambiado -= OnSimulationChanged;
        if (canvas != null) Destroy(canvas.gameObject);
        if (textMaterial != null) Destroy(textMaterial);
        if (uiMaterial != null) Destroy(uiMaterial);
    }
}
