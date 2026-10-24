using UnityEngine;
using System.IO;

public static class ProceduralIconsGenerator
{
    private const string FolderPath = "Assets/_Project/Resources";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void EnsureStartAndPlayIcons()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        string openPath = Path.Combine(FolderPath, "padlock_open.png");
        string playPath = Path.Combine(FolderPath, "play_start_icon.png");

        // 1. Create a sleek golden open padlock (crisp vector-like drawing)
        if (!File.Exists(openPath) || new FileInfo(openPath).Length < 1000)
        {
            byte[] bytes = GenerateCrispOpenLockPNG();
            File.WriteAllBytes(openPath, bytes);
        }

        // 2. Create a glowing golden triangular Play button icon
        if (!File.Exists(playPath))
        {
            byte[] bytes = GenerateGoldenPlayArrowPNG();
            File.WriteAllBytes(playPath, bytes);
        }
    }

    private static byte[] GenerateCrispOpenLockPNG()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        Color goldBody = new Color(0.98f, 0.78f, 0.22f, 1f);
        Color goldLight = new Color(1.0f, 0.95f, 0.55f, 1f);
        Color shackleGold = new Color(1.0f, 0.88f, 0.40f, 1f);
        Color innerDark = new Color(0.35f, 0.22f, 0.05f, 1f);

        // Body: Rounded rectangle at bottom
        int bodyLeft = 56, bodyRight = 200, bodyBottom = 30, bodyTop = 135;
        for (int y = bodyBottom; y <= bodyTop; y++)
        {
            for (int x = bodyLeft; x <= bodyRight; x++)
            {
                float t = (float)(y - bodyBottom) / (bodyTop - bodyBottom);
                Color c = Color.Lerp(goldBody, goldLight, t * 0.7f);
                cols[y * size + x] = c;
            }
        }

        // Keyhole in body
        int khCenterX = 128, khCenterY = 92, khRadius = 16;
        for (int y = 55; y <= 115; y++)
        {
            for (int x = 112; x <= 144; x++)
            {
                float distSq = (x - khCenterX) * (x - khCenterX) + (y - khCenterY) * (y - khCenterY);
                if (distSq <= khRadius * khRadius)
                {
                    cols[y * size + x] = innerDark;
                }
                else if (y < khCenterY && y >= 60 && Mathf.Abs(x - khCenterX) <= 8)
                {
                    cols[y * size + x] = innerDark;
                }
            }
        }

        // Shackle: Arched curved bar OPEN on the right side
        // Left upright bar: from y=135 up to y=205, x from 72 to 98
        for (int y = 135; y <= 205; y++)
        {
            for (int x = 72; x <= 98; x++)
            {
                cols[y * size + x] = shackleGold;
            }
        }

        // Top arc: centered at (128, 205), outer radius 56, inner radius 30
        Vector2 arcCenter = new Vector2(128, 205);
        for (int y = 180; y <= 250; y++)
        {
            for (int x = 72; x <= 184; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), arcCenter);
                if (d >= 30 && d <= 56 && y >= 200)
                {
                    cols[y * size + x] = shackleGold;
                }
            }
        }

        // Right bar (Raised/Open, ending higher at y=175)
        for (int y = 175; y <= 205; y++)
        {
            for (int x = 158; x <= 184; x++)
            {
                cols[y * size + x] = shackleGold;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex.EncodeToPNG();
    }

    private static byte[] GenerateGoldenPlayArrowPNG()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.clear;

        Vector2 p1 = new Vector2(75, 45);   // Bottom left
        Vector2 p2 = new Vector2(75, 211);  // Top left
        Vector2 p3 = new Vector2(215, 128); // Right apex

        Color gold1 = new Color(1.0f, 0.85f, 0.28f, 1f);
        Color gold2 = new Color(1.0f, 0.95f, 0.60f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pt = new Vector2(x, y);
                if (IsPointInTriangle(pt, p1, p2, p3))
                {
                    float t = (float)x / size;
                    cols[y * size + x] = Color.Lerp(gold1, gold2, t);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex.EncodeToPNG();
    }

    private static bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d1 = Sign(pt, v1, v2);
        float d2 = Sign(pt, v2, v3);
        float d3 = Sign(pt, v3, v1);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
