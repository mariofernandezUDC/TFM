using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Keep the menu at the same place when a simulation request reloads the scene.</summary>
[RequireComponent(typeof(ScrollRect))]
public class UI_MenuScrollState : MonoBehaviour
{
    static float savedPosition = 1f;
    ScrollRect scroll;
    bool restored;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() { savedPosition = 1f; }

    void Awake() { scroll = GetComponent<ScrollRect>(); }
    void OnEnable() { StartCoroutine(Restore()); }
    IEnumerator Restore()
    {
        restored = false;
        // Accordion Start restores its children after the first layout pass.
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        Canvas.ForceUpdateCanvases();
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = savedPosition;
        restored = true;
    }
    void OnDisable()
    {
        if (restored && scroll != null) savedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition);
    }
}
