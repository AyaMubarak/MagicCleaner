using System.Collections;
using UnityEngine;
using TMPro;

public class SecretCodeWall : MonoBehaviour
{
    [Header("Wall Code Settings")]
    [SerializeField] private string secretCode = "4729";
    [SerializeField] private float revealSpeed = 2.5f;
    [SerializeField] private float delayBeforeInventoryCollect = 2f; // الانتظار ثانيتين بعد الظهور الكامل

    [Header("Inventory Item Settings")]
    [Tooltip("اسحبي صورة أيقونة الكود المخصصة هنا")]
    [SerializeField] private Sprite noteIcon;
    [SerializeField] private string itemID = "SecretCode";

    private TextMeshPro codeTextMesh;
    private bool isRevealed = false;
    private bool isCollected = false;

    private void Awake()
    {
        codeTextMesh = GetComponent<TextMeshPro>();

        if (codeTextMesh != null)
        {
            codeTextMesh.text = secretCode;
            // النص شفاف في البداية حتى تشتعل الشمعة
            Color c = codeTextMesh.color;
            c.a = 0f;
            codeTextMesh.color = c;
        }
    }

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool candleDone = (LevelProgressGuide.Instance != null && LevelProgressGuide.Instance.IsTaskDone("candle"))
                          || PlayerPrefs.GetInt($"{sceneName}_Task_candle", 0) == 1
                          || PlayerPrefs.GetInt("HasItem_SecretCode", 0) == 1;

        if (candleDone)
        {
            isRevealed = true;
            isCollected = true;
            
            // يختفي كود الجدار لأنه تم نقله وحفظه داخل الحقيبة
            gameObject.SetActive(false);

            // ضمان بقاء ورقة الكود محفوظة داخل الحقيبة بأيقونتها الصحيحة دائماً
            if (InventoryManager.Instance != null && !InventoryManager.Instance.HasItem(itemID))
            {
                Sprite iconToUse = noteIcon != null ? noteIcon : InventoryManager.GetNoteSprite();
                InventoryManager.Instance.AddItem(itemID, iconToUse, false);
            }
        }
    }

    /// <summary>
    /// يتم استدعاؤها من CandlePuzzle عند إشعال الشمعة
    /// </summary>
    public void RevealCode()
    {
        if (!isRevealed)
        {
            StartCoroutine(FadeInAndCollectRoutine());
        }
    }

    private IEnumerator FadeInAndCollectRoutine()
    {
        isRevealed = true;
        ProceduralAudioGenerator.PlaySound("magic_chime", 0.9f);
        float alpha = 0f;

        // ظهور تدريجي للكود على الجدار
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * revealSpeed;
            if (codeTextMesh != null)
            {
                Color c = codeTextMesh.color;
                c.a = Mathf.Clamp01(alpha);
                codeTextMesh.color = c;
            }
            yield return null;
        }

        Debug.Log("<color=cyan>[الكود السري]:</color> ظهر الكود على الجدار، سينتقل للحقيبة بعد ثانيتين...");

        // الانتظار ثانيتين ليراه اللاعب بوضوح
        yield return new WaitForSeconds(delayBeforeInventoryCollect);

        // نقله للحقيبة
        CollectCodeToInventory();
    }

    private void CollectCodeToInventory()
    {
        if (isCollected) return;
        isCollected = true;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        PlayerPrefs.SetInt($"{sceneName}_Task_candle", 1);
        PlayerPrefs.SetInt("HasItem_SecretCode", 1);
        PlayerPrefs.Save();

        if (InventoryManager.Instance != null)
        {
            Sprite iconToUse = noteIcon;

            // إذا لم توضع صورة في الـ Inspector، يتم توليد أيقونة ورقة ملاحظة مرسومة بدلاً من مربع أبيض
            if (iconToUse == null)
            {
                iconToUse = GenerateCustomNoteSprite();
            }

            InventoryManager.Instance.AddItem(itemID, iconToUse);
            ProceduralAudioGenerator.PlaySound("pickup", 0.8f);
            Debug.Log("<color=green>[الكود السري]:</color> تم حفظ ورقة الكود بأيقونتها المخصصة في الحقيبة!");
        }

        // يختفي كود الجدار فور انتقاله للحقيبة
        gameObject.SetActive(false);
    }

    /// <summary>
    /// توليد أيقونة ورقة مخططة احترافية برمجياً لتجنب ظهور أي مربع أبيض
    /// </summary>
    private Sprite GenerateCustomNoteSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color paperColor = new Color(0.92f, 0.88f, 0.78f, 1f); // لون ورق قديم / بردي
        Color borderColor = new Color(0.25f, 0.2f, 0.15f, 1f); // إطار بني داكن
        Color lineCodeColor = new Color(0.2f, 0.35f, 0.55f, 1f); // خطوط كتابة باللون الحبري

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // هوامش خارجية شفافة
                if (x < 6 || x > size - 7 || y < 6 || y > size - 7)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                // رسم الإطار الخارجي للورقة
                if (x == 6 || x == size - 7 || y == 6 || y == size - 7)
                {
                    texture.SetPixel(x, y, borderColor);
                    continue;
                }

                // زاوية مطوية في أعلى الورقة
                if (x > size - 14 && y > size - 14)
                {
                    texture.SetPixel(x, y, (x + y > size + size - 20) ? Color.clear : borderColor);
                    continue;
                }

                // رسم أسطر الملاحظة الحبرية المشفرة
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
}