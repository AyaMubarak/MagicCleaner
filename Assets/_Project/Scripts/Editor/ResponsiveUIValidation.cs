using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ResponsiveUIValidation
{
    [MenuItem("Tools/Magic Cleaner/Validate Phone and Tablet UI")]
    public static void Validate()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var report = new StringBuilder();
        string output = ".agents/scratch/ui-previews";
        Directory.CreateDirectory(output);
        try
        {
            foreach (string sceneName in new[] { "MainMenu", "Room1", "Room2", "Room3" })
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + sceneName + ".unity");
                var source = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .First(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace);
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2340, 1080), new Vector2Int(2048, 1536) })
                {
                    string[] states = sceneName == "MainMenu" ? new[] { "Main", "Levels", "Settings" } : new[] { "HUD" };
                    foreach (string state in states)
                        Preview(source, sceneName, state, size, output, report);
                }
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        File.WriteAllText(output + "/validation.txt", report.ToString());
    }
    static void Preview(Canvas source, string sceneName, string state, Vector2Int size, string output, StringBuilder report)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture texture = null;
        Texture2D png = null;
        try
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(clone, preview);
            clone.name = "PreviewCanvas";
            var canvas = clone.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            clone.GetComponent<CanvasScaler>().enabled = false;
            var rt = (RectTransform)clone.transform;
            float scale = Mathf.Min(size.x / 1920f, size.y / 1080f);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = (Vector2)size / scale;
            rt.position = Vector3.zero; rt.rotation = Quaternion.identity; rt.localScale = Vector3.one;
            foreach (var transform in clone.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 5;
            // Test an asymmetric notched phone safe area as well as the full tablet viewport.
            Rect safe = size.x == 2340 ? new Rect(88, 24, size.x - 120, size.y - 24) : new Rect(0, 0, size.x, size.y);
            foreach (var area in clone.GetComponentsInChildren<SafeAreaLayout>(true)) { area.enabled = false; area.Apply(safe, size); }
            var manager = clone.GetComponentInChildren<LevelsMenuManager>(true);
            if (manager != null)
            {
                manager.levelsPanel.SetActive(state == "Levels");
                var settings = clone.GetComponentInChildren<SettingsMenuManager>(true);
                if (settings != null && settings.settingsBookPanel != null) settings.settingsBookPanel.SetActive(state == "Settings");
                else
                    foreach (var t in clone.GetComponentsInChildren<RectTransform>(true)) if (t.name == "SettingsBookPanel") t.gameObject.SetActive(state == "Settings");
            }
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.orthographic = true; camera.orthographicSize = rt.sizeDelta.y * .5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 5; camera.farClipPlane = 200;
            // A smaller render keeps the review files light while preserving the exact aspect ratio.
            int w = size.x / 2, h = size.y / 2;
            texture = new RenderTexture(w, h, 24); texture.Create(); camera.targetTexture = texture;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Canvas.ForceUpdateCanvases();
            foreach (var text in clone.GetComponentsInChildren<TMP_Text>())
            {
                if (!text.enabled) continue;
                text.ForceMeshUpdate();
                if (text.rectTransform.rect.width <= 0 || text.rectTransform.rect.height <= 0)
                    throw new Exception(sceneName + "/" + state + ": invalid text rectangle " + text.name);
                if (sceneName == "MainMenu" && state != "Settings" && text.textInfo.lineCount > 1)
                    throw new Exception(sceneName + "/" + state + ": wrapped text " + text.name);
            }
            foreach (var button in clone.GetComponentsInChildren<Button>())
            {
                var rect = (RectTransform)button.transform;
                if (rect.rect.width <= 0 || rect.rect.height <= 0) throw new Exception(sceneName + "/" + state + ": invalid button " + button.name);
                Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector3 p = rt.InverseTransformPoint(corner);
                    if (!rt.rect.Contains(new Vector2(p.x, p.y))) throw new Exception(sceneName + "/" + state + ": offscreen button " + button.name + " at " + size);
                }
            }
            camera.Render();
            RenderTexture previous = RenderTexture.active; RenderTexture.active = texture;
            png = new Texture2D(w, h, TextureFormat.RGB24, false); png.ReadPixels(new Rect(0, 0, w, h), 0, 0); png.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(output + "/" + sceneName + "-" + state + "-" + size.x + "x" + size.y + ".png", png.EncodeToPNG());
            report.AppendLine("PASS " + sceneName + " " + state + " " + size + " buttons visible; safeArea=" + safe);
        }
        finally
        {
            foreach (var root in preview.GetRootGameObjects())
                foreach (var camera in root.GetComponentsInChildren<Camera>()) camera.targetTexture = null;
            if (png != null) UnityEngine.Object.DestroyImmediate(png);
            if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
