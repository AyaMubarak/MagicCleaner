using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI Components")]
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private Image[] slotIcons;                // صور الأدوات داخل الخانات
    [SerializeField] private Button[] slotButtons;             // أزرار الخانات

    [Header("Previews")]
    [SerializeField] private GameObject inspectBookPanel;
    [SerializeField] private GameObject notePreviewPanel;
    [SerializeField] private TextMeshProUGUI noteTextDisplay;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 4f;
    [SerializeField] private float fadeSpeed = 4f;

    private class InventoryEntry
    {
        public string id;
        public Sprite icon;
    }

    private static List<InventoryEntry> persistentItems = new List<InventoryEntry>();
    private List<InventoryEntry> inventoryItems = new List<InventoryEntry>();
    public string SelectedItem { get; private set; } = "";

    private Coroutine hideCoroutine;
    private bool isManuallyOpen = false;
    private int lastToggleFrame = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Support pressing 'B' or 'I' on keyboard to toggle bag as well
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    private void Start()
    {
        isManuallyOpen = false;
        SetInventoryVisible(false);

        if (inspectBookPanel != null) inspectBookPanel.SetActive(false);
        if (notePreviewPanel != null) notePreviewPanel.SetActive(false);

        RepositionBagButton();
        BindInventoryToggleButton();

        // Ø¯Ø§Ø¦Ù…Ø§Ù‹ Ø§Ø³ØªØ±Ø¬Ø§Ø¹ ÙˆØªØ£ÙƒÙŠØ¯ Ù…Ø­ØªÙˆÙŠØ§Øª Ø§Ù„Ø­Ù‚ÙŠØ¨Ø© Ù…Ù† Ø§Ù„Ù‚Ø±Øµ ÙˆÙ…Ù† Ø§Ù„Ù…Ù‡Ø§Ù… Ø§Ù„Ù…Ù†Ø¬Ø²Ø© Ø­ØªÙ‰ Ù„Ø§ ØªØ¶ÙŠØ¹ Ø§Ù„Ø£Ø¯ÙˆØ§Øª Ø£Ø¨Ø¯Ø§Ù‹
        LoadInventoryFromDisk();

        // Ø±Ø¨Ø· Ø£Ø²Ø±Ø§Ø± Ø§Ù„Ø®Ø§Ù†Ø§Øª ØªÙ„Ù‚Ø§Ø¦ÙŠØ§Ù‹ ÙˆØ§Ù„ØªØ£ÙƒØ¯ Ù…Ù† Ø¥Ù…ÙƒØ§Ù†ÙŠØ© Ø§Ù„Ù†Ù‚Ø± Ø¹Ù„Ù‰ Ø§Ù„Ø£ÙŠÙ‚ÙˆÙ†Ø§Øª Ù…Ø¨Ø§Ø´Ø±Ø© Ø­ØªÙ‰ Ù„Ùˆ ÙƒØ§Ù† slotButtons ÙØ§Ø±ØºØ§Ù‹ ÙÙŠ Ø§Ù„Ù€ Inspector
        if (slotIcons != null)
        {
            if (slotButtons == null || slotButtons.Length == 0)
            {
                slotButtons = new Button[slotIcons.Length];
            }

            for (int i = 0; i < slotIcons.Length; i++)
            {
                int index = i;
                if (slotIcons[i] != null)
                {
                    // Ø¬Ø¹Ù„ Ø§Ù„Ø£ÙŠÙ‚ÙˆÙ†Ø© Ù†ÙØ³Ù‡Ø§ Ù‚Ø§Ø¨Ù„Ø© Ù„Ù„Ø¶ØºØ· ÙƒÙ€ Raycast Target
                    slotIcons[i].raycastTarget = true;

                    // Ø§Ù„Ø¨Ø­Ø« Ø¹Ù† Button Ø¹Ù„Ù‰ Ù†ÙØ³ Ø§Ù„ÙƒØ§Ø¦Ù† Ø£Ùˆ Ø§Ù„ÙƒØ§Ø¦Ù† Ø§Ù„Ø£Ø¨
                    Button btn = slotIcons[i].GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = slotIcons[i].GetComponentInParent<Button>();
                    }
                    if (btn == null)
                    {
                        btn = slotIcons[i].gameObject.AddComponent<Button>();
                    }

                    if (i < slotButtons.Length)
                    {
                        slotButtons[i] = btn;
                    }

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSlotClicked(index));
                }
            }
        }

        ForceRefreshUI();

        // إذا كان لدى اللاعب أدوات محفوظة في حقيبته (مثلاً بعد الضغط على BACK ثم العودة للمرحلة)، نظهر شريط الأدوات مباشرة حتى لا تختفي الأيقونات
        if (inventoryItems.Count > 0)
        {
            ShowInventoryTemporarily();
        }
    }

    private void BindInventoryToggleButton()
    {
        GameObject bagObject = GameObject.Find("Inventory_Toggle_Button") ?? GameObject.Find("BagButton");
        if (bagObject == null) return;

        // وضع زر الحقيبة في أعلى طبقة بالكانفاس
        bagObject.transform.SetAsLastSibling();

        Image img = bagObject.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }

        Button bagButton = bagObject.GetComponent<Button>();
        if (bagButton == null)
        {
            bagButton = bagObject.AddComponent<Button>();
        }

        bagButton.interactable = true;
        bagButton.onClick.RemoveAllListeners();
        bagButton.onClick.AddListener(ToggleInventory);
    }

    public static Sprite GetBookSprite()
    {
        Sprite bIcon = Resources.Load<Sprite>("book_icon") 
                    ?? Resources.Load<Sprite>("magic_book_clean") 
                    ?? Resources.Load<Sprite>("SecretBook");
        if (bIcon == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("magic_book_clean") 
                         ?? Resources.Load<Texture2D>("book_icon") 
                         ?? Resources.Load<Texture2D>("SecretBook");
            if (tex != null)
            {
                bIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return bIcon;
    }

    public static Sprite GetKeySprite()
    {
        Sprite kIcon = Resources.Load<Sprite>("key_icon") 
                    ?? Resources.Load<Sprite>("key");
        if (kIcon == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("key_icon") ?? Resources.Load<Texture2D>("key");
            if (tex != null)
            {
                kIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return kIcon;
    }

    public static Sprite CreateProceduralNoteSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color paperColor = new Color(0.92f, 0.88f, 0.78f, 1f); // لون ورق قديم
        Color borderColor = new Color(0.30f, 0.22f, 0.15f, 1f); // إطار بني داكن
        Color lineCodeColor = new Color(0.2f, 0.35f, 0.55f, 1f); // خطوط الكود الحبرية

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x < 6 || x > size - 7 || y < 6 || y > size - 7)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }
                if (x == 6 || x == size - 7 || y == 6 || y == size - 7)
                {
                    texture.SetPixel(x, y, borderColor);
                    continue;
                }
                if (x > size - 14 && y > size - 14)
                {
                    texture.SetPixel(x, y, (x + y > size + size - 20) ? Color.clear : borderColor);
                    continue;
                }
                if ((y == 40 || y == 30 || y == 20) && x > 12 && x < size - 14)
                {
                    texture.SetPixel(x, y, lineCodeColor);
                }
                else
                {
                    texture.SetPixel(x, y, paperColor);
                }
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite GetNoteSprite()
    {
        Sprite nIcon = Resources.Load<Sprite>("paper_note") 
                    ?? Resources.Load<Sprite>("note_icon") 
                    ?? Resources.Load<Sprite>("SecretCode");
        if (nIcon != null) return nIcon;

        Texture2D tex = Resources.Load<Texture2D>("paper_note") 
                     ?? Resources.Load<Texture2D>("note_icon") 
                     ?? Resources.Load<Texture2D>("SecretCode");
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        return CreateProceduralNoteSprite();
    }

    public static Sprite GetFlintSprite()
    {
        Sprite fIcon = Resources.Load<Sprite>("flint_icon") ?? Resources.Load<Sprite>("lighter_icon");
        if (fIcon != null) return fIcon;

        Texture2D tex = Resources.Load<Texture2D>("flint_icon") ?? Resources.Load<Texture2D>("lighter_icon");
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    public static Sprite GetMemoryPaintingSprite()
    {
        Sprite s = Resources.Load<Sprite>("memory_painting_time_icon") 
                ?? Resources.Load<Sprite>("memory_painting");
        if (s != null) return s;

        Texture2D tex = Resources.Load<Texture2D>("memory_painting_time_icon")
                     ?? Resources.Load<Texture2D>("memory_painting");
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        string p = System.IO.Path.Combine(Application.dataPath, "_Project/Resources/memory_painting_time_icon.png");
        if (System.IO.File.Exists(p))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(p);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            diskTex.LoadImage(bytes);
            return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
        }

        return CreateProceduralNoteSprite();
    }

    public static Sprite GetClockTimeSprite()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Vector2 center = new Vector2(128f, 52f); // Pivot mount hole near base

        // 1. Base Mounting Ring (Golden gear collar)
        Color goldDark = new Color(0.60f, 0.45f, 0.15f, 1f);
        Color goldMid = new Color(0.85f, 0.68f, 0.25f, 1f);
        Color goldLight = new Color(1.0f, 0.90f, 0.50f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dCenter = Vector2.Distance(new Vector2(x, y), center);
                if (dCenter <= 28f && dCenter >= 10f)
                {
                    pixels[y * size + x] = (x > 128) ? goldLight : goldMid;
                }
                else if (dCenter < 10f)
                {
                    // Center mounting hole
                    pixels[y * size + x] = new Color(0.12f, 0.10f, 0.08f, 0.9f);
                }
            }
        }

        // 2. Ornate vintage antique pointer body (tapers up towards tip at y=230)
        for (int y = 52; y <= 236; y++)
        {
            float progress = (y - 52f) / (236f - 52f);
            float halfWidth;

            if (progress < 0.25f)
            {
                halfWidth = Mathf.Lerp(16f, 9f, progress / 0.25f);
            }
            else if (progress >= 0.25f && progress < 0.55f)
            {
                // Decorative diamond / spade bulge
                float tDiamond = (progress - 0.25f) / 0.30f;
                halfWidth = 9f + Mathf.Sin(tDiamond * Mathf.PI) * 16f;
            }
            else
            {
                // Taper to sharp spear tip
                halfWidth = Mathf.Lerp(9f, 1f, (progress - 0.55f) / 0.45f);
            }

            int minX = Mathf.RoundToInt(128f - halfWidth);
            int maxX = Mathf.RoundToInt(128f + halfWidth);

            for (int x = minX; x <= maxX; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    if (x == minX || x == maxX)
                        pixels[y * size + x] = goldDark;
                    else if (x >= 128)
                        pixels[y * size + x] = goldLight;
                    else
                        pixels[y * size + x] = goldMid;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite GetPotionSprite()
    {
        // 1. البحث عن الأيقونة الأصلية من الكائن الموجود في المشهد (حتى لو كان غير مفعل)
        AlchemyCauldron[] cauldrons = FindObjectsByType<AlchemyCauldron>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in cauldrons)
        {
            if (c != null && c.ResultItemIcon != null) return c.ResultItemIcon;
        }

        // 2. البحث في Resources
        Sprite resSprite = Resources.Load<Sprite>("potion");
        if (resSprite != null) return resSprite;

        Texture2D resTex = Resources.Load<Texture2D>("potion");
        if (resTex != null)
        {
            return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
        }

        // 3. البحث في الذاكرة
        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in loadedSprites)
        {
            if (s != null && s.name.StartsWith("potion", System.StringComparison.OrdinalIgnoreCase))
                return s;
        }

        // 4. البحث في القرص (في المحرر)
        string p = System.IO.Path.Combine(Application.dataPath, "_Project/potion.png");
        if (System.IO.File.Exists(p))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(p);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            diskTex.LoadImage(bytes);
            return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    public static Sprite GetBottleSprite()
    {
        // 1. البحث عن الأيقونة الأصلية من الكائن الموجود في المشهد (حتى لو كان غير مفعل)
        CollectibleItem[] items = FindObjectsByType<CollectibleItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item != null && item.ItemIcon != null &&
                (item.ItemID.IndexOf("flask", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 item.ItemID.IndexOf("bottle", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return item.ItemIcon;
            }
        }

        // 2. البحث في Resources
        Sprite resSprite = Resources.Load<Sprite>("bottle");
        if (resSprite != null) return resSprite;

        Texture2D resTex = Resources.Load<Texture2D>("bottle");
        if (resTex != null)
        {
            return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
        }

        // 3. البحث في الذاكرة
        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in loadedSprites)
        {
            if (s != null && s.name.StartsWith("bottle", System.StringComparison.OrdinalIgnoreCase))
                return s;
        }

        // 4. البحث في القرص (في المحرر)
        string p = System.IO.Path.Combine(Application.dataPath, "_Project/bottle.png");
        if (System.IO.File.Exists(p))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(p);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            diskTex.LoadImage(bytes);
            return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    public static Sprite GetMortarSprite()
    {
        // 1. البحث عن الأيقونة الأصلية من الكائن الموجود في المشهد (حتى لو كان غير مفعل)
        CollectibleItem[] items = FindObjectsByType<CollectibleItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item != null && item.ItemIcon != null &&
                (item.ItemID.IndexOf("herb", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 item.ItemID.IndexOf("mortar", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return item.ItemIcon;
            }
        }

        // 2. البحث في Resources
        Sprite resSprite = Resources.Load<Sprite>("mortar");
        if (resSprite != null) return resSprite;

        Texture2D resTex = Resources.Load<Texture2D>("mortar");
        if (resTex != null)
        {
            return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
        }

        // 3. البحث في الذاكرة
        Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in loadedSprites)
        {
            if (s != null && s.name.StartsWith("mortar", System.StringComparison.OrdinalIgnoreCase))
                return s;
        }

        // 4. البحث في القرص (في المحرر)
        string p = System.IO.Path.Combine(Application.dataPath, "_Project/mortar.png");
        if (System.IO.File.Exists(p))
        {
            byte[] bytes = System.IO.File.ReadAllBytes(p);
            Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            diskTex.LoadImage(bytes);
            return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
        }
        return null;
    }

    public static Sprite ResolveItemIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        string lower = itemId.ToLower();

        if (lower.Contains("acidpotion") || lower.Contains("potion"))
        {
            return GetPotionSprite();
        }
        else if (lower.Contains("waterflask") || lower.Contains("bottle") || lower.Contains("flask"))
        {
            return GetBottleSprite();
        }
        else if (lower.Contains("herb") || lower.Contains("mortar"))
        {
            return GetMortarSprite();
        }
        else if (lower.Contains("book"))
        {
            return GetBookSprite();
        }
        else if (lower.Contains("key"))
        {
            return GetKeySprite();
        }
        else if (lower.Contains("code") || lower.Contains("note") || lower.Contains("paper"))
        {
            return GetNoteSprite();
        }
        else if (lower.Contains("flint") || lower.Contains("lighter"))
        {
            return GetFlintSprite();
        }
        else if (lower.Contains("clockhand") || lower.Contains("hourhand") || lower.Contains("hand") || lower.Contains("clock"))
        {
            return GetClockTimeSprite();
        }
        else if (lower.Contains("painting") || lower.Contains("memory") || lower.Contains("tablo") || lower.Contains("time"))
        {
            return GetMemoryPaintingSprite();
        }

        Texture2D fallbackTex = new Texture2D(64, 64);
        Color[] cols = new Color[64 * 64];
        for (int ci = 0; ci < cols.Length; ci++) cols[ci] = Color.clear;
        fallbackTex.SetPixels(cols);
        fallbackTex.Apply();
        return Sprite.Create(fallbackTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    private void ValidateAndRefreshIcons()
    {
        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(inventoryItems[i].id))
            {
                inventoryItems.RemoveAt(i);
                continue;
            }

            if (inventoryItems[i].icon == null)
            {
                var entry = inventoryItems[i];
                entry.icon = ResolveItemIcon(entry.id);
                inventoryItems[i] = entry;
            }
        }
        persistentItems = new List<InventoryEntry>(inventoryItems);
    }

    public void SaveInventoryToDisk()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        List<string> savedIds = new List<string>();
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (!string.IsNullOrEmpty(inventoryItems[i].id))
            {
                savedIds.Add(inventoryItems[i].id);
            }
        }
        PlayerPrefs.SetString(sceneName + "_SavedInventory_ItemIDs", string.Join(";", savedIds));
        PlayerPrefs.Save();
    }

    private void LoadInventoryFromDisk()
    {
        inventoryItems.Clear();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 1. استرجاع الأدوات من السلسلة المحفوظة بالقرص للغرفة الحالية فقط
        string raw = PlayerPrefs.GetString(sceneName + "_SavedInventory_ItemIDs", "");
        if (!string.IsNullOrEmpty(raw))
        {
            string[] ids = raw.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var id in ids)
            {
                if (HasItem(id)) continue;
                Sprite icon = ResolveItemIcon(id);
                inventoryItems.Add(new InventoryEntry { id = id, icon = icon });
            }
        }

        // 2. التحقق من المهام المنجزة وضمان بقاء كل أداة جناها اللاعب داخل حقيبته بحسب الغرفة النشطة فقط
        if (sceneName == "Room1")
        {
            bool dustDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("dust"))
                            || PlayerPrefs.GetInt("Room1_Task_dust", 0) == 1
                            || PlayerPrefs.GetInt("HasItem_SecretBook", 0) == 1;

            if (dustDone && !HasItem("SecretBook") && !HasItem("Book"))
            {
                inventoryItems.Add(new InventoryEntry { id = "SecretBook", icon = GetBookSprite() });
            }

            bool keyDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("key"))
                           || PlayerPrefs.GetInt("Room1_Task_key", 0) == 1
                           || PlayerPrefs.GetInt("HasItem_ChestKey", 0) == 1;

            bool doorDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("door"))
                            || PlayerPrefs.GetInt("Room1_Task_door", 0) == 1
                            || PlayerPrefs.GetInt("Room1_Done", 0) == 1;

            if (keyDone && !doorDone && !HasItem("ChestKey") && !HasItem("key"))
            {
                inventoryItems.Add(new InventoryEntry { id = "ChestKey", icon = GetKeySprite() });
            }

            bool candleDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("candle"))
                              || PlayerPrefs.GetInt("Room1_Task_candle", 0) == 1
                              || PlayerPrefs.GetInt("HasItem_SecretCode", 0) == 1;

            if (candleDone && !HasItem("SecretCode") && !HasItem("code"))
            {
                inventoryItems.Add(new InventoryEntry { id = "SecretCode", icon = GetNoteSprite() });
            }

            if (candleDone)
            {
                inventoryItems.RemoveAll(x => x.id.Equals("Flint", System.StringComparison.OrdinalIgnoreCase) 
                                           || x.id.Equals("flint", System.StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                bool trashDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("trash"))
                                 || PlayerPrefs.GetInt("Room1_Task_trash", 0) == 1;

                if (trashDone && !HasItem("Flint") && !HasItem("flint"))
                {
                    inventoryItems.Add(new InventoryEntry { id = "Flint", icon = GetFlintSprite() });
                }
            }
        }
        else if (sceneName == "Room3")
        {
            // التحقق من لوحة الذكريات للغرفة 3
            bool paintingDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("painting"))
                                || PlayerPrefs.GetInt("Room3_Task_painting", 0) == 1
                                || PlayerPrefs.GetInt("HasItem_MemoryPainting", 0) == 1;

            if (paintingDone && !HasItem("MemoryPainting"))
            {
                inventoryItems.Add(new InventoryEntry { id = "MemoryPainting", icon = GetMemoryPaintingSprite() });
            }

            // التحقق من عقرب الساعة المحفوظ (إذا تم التقاطه ولم يتم تركيبه بالساعة بعد)
            bool handCollected = PlayerPrefs.GetInt("Room3_HourHandCollected", 0) == 1
                                 || PlayerPrefs.GetInt("HasItem_ClockHand", 0) == 1;

            bool handInstalled = PlayerPrefs.GetInt("Room3_HandInstalled", 0) == 1
                                 || PlayerPrefs.GetInt("Room3_ClockSolved", 0) == 1;

            if (handCollected && !handInstalled && !HasItem("ClockHand") && !HasItem("HourHand"))
            {
                inventoryItems.Add(new InventoryEntry { id = "ClockHand", icon = GetClockTimeSprite() });
            }

            // التحقق من المفتاح الذهبي للغرفة 3 (إذا تم التقاطه ولم يُفتح الباب بعد)
            bool keyCollected = PlayerPrefs.GetInt("Room3_Task_key", 0) == 1
                                || PlayerPrefs.GetInt("HasItem_GoldenKey", 0) == 1
                                || PlayerPrefs.GetInt("HasItem_Room3Key", 0) == 1;

            bool doorOpened = PlayerPrefs.GetInt("Room3_Done", 0) == 1
                              || PlayerPrefs.GetInt("Level3_Done", 0) == 1
                              || PlayerPrefs.GetInt("Room3_Task_door", 0) == 1;

            bool hasAnyKey = inventoryItems.Exists(x => x.id.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (keyCollected && !doorOpened && !hasAnyKey)
            {
                inventoryItems.Add(new InventoryEntry { id = "GoldenKey", icon = GetKeySprite() });
            }

            // إزالة أي مفتاح مكرر إذا وُجد أكثر من مفتاح واحد في الحقيبة
            int keyCount = 0;
            for (int k = inventoryItems.Count - 1; k >= 0; k--)
            {
                if (inventoryItems[k].id.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    keyCount++;
                    if (keyCount > 1)
                    {
                        inventoryItems.RemoveAt(k);
                    }
                }
            }
        }
        else if (sceneName == "Room2")
        {
            bool herbCollected = PlayerPrefs.GetInt("Room2_Task_herb", 0) == 1
                                 || PlayerPrefs.GetInt("HasItem_Herb", 0) == 1;

            bool flaskCollected = PlayerPrefs.GetInt("Room2_Task_flask", 0) == 1
                                  || PlayerPrefs.GetInt("HasItem_WaterFlask", 0) == 1;

            bool alchemyDone = PlayerPrefs.GetInt("Room2_Task_alchemy", 0) == 1
                               || PlayerPrefs.GetInt("HasItem_AcidPotion", 0) == 1;

            bool plantWatered = PlayerPrefs.GetInt("Room2_Task_water", 0) == 1;

            bool keyCollected = PlayerPrefs.GetInt("Room2_Task_key", 0) == 1
                                || PlayerPrefs.GetInt("HasItem_ExitKey", 0) == 1;

            bool roofDone = PlayerPrefs.GetInt("Room2_Task_roof", 0) == 1
                            || PlayerPrefs.GetInt("Room2_Done", 0) == 1;

            if (herbCollected && !alchemyDone && !HasItem("Herb"))
            {
                inventoryItems.Add(new InventoryEntry { id = "Herb", icon = GetMortarSprite() });
            }

            if (flaskCollected && !alchemyDone && !HasItem("WaterFlask"))
            {
                inventoryItems.Add(new InventoryEntry { id = "WaterFlask", icon = GetBottleSprite() });
            }

            if (alchemyDone && !plantWatered && !HasItem("AcidPotion"))
            {
                inventoryItems.Add(new InventoryEntry { id = "AcidPotion", icon = GetPotionSprite() });
            }

            if (keyCollected && !roofDone && !HasItem("ExitKey") && !HasItem("key"))
            {
                inventoryItems.Add(new InventoryEntry { id = "ExitKey", icon = GetKeySprite() });
            }
        }

        ValidateAndRefreshIcons();
        SaveInventoryToDisk();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaleProgressOnLaunch()
    {
    }

    public static void ResetInventoryData()
    {
        persistentItems.Clear();
        PlayerPrefs.DeleteKey("SavedInventory_ItemIDs");
        PlayerPrefs.DeleteKey("Room1_SavedInventory_ItemIDs");
        PlayerPrefs.DeleteKey("Room2_SavedInventory_ItemIDs");
        PlayerPrefs.DeleteKey("Room3_SavedInventory_ItemIDs");
        PlayerPrefs.DeleteKey("HasItem_SecretBook");
        PlayerPrefs.DeleteKey("HasItem_SecretCode");
        PlayerPrefs.DeleteKey("HasItem_ChestKey");
        PlayerPrefs.DeleteKey("HasItem_MemoryPainting");
        PlayerPrefs.DeleteKey("HasItem_ClockHand");
        PlayerPrefs.DeleteKey("HasItem_Herb");
        PlayerPrefs.DeleteKey("HasItem_WaterFlask");
        PlayerPrefs.DeleteKey("HasItem_AcidPotion");
        PlayerPrefs.DeleteKey("HasItem_ExitKey");
        PlayerPrefs.Save();
        if (Instance != null)
        {
            Instance.inventoryItems.Clear();
            Instance.SelectedItem = "";
            Instance.ForceRefreshUI();
        }
    }

    public bool HasItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        bool exists = inventoryItems.Exists(x => x.id.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));
        if (!exists && itemName.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            exists = inventoryItems.Exists(x => x.id.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
        return exists;
    }

    public void ClearSelection()
    {
        SelectedItem = "";
    }

    public void ToggleInventory()
    {
        // Prevent double execution in the exact same frame (e.g. from both Unity Inspector onClick and runtime AddListener)
        if (Time.frameCount == lastToggleFrame) return;
        lastToggleFrame = Time.frameCount;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        isManuallyOpen = !isManuallyOpen;
        if (isManuallyOpen)
        {
            ValidateAndRefreshIcons();
            ForceRefreshUI();
        }
        SetInventoryVisible(isManuallyOpen);
        ProceduralAudioGenerator.PlaySound("ui_click", 0.65f);
    }

    public void SetInventoryVisible(bool visible)
    {
        if (inventoryCanvasGroup == null)
        {
            inventoryCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.gameObject.SetActive(true);
            inventoryCanvasGroup.alpha = visible ? 1f : 0f;
            inventoryCanvasGroup.blocksRaycasts = visible;
            inventoryCanvasGroup.interactable = visible;
            if (visible)
            {
                inventoryCanvasGroup.transform.SetAsLastSibling();

                // If parent has a Canvas, bring it above other UI or ensure it renders on top
                Canvas parentCanvas = inventoryCanvasGroup.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.sortingOrder < 1000)
                {
                    parentCanvas.sortingOrder = 1000;
                }
            }
        }
        else
        {
            // Direct gameObject fallback
            gameObject.SetActive(visible);
            if (visible) transform.SetAsLastSibling();
        }
    }

    public bool AddItem(string itemName, Sprite itemIcon, bool playFeedback = true)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        if (itemIcon == null)
        {
            itemIcon = ResolveItemIcon(itemName);
        }

        if (HasItem(itemName)) return false;

        // في حال كان العنصر مفتاحاً وكان اللاعب يمتلك مفتاحاً بالفعل في حقيبته، نمنع تكراره نهائياً
        if (itemName.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (inventoryItems.Exists(x => x.id.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Debug.LogWarning("[Inventory]: يوجد مفتاح بالفعل في الحقيبة، تم منع تكرار إضافة مفتاح ثانٍ: " + itemName);
                return false;
            }
        }

        if (slotIcons != null && inventoryItems.Count >= slotIcons.Length)
        {
            Debug.LogWarning("[Inventory]: الحقيبة ممتلئة!");
            return false;
        }

        inventoryItems.Add(new InventoryEntry { id = itemName, icon = itemIcon });
        persistentItems = new List<InventoryEntry>(inventoryItems);
        SaveInventoryToDisk();
        Debug.Log("<color=green>[Ø§Ù„Ø­Ù‚ÙŠØ¨Ø©]:</color> ØªÙ…Øª Ø¥Ø¶Ø§ÙØ©: " + itemName);

        if (playFeedback)
        {
            ProceduralAudioGenerator.PlaySound("pickup", 0.85f);
            if (UniversalVideoPlayer.Instance != null)
            {
                UniversalVideoPlayer.Instance.TryPlayItemVideo(itemName);
            }
            ShowInventoryTemporarily();
        }

        ForceRefreshUI();
        return true;
    }

    public bool AddItem(string itemName, Sprite itemIcon)
    {
        return AddItem(itemName, itemIcon, true);
    }

    public bool AddItem(Sprite itemIcon)
    {
        return AddItem("Flint", itemIcon, true);
    }
    public void RemoveItem(string itemName)
    {
        int index = inventoryItems.FindIndex(x => x.id.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));
        if (index == -1 && !string.IsNullOrEmpty(itemName) && itemName.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            index = inventoryItems.FindIndex(x => x.id.IndexOf("key", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (index != -1)
        {
            string removedId = inventoryItems[index].id;
            inventoryItems.RemoveAt(index);
            persistentItems = new List<InventoryEntry>(inventoryItems);
            SaveInventoryToDisk();
            if (SelectedItem.Equals(itemName, System.StringComparison.OrdinalIgnoreCase) ||
                SelectedItem.Equals(removedId, System.StringComparison.OrdinalIgnoreCase))
            {
                ClearSelection();
            }

            Debug.Log("<color=red>[الحقيبة]:</color> تم حذف العنصر بنجاح: " + removedId);

            ForceRefreshUI();
            ShowInventoryTemporarily();
        }
        else
        {
            Debug.LogWarning("[Inventory]: لم يتم العثور على العنصر " + itemName + " لحذفه!");
        }
    }

    // الدالة المسؤولة عن منع أي مربع أو خانة بيضاء نهائياً
    private void ForceRefreshUI()
    {
        if (slotIcons == null) return;

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] == null) continue;

            if (i < inventoryItems.Count)
            {
                if (inventoryItems[i].icon == null)
                {
                    var entry = inventoryItems[i];
                    entry.icon = ResolveItemIcon(entry.id);
                    inventoryItems[i] = entry;
                }

                // إذا بقيت الأيقونة فارغة لأي سبب، لا نفعّل الخانة أبداً حتى لا تظهر كمربع أبيض!
                if (inventoryItems[i].icon == null)
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].color = new Color(0, 0, 0, 0);
                    slotIcons[i].gameObject.SetActive(false);
                    if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                    {
                        slotButtons[i].gameObject.SetActive(false);
                    }
                    continue;
                }

                // خانة ممتلئة بعنصر حقيقي
                slotIcons[i].gameObject.SetActive(true);
                slotIcons[i].sprite = inventoryItems[i].icon;
                slotIcons[i].preserveAspect = true;
                
                bool isThisSelected = !string.IsNullOrEmpty(SelectedItem) && 
                    inventoryItems[i].id.Equals(SelectedItem, System.StringComparison.OrdinalIgnoreCase);

                // إبراز وتوهج الأداة المحددة بلون ذهبي واضح
                slotIcons[i].color = isThisSelected ? new Color(1f, 0.95f, 0.5f, 1f) : Color.white;
                slotIcons[i].transform.localScale = isThisSelected ? Vector3.one * 1.15f : Vector3.one;

                Outline outline = slotIcons[i].GetComponent<Outline>();
                if (outline == null) outline = slotIcons[i].gameObject.AddComponent<Outline>();
                outline.enabled = isThisSelected;
                outline.effectColor = new Color(1f, 0.85f, 0.2f, 0.9f);
                outline.effectDistance = new Vector2(3f, -3f);

                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                {
                    slotButtons[i].gameObject.SetActive(true);
                    slotButtons[i].interactable = true;
                    int slotIdx = i;
                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIdx));
                }
            }
            else
            {
                // خانة فارغة: إخفاء الصورة ومسحها تماماً حتى لا تظهر كخانة بيضاء
                slotIcons[i].sprite = null;
                slotIcons[i].color = new Color(0, 0, 0, 0);
                slotIcons[i].gameObject.SetActive(false);

                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                {
                    // إخفاء زر الخانة بالكامل حتى لا يظهر له خلفية بيضاء من الـ Button Image
                    slotButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void ShowInventoryTemporarily()
    {
        if (inventoryCanvasGroup == null) return;
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(ShowAndHideRoutine());
    }

    private IEnumerator ShowAndHideRoutine()
    {
        inventoryCanvasGroup.gameObject.SetActive(true);
        inventoryCanvasGroup.blocksRaycasts = true;
        inventoryCanvasGroup.interactable = true;

        while (inventoryCanvasGroup.alpha < 1f)
        {
            inventoryCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        inventoryCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        if (!isManuallyOpen)
        {
            while (inventoryCanvasGroup.alpha > 0f)
            {
                inventoryCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
            inventoryCanvasGroup.alpha = 0f;
            inventoryCanvasGroup.blocksRaycasts = false;
            inventoryCanvasGroup.interactable = false;
        }

        hideCoroutine = null;
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < inventoryItems.Count)
        {
            // إذا كان العنصر محدداً بالفعل وتم الضغط عليه ثانية يمكن إلغاء التحديد أو إعادة تحديده
            if (SelectedItem.Equals(inventoryItems[slotIndex].id, System.StringComparison.OrdinalIgnoreCase))
            {
                // إبقاء التحديد فعالاً
            }
            else
            {
                SelectedItem = inventoryItems[slotIndex].id;
            }

            ProceduralAudioGenerator.PlaySound("ui_click", 0.6f);
            Debug.Log("<color=yellow>[الحقيبة]:</color> تم تحديد الأداة: " + SelectedItem);
            ForceRefreshUI();

            // عند النقر على الأيقونة في الحقيبة يفتح التصميم والمعاينة مباشرة
            if (SelectedItem.Contains("Book") || SelectedItem.Contains("book"))
            {
                OpenInspectBook();
            }
            else if (SelectedItem.Contains("Code") || SelectedItem.Contains("code"))
            {
                OpenNotePreview();
            }
            else if (SelectedItem.Contains("Painting") || SelectedItem.Contains("painting"))
            {
                OpenMemoryPaintingPreview();
            }
        }
        else
        {
            ClearSelection();
            ForceRefreshUI();
        }
    }

    public void OpenInspectBook()
    {
        if (inspectBookPanel != null)
        {
            inspectBookPanel.SetActive(true);
            inspectBookPanel.transform.SetAsLastSibling();

            // تطبيق ديزاين أنيق ومرتب للوحة الكتاب (Ancient Leather & Gold Grimoire Design)
            StyleBookInspectPanel();

            RectTransform rt = inspectBookPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                // إزاحة متناسقة لليمين حتى يظهر بجانب لوحة الصندوق بشكل متكامل
                rt.anchoredPosition = new Vector2(320f, 10f);
            }
        }
    }

    private void StyleBookInspectPanel()
    {
        if (inspectBookPanel == null) return;

        // 1. حجم اللوحة وغلاف كحلي ملكي داكن فخم
        RectTransform rt = inspectBookPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(430, 520);
        }

        Image bg = inspectBookPanel.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);
        }

        Outline outline = inspectBookPanel.GetComponent<Outline>();
        if (outline == null) outline = inspectBookPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.88f, 0.72f, 0.35f, 0.95f); // إطار ذهبي معتق
        outline.effectDistance = new Vector2(3f, -3f);

        // 2. إنشاء / تنسيق صفحة المخطوطة الداخلية (Navy Midnight Inner Page)
        Transform pageT = inspectBookPanel.transform.Find("Grimoire_InnerPage");
        GameObject pageObj = (pageT != null) ? pageT.gameObject : new GameObject("Grimoire_InnerPage", typeof(RectTransform));
        pageObj.transform.SetParent(inspectBookPanel.transform, false);
        pageObj.transform.SetAsFirstSibling();

        RectTransform pRect = pageObj.GetComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero;
        pRect.anchorMax = Vector2.one;
        pRect.offsetMin = new Vector2(16, 18);
        pRect.offsetMax = new Vector2(-16, -18);

        Image pageImg = pageObj.GetComponent<Image>() ?? pageObj.AddComponent<Image>();
        pageImg.color = new Color(0.09f, 0.13f, 0.22f, 0.96f);
        pageImg.raycastTarget = false;

        Outline pageOutline = pageObj.GetComponent<Outline>() ?? pageObj.AddComponent<Outline>();
        pageOutline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.5f);
        pageOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // إخفاء الخلفية المظلمة الشفافة إن وجدت
        Transform darkBacking = inspectBookPanel.transform.Find("InspectBook_Panel");
        if (darkBacking != null) darkBacking.gameObject.SetActive(false);

        // 3. إضافة وتنسيق عنوان فخم للكتاب
        Transform existingHeader = inspectBookPanel.transform.Find("Book_HeaderTitle");
        GameObject headerObj = (existingHeader != null) ? existingHeader.gameObject : new GameObject("Book_HeaderTitle", typeof(RectTransform));
        headerObj.transform.SetParent(inspectBookPanel.transform, false);

        RectTransform hRect = headerObj.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 1f);
        hRect.anchorMax = new Vector2(1f, 1f);
        hRect.pivot = new Vector2(0.5f, 1f);
        hRect.anchoredPosition = new Vector2(0, -28);
        hRect.sizeDelta = new Vector2(380, 42);

        TextMeshProUGUI hTmp = headerObj.GetComponent<TextMeshProUGUI>() ?? headerObj.AddComponent<TextMeshProUGUI>();
        hTmp.text = "ANCIENT TOME";
        hTmp.fontSize = 20;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.characterSpacing = 4f;
        hTmp.alignment = TextAlignmentOptions.Center;
        hTmp.color = new Color(0.96f, 0.82f, 0.45f, 1f);

        // خط فاصل ذهبي تحت العنوان
        Transform dividerT = inspectBookPanel.transform.Find("Book_HeaderDivider");
        GameObject dividerObj = (dividerT != null) ? dividerT.gameObject : new GameObject("Book_HeaderDivider", typeof(RectTransform));
        dividerObj.transform.SetParent(inspectBookPanel.transform, false);
        RectTransform divRect = dividerObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.15f, 1f);
        divRect.anchorMax = new Vector2(0.85f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.anchoredPosition = new Vector2(0, -70);
        divRect.sizeDelta = new Vector2(0, 2f);
        Image divImg = dividerObj.GetComponent<Image>() ?? dividerObj.AddComponent<Image>();
        divImg.color = new Color(0.85f, 0.68f, 0.28f, 0.45f);
        divImg.raycastTarget = false;

        // 4. ترقية وتنسيق نص الكود السري (CODE: 7 3 9)
        Transform codeText = inspectBookPanel.transform.Find("Secret_Code_Text");
        if (codeText != null)
        {
            RectTransform ctRect = codeText.GetComponent<RectTransform>();
            if (ctRect != null)
            {
                ctRect.anchoredPosition = new Vector2(0, -30);
                ctRect.sizeDelta = new Vector2(340, 100);
            }

            TextMeshProUGUI cTmp = codeText.GetComponent<TextMeshProUGUI>();
            if (cTmp != null)
            {
                cTmp.fontSize = 32;
                cTmp.fontStyle = FontStyles.Bold;
                cTmp.characterSpacing = 6;
                cTmp.alignment = TextAlignmentOptions.Center;
                cTmp.color = new Color(1f, 0.92f, 0.65f, 1f); // ذهبي فاتح مشع
            }
        }

        // 5. زر إغلاق دائري عصري فخم (Floating Close Button)
        Transform closeBtn = inspectBookPanel.transform.Find("Close_Button");
        if (closeBtn != null)
        {
            RectTransform cbRect = closeBtn.GetComponent<RectTransform>();
            if (cbRect != null)
            {
                cbRect.anchorMin = new Vector2(1f, 1f);
                cbRect.anchorMax = new Vector2(1f, 1f);
                cbRect.pivot = new Vector2(0.5f, 0.5f);
                cbRect.anchoredPosition = new Vector2(-22, -22);
                cbRect.sizeDelta = new Vector2(34, 34);
            }

            Image cbImg = closeBtn.GetComponent<Image>();
            if (cbImg != null)
            {
                cbImg.color = new Color(0.32f, 0.10f, 0.10f, 0.95f);
                cbImg.raycastTarget = true;
            }

            Outline cbOutline = closeBtn.GetComponent<Outline>();
            if (cbOutline == null) cbOutline = closeBtn.gameObject.AddComponent<Outline>();
            cbOutline.effectColor = new Color(0.95f, 0.40f, 0.40f, 0.9f);
            cbOutline.effectDistance = new Vector2(1.5f, -1.5f);

            TextMeshProUGUI cbText = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cbText != null)
            {
                cbText.text = "X";
                cbText.fontSize = 18;
                cbText.fontStyle = FontStyles.Bold;
                cbText.alignment = TextAlignmentOptions.Center;
                cbText.color = new Color(1f, 0.85f, 0.85f, 1f);
                cbText.raycastTarget = false;
            }

            closeBtn.SetAsLastSibling();
        }
    }

    public void CloseInspectBook()
    {
        if (inspectBookPanel != null) inspectBookPanel.SetActive(false);
    }

    public void OpenNotePreview()
    {
        if (notePreviewPanel != null)
        {
            notePreviewPanel.SetActive(true);
            // إحضار ورقة الكود لأعلى طبقة بالكانفاس حتى تظهر فوق لوحة الصندوق
            notePreviewPanel.transform.SetAsLastSibling();

            RectTransform rt = notePreviewPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                // إزاحة الورقة قليلاً لليمين أو اليسار حتى تظهر بوضوح بجانب قفل الأرقام ولا تحجبها
                rt.anchoredPosition = new Vector2(340f, 20f);
            }

            if (noteTextDisplay != null)
            {
                noteTextDisplay.text = "4729";
            }
            else
            {
                TextMeshProUGUI tmp = notePreviewPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "4729";
                }
            }
        }
    }

    public void CloseNotePreview()
    {
        if (notePreviewPanel != null) notePreviewPanel.SetActive(false);
    }

    private void RepositionBagButton()
    {
        GameObject bagBtn = GameObject.Find("Inventory_Toggle_Button") ?? GameObject.Find("BagButton");
        if (bagBtn != null)
        {
            RectTransform rt = bagBtn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                // الحقيبة بجانب زر الرجوع أعلى اليسار
                rt.anchoredPosition = new Vector2(205f, -42f);
                rt.sizeDelta = new Vector2(65f, 65f);
            }
        }
    }

    private GameObject memoryPaintingPreviewPanel;

    public void OpenMemoryPaintingPreview()
    {
        if (memoryPaintingPreviewPanel == null)
        {
            BuildMemoryPaintingInspectPanel();
        }

        if (memoryPaintingPreviewPanel != null)
        {
            memoryPaintingPreviewPanel.SetActive(true);
            memoryPaintingPreviewPanel.transform.SetAsLastSibling();

            RectTransform rt = memoryPaintingPreviewPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(320f, 10f);
            }
        }
    }

    public void CloseMemoryPaintingPreview()
    {
        if (memoryPaintingPreviewPanel != null)
        {
            memoryPaintingPreviewPanel.SetActive(false);
        }
    }

    private void BuildMemoryPaintingInspectPanel()
    {
        Canvas parentCanvas = inventoryCanvasGroup != null ? inventoryCanvasGroup.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (parentCanvas == null) return;

        memoryPaintingPreviewPanel = new GameObject("MemoryPainting_InspectPanel", typeof(RectTransform));
        memoryPaintingPreviewPanel.transform.SetParent(ResponsiveUI.SafeContent(parentCanvas), false);

        RectTransform rt = memoryPaintingPreviewPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(320f, 10f);
        rt.sizeDelta = new Vector2(440, 560);

        Image bg = memoryPaintingPreviewPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.09f, 0.16f, 0.98f);

        Outline outline = memoryPaintingPreviewPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.92f, 0.78f, 0.35f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Header Title
        GameObject headerObj = new GameObject("Title", typeof(RectTransform));
        headerObj.transform.SetParent(memoryPaintingPreviewPanel.transform, false);
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0, -22);
        hRt.sizeDelta = new Vector2(400, 38);

        TextMeshProUGUI hTmp = headerObj.AddComponent<TextMeshProUGUI>();
        hTmp.text = "MEMORY PAINTING";
        hTmp.fontSize = 20;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.characterSpacing = 3f;
        hTmp.alignment = TextAlignmentOptions.Center;
        hTmp.color = new Color(0.96f, 0.82f, 0.45f, 1f);

        // Painting Preview Image
        GameObject imgObj = new GameObject("PaintingImage", typeof(RectTransform));
        imgObj.transform.SetParent(memoryPaintingPreviewPanel.transform, false);
        RectTransform iRt = imgObj.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0.5f, 1f);
        iRt.anchorMax = new Vector2(0.5f, 1f);
        iRt.pivot = new Vector2(0.5f, 1f);
        iRt.anchoredPosition = new Vector2(0, -68);
        iRt.sizeDelta = new Vector2(230, 230);

        Image pImg = imgObj.AddComponent<Image>();
        pImg.sprite = GetClockTimeSprite();
        pImg.preserveAspect = true;

        Outline pOutline = imgObj.AddComponent<Outline>();
        pOutline.effectColor = new Color(0.85f, 0.70f, 0.30f, 0.85f);
        pOutline.effectDistance = new Vector2(2f, -2f);

        // Revealed Time Display Box
        GameObject timeBoxObj = new GameObject("TimeBox", typeof(RectTransform));
        timeBoxObj.transform.SetParent(memoryPaintingPreviewPanel.transform, false);
        RectTransform tbRt = timeBoxObj.GetComponent<RectTransform>();
        tbRt.anchorMin = new Vector2(0.5f, 0f);
        tbRt.anchorMax = new Vector2(0.5f, 0f);
        tbRt.pivot = new Vector2(0.5f, 0f);
        tbRt.anchoredPosition = new Vector2(0, 85);
        tbRt.sizeDelta = new Vector2(380, 130);

        Image tbBg = timeBoxObj.AddComponent<Image>();
        tbBg.color = new Color(0.09f, 0.13f, 0.22f, 0.95f);

        Outline tbOutline = timeBoxObj.AddComponent<Outline>();
        tbOutline.effectColor = new Color(0.35f, 0.55f, 0.85f, 0.6f);
        tbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject tbTextObj = new GameObject("Text", typeof(RectTransform));
        tbTextObj.transform.SetParent(timeBoxObj.transform, false);
        RectTransform tbtRt = tbTextObj.GetComponent<RectTransform>();
        tbtRt.anchorMin = Vector2.zero;
        tbtRt.anchorMax = Vector2.one;
        tbtRt.offsetMin = new Vector2(10, 8);
        tbtRt.offsetMax = new Vector2(-10, -8);

        TextMeshProUGUI tbTmp = tbTextObj.AddComponent<TextMeshProUGUI>();
        tbTmp.text = "<size=42><color=#38BDF8><b>8:15</b></color></size>";
        tbTmp.fontSize = 24;
        tbTmp.alignment = TextAlignmentOptions.Center;
        tbTmp.color = Color.white;

        // Replay Video Button
        GameObject replayBtnObj = new GameObject("ReplayBtn", typeof(RectTransform));
        replayBtnObj.transform.SetParent(memoryPaintingPreviewPanel.transform, false);
        RectTransform rRt = replayBtnObj.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 0f);
        rRt.anchorMax = new Vector2(0.5f, 0f);
        rRt.pivot = new Vector2(0.5f, 0f);
        rRt.anchoredPosition = new Vector2(0, 22);
        rRt.sizeDelta = new Vector2(260, 48);

        Image rbImg = replayBtnObj.AddComponent<Image>();
        rbImg.color = new Color(0.12f, 0.18f, 0.30f, 0.95f);

        Outline rbOutline = replayBtnObj.AddComponent<Outline>();
        rbOutline.effectColor = new Color(0.95f, 0.80f, 0.35f, 0.85f);
        rbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button rBtn = replayBtnObj.AddComponent<Button>();
        rBtn.onClick.AddListener(() =>
        {
            if (UniversalVideoPlayer.Instance != null)
            {
                UniversalVideoPlayer.Instance.PlayMemoryPaintingVideo();
            }
        });

        GameObject rTextObj = new GameObject("Text", typeof(RectTransform));
        rTextObj.transform.SetParent(replayBtnObj.transform, false);
        RectTransform rtRt = rTextObj.GetComponent<RectTransform>();
        rtRt.anchorMin = Vector2.zero;
        rtRt.anchorMax = Vector2.one;
        rtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI rTmp = rTextObj.AddComponent<TextMeshProUGUI>();
        rTmp.text = "▶ <b>REPLAY MEMORY VIDEO</b>";
        rTmp.fontSize = 15;
        rTmp.alignment = TextAlignmentOptions.Center;
        rTmp.color = new Color(1f, 0.92f, 0.70f, 1f);
        replayBtnObj.SetActive(false);

        // Close Button
        GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform));
        closeObj.transform.SetParent(memoryPaintingPreviewPanel.transform, false);
        RectTransform cRt = closeObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = new Vector2(-22, -22);
        cRt.sizeDelta = new Vector2(34, 34);

        Image cImg = closeObj.AddComponent<Image>();
        cImg.color = new Color(0.32f, 0.10f, 0.10f, 0.95f);

        Outline cOutline = closeObj.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.95f, 0.40f, 0.40f, 0.9f);
        cOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button cBtn = closeObj.AddComponent<Button>();
        cBtn.onClick.AddListener(CloseMemoryPaintingPreview);

        GameObject cTextObj = new GameObject("Text", typeof(RectTransform));
        cTextObj.transform.SetParent(closeObj.transform, false);
        RectTransform ctRt = cTextObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI cTmp = cTextObj.AddComponent<TextMeshProUGUI>();
        cTmp.text = "X";
        cTmp.fontSize = 18;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.color = new Color(1f, 0.85f, 0.85f, 1f);
    }
}
