using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared screen scaling and safe-area roots for scene and generated UI.</summary>
public static class ResponsiveUI
{
    public static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // Keep the complete design visible on both wide phones and 4:3 tablets.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    public static RectTransform SafeContent(Canvas canvas)
    {
        ConfigureCanvas(canvas);
        Transform existing = canvas.transform.Find("SafeArea");
        if (existing != null) return (RectTransform)existing;
        var root = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaLayout));
        root.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)root.transform);
        root.GetComponent<SafeAreaLayout>().Refresh();
        return (RectTransform)root.transform;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
