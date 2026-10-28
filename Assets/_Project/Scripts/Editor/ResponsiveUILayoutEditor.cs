using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ResponsiveUILayoutEditor
{
    [MenuItem("Tools/Magic Cleaner/Repair Responsive UI")]
    public static void RepairAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before repairing scene UI.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string name in new[] { "MainMenu", "Room1", "Room2", "Room3" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + name + ".unity");
                var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .First(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace);
                ResponsiveUI.ConfigureCanvas(canvas);
                if (name == "MainMenu") RepairMenu(canvas);
                else RepairRoom(canvas);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    static RectTransform Find(Transform root, string name)
    {
        return root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);
    }
    static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name) as RectTransform;
        if (found != null) return found;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static void Region(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
    static void Fixed(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.localScale = Vector3.one;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = position; rt.sizeDelta = size;
    }
    static void FitArt(RectTransform art)
    {
        var image = art.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = false;
        var fit = art.GetComponent<AspectRatioFitter>() ?? art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
        art.localScale = Vector3.one;
    }
    static void Backdrop(Transform parent, Image source, string name)
    {
        var rect = Child(parent, name);
        ResponsiveUI.Stretch(rect);
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = null;
        image.color = new Color(.025f, .018f, .04f, 1f);
        image.raycastTarget = true;
        var fit = rect.GetComponent<AspectRatioFitter>();
        if (fit != null) UnityEngine.Object.DestroyImmediate(fit);
        ResponsiveUI.Stretch(rect);        rect.SetAsFirstSibling();
    }
    static void TextStyle(TMP_Text text, float max = 32f)
    {
        text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = max; text.fontSize = max;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
    }
    static void RepairMenu(Canvas canvas)
    {
        var root = canvas.transform;
        var background = (root.Find("Background") ?? root.Find("MainMenuSafeArea/Background")) as RectTransform;
        // The menu artwork contains the decorative frames. Keep every hit area in its coordinates.
        Backdrop(root, background.GetComponent<Image>(), "MainMenuBackdrop");
        var mainSafe = Child(root, "MainMenuSafeArea");
        ResponsiveUI.Stretch(mainSafe);
        (mainSafe.GetComponent<SafeAreaLayout>() ?? mainSafe.gameObject.AddComponent<SafeAreaLayout>()).Refresh();
        background.SetParent(mainSafe, false); FitArt(background);
        string[] names = { "Btn_Play", "Btn_Levels", "Btn_Settings" };
        string[] labels = { "PLAY", "LEVELS", "SETTINGS" };
        float[] centers = { .303f, .214f, .125f };
        string[] glows = { "SelectionGlow", "SelectionGlow (1)", "SelectionGlow (2)" };
        for (int i = 0; i < names.Length; i++)
        {
            var button = Find(root, names[i]); button.SetParent(background, false);
            Region(button, .415f, centers[i] - .029f, .585f, centers[i] + .029f);
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            ResponsiveUI.Stretch(text.rectTransform); TextStyle(text, 34); text.text = labels[i];
            var glow = Find(root, glows[i]);
            if (glow != null) { glow.SetParent(background, false); Region(glow, .408f, centers[i] - .033f, .592f, centers[i] + .033f); glow.SetAsFirstSibling(); }
        }
        RepairLevels(root);
        RepairSettings(root);
        mainSafe.SetSiblingIndex(1);
    }
    static void RepairLevels(Transform root)
    {
        var panel = Find(root, "LevelsPanel"); ResponsiveUI.Stretch(panel); panel.SetAsLastSibling();
        var art = Find(panel, "BackGround");
        Backdrop(panel, art.GetComponent<Image>(), "LevelsBackdrop");
        var safe = Child(panel, "LevelsSafeArea"); ResponsiveUI.Stretch(safe);
        (safe.GetComponent<SafeAreaLayout>() ?? safe.gameObject.AddComponent<SafeAreaLayout>()).Refresh();
        art.SetParent(safe, false); FitArt(art);
        var manager = panel.GetComponent<LevelsMenuManager>();
        var buttons = new[] { manager.room1Button, manager.room2Button, manager.room3Button };
        var completed = new[] { manager.room1CompletedUI, manager.room2CompletedUI, manager.room3CompletedUI };
        for (int i = 0; i < 3; i++)
        {
            RectTransform card = (RectTransform)buttons[i].transform;
            // Disable duplicate legacy card/button overlays; keep the existing gameplay button and events.
            foreach (Transform child in card) if (child.name != "LevelStatus" && child.name != "new icons" && !completed.Contains(child.gameObject)) child.gameObject.SetActive(false);
            card.SetParent(art, false);
            float left = .137f + i * .254f;
            Region(card, left, .18f, left + .218f, .733f);
            var status = Child(card, "LevelStatus");
            Region(status, .23f, .113f, .87f, .203f);
            var text = status.GetComponent<TextMeshProUGUI>() ?? status.gameObject.AddComponent<TextMeshProUGUI>();
            TextStyle(text, 26); text.text = i == 0 ? "START" : "Locked";
            if (i == 0) manager.room1Text = text;
            else if (i == 1) manager.room2Text = text;
            else manager.room3Text = text;
            var icon = Find(card, "new icons");
            if (icon == null) icon = Child(card, "new icons");
            icon.SetParent(card, false); Region(icon, .13f, .125f, .21f, .19f);
            var iconImage = icon.GetComponent<Image>() ?? icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true; iconImage.raycastTarget = false;
            var done = (RectTransform)completed[i].transform;
            done.SetParent(card, false); Region(done, .17f, .11f, .89f, .21f);
            foreach (var image in done.GetComponentsInChildren<Image>(true))
                if (image.transform != done) image.enabled = false;
            foreach (var label in done.GetComponentsInChildren<TextMeshProUGUI>(true))
            { ResponsiveUI.Stretch(label.rectTransform); TextStyle(label, 24); }
        }
        var back = Find(panel, "Btn_Back"); back.SetParent(art, false);
        Region(back, .024f, .895f, .145f, .965f);
        foreach (var t in back.GetComponentsInChildren<TextMeshProUGUI>(true))
        { ResponsiveUI.Stretch(t.rectTransform); TextStyle(t, 30); t.text = "« BACK"; }
    }
    static void RepairSettings(Transform root)
    {
        var panel = Find(root, "SettingsBookPanel"); panel.SetAsLastSibling();
        // Preserve the full-screen panel's activation and inspector references.
        var image = panel.GetComponent<Image>();
        Backdrop(panel, image, "SettingsBackdrop");
        var safe = Child(panel, "SettingsSafeArea"); ResponsiveUI.Stretch(safe);
        (safe.GetComponent<SafeAreaLayout>() ?? safe.gameObject.AddComponent<SafeAreaLayout>()).Refresh();
        var art = Child(safe, "SettingsArtboard");
        var artImage = art.GetComponent<Image>() ?? art.gameObject.AddComponent<Image>(); artImage.sprite = image.sprite;
        image.enabled = false; ResponsiveUI.Stretch(panel); FitArt(art);
        var left = Find(panel, "LeftPage"); left.SetParent(art, false); Region(left, .205f, .235f, .485f, .70f);
        var right = Find(panel, "RightPage"); right.SetParent(art, false); Region(right, .525f, .235f, .805f, .70f);
        var leftTitle = Find(left, "Text_SettingsTitle"); Region(leftTitle, .1f, 1.07f, .95f, 1.21f);
        var rightTitle = Find(right, "Text_GamePlayTitle"); Region(rightTitle, .05f, 1.07f, .95f, 1.21f);
        TextStyle(leftTitle.GetComponent<TMP_Text>(), 32); TextStyle(rightTitle.GetComponent<TMP_Text>(), 32);
        string[] sliders = { "Slider_MasterVolume", "Slider_Music", "Slider_SFX", "Mute_all" };
        string[] titles = { "Master Volume", "Music", "SFX", "Mute All" };
        for (int i = 0; i < sliders.Length; i++)
        {
            var slider = Find(panel, sliders[i]); if (slider == null) continue;
            slider.SetParent(left, false);
            float y = .83f - i * .22f;
            Region(slider, .40f, y - .04f, .98f, y + .04f);
            var label = Child(slider, "ResponsiveLabel");
            Region(label, -.72f, 0f, -.06f, 1f);
            var text = label.GetComponent<TextMeshProUGUI>() ?? label.gameObject.AddComponent<TextMeshProUGUI>();
            TextStyle(text, 24); text.text = titles[i]; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Left; text.color = new Color(.49f, .25f, .10f, 1f);
        }
        foreach (var legacyLabel in panel.GetComponentsInChildren<TMP_Text>(true))
            if (legacyLabel.name == "Label_Master" || legacyLabel.name == "Label_SFX") legacyLabel.gameObject.SetActive(false);        foreach (Transform child in left)
            if (child.name.StartsWith("Image")) child.gameObject.SetActive(false);
        var share = Find(right, "Btn_Share"); Region(share, .70f, .70f, .90f, .85f);
        var shareLabel = Find(right, "Label_Share"); Region(shareLabel, .03f, .70f, .60f, .85f); TextStyle(shareLabel.GetComponent<TMP_Text>(), 26);
        var language = Find(right, "Languages"); Region(language, .03f, .46f, .45f, .59f);
        if (language.GetComponent<TMP_Text>() != null) TextStyle(language.GetComponent<TMP_Text>(), 25);
        var dropdown = Find(right, "Dropdown_Language"); dropdown.SetParent(right, false); Region(dropdown, .48f, .46f, .98f, .59f);
        var apply = Find(right, "ApplyChanges"); Region(apply, .08f, .03f, .92f, .17f);
        foreach (var t in apply.GetComponentsInChildren<TMP_Text>(true)) { ResponsiveUI.Stretch(t.rectTransform); TextStyle(t, 26); }
        foreach (Transform child in apply) if (child.GetComponent<Button>() != null) ResponsiveUI.Stretch((RectTransform)child);
        var notificationLabel = Find(right, "Label_Notification");
        if (notificationLabel != null) { Region(notificationLabel, .03f, .23f, .65f, .34f); TextStyle(notificationLabel.GetComponent<TMP_Text>(), 25); }
        // Notification controls remain on the right page, clear of the dropdown and Apply button.
        foreach (var slider in panel.GetComponentsInChildren<Slider>(true))
            if (slider.name.IndexOf("Notification", StringComparison.OrdinalIgnoreCase) >= 0)
            { slider.transform.SetParent(right, false); Region((RectTransform)slider.transform, .70f, .23f, .94f, .34f); }
    }
    static void RepairRoom(Canvas canvas)
    {
        var root = canvas.transform;
        var safe = ResponsiveUI.SafeContent(canvas);
        var children = root.Cast<Transform>().Where(t => t != safe).ToArray();
        foreach (Transform child in children) child.SetParent(safe, false);
        var joystick = Find(safe, "Joystick_Background");
        Fixed(joystick, Vector2.zero, new Vector2(.5f, .5f), new Vector2(165, 165), new Vector2(220, 220));
        var handle = Find(joystick, "Joystick_Handle");
        Fixed(handle, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 100));
        var inventory = Find(safe, "Inventory_Bar");
        Fixed(inventory, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(620, 120));
        var bag = Find(safe, "Inventory_Toggle_Button");
        Fixed(bag, Vector2.one, Vector2.one, new Vector2(-40, -120), new Vector2(90, 90));
        var inspect = Find(safe, "InspectBook_Panel"); if (inspect != null) ResponsiveUI.Stretch(inspect);
        // TMP defaults in the authored scene allowed text rectangles larger than the entire dial.
        foreach (var text in safe.GetComponentsInChildren<TMP_Text>(true))
            if (text.name == "Num_Text") { ResponsiveUI.Stretch(text.rectTransform); TextStyle(text, 38); }
    }
}
