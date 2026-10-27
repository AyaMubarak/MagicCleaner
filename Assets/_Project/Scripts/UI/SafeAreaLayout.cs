using UnityEngine;

/// <summary>Insets UI controls for notches and home indicators as the display changes.</summary>
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class SafeAreaLayout : MonoBehaviour
{
    private Rect lastArea;
    private Vector2Int lastSize;
    private void OnEnable() { Refresh(); }
    private void Update()
    {
        if (lastArea != Screen.safeArea || lastSize.x != Screen.width || lastSize.y != Screen.height)
            Refresh();
    }
    public void Refresh()
    {
        Apply(Screen.safeArea, new Vector2Int(Screen.width, Screen.height));
    }
    public void Apply(Rect safeArea, Vector2Int screenSize)
    {
        if (screenSize.x <= 0 || screenSize.y <= 0 || safeArea.width <= 0 || safeArea.height <= 0) return;
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
        rect.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        lastArea = safeArea;
        lastSize = screenSize;
    }
}
