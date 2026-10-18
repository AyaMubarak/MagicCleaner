using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// الجنية السحرية المرشدة (Magical Fairy Guide & Hint Companion)
/// - تطير وتتأرجح بنعومة في الشاشة.
/// - عند النقر على الهنت، تطير الجنية بخط منحني سحري نحو الكائن المستهدف للمهمة الحالية (التراب، المهملات، الشمعة، الصندوق، المفتاح، الباب).
/// - تدور حول الهدف بحركات متوهجة ثم تعود لمكانها بجانب زر الهنت.
/// </summary>
public class MagicalFairyCompanion : MonoBehaviour
{
    public static MagicalFairyCompanion Instance { get; private set; }

    [Header("Sprite & Visuals")]
    public Sprite fairySprite;
    private RectTransform fairyRect;
    private Image fairyImage;
    private Outline glowOutline;

    private Vector2 idleRestPosition;
    private Coroutine flightCoroutine;
    private bool isFlyingToTarget = false;

    private float idleBobTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// بناء كائن الجنية برمجياً داخل الـ Canvas
    /// </summary>
    public static MagicalFairyCompanion CreateOnCanvas(Canvas canvas, Vector2 restPosition, Sprite customSprite = null)
    {
        GameObject fairyObj = new GameObject("MagicalFairyCompanion");
        fairyObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = fairyObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = restPosition;
        rt.sizeDelta = new Vector2(105, 105);

        Image img = fairyObj.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;
        img.preserveAspect = true;

        if (customSprite != null)
        {
            img.sprite = customSprite;
        }

        MagicalFairyCompanion comp = fairyObj.AddComponent<MagicalFairyCompanion>();
        comp.fairyRect = rt;
        comp.fairyImage = img;
        comp.fairySprite = customSprite;
        comp.idleRestPosition = restPosition;

        // زر تفاعلي عند النقر على الجنية نفسها تفتح الهنت أو تطير
        Button btn = fairyObj.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (LevelProgressGuide.Instance != null)
            {
                LevelProgressGuide.Instance.ToggleHintBubble();
            }
        });

        return comp;
    }

    private void Update()
    {
        // اهتزاز وتأرجح لطيف للجنية (Floating bobbing animation) أثناء وقوفها
        if (!isFlyingToTarget && fairyRect != null)
        {
            idleBobTimer += Time.deltaTime * 3.5f;
            float bobY = Mathf.Sin(idleBobTimer) * 7f;
            float wobbleZ = Mathf.Sin(idleBobTimer * 0.7f) * 6f;

            fairyRect.anchoredPosition = idleRestPosition + new Vector2(0, bobY);
            fairyRect.localRotation = Quaternion.Euler(0, 0, wobbleZ);

            if (glowOutline != null)
            {
                float pulse = 0.5f + Mathf.PingPong(Time.time * 2f, 0.5f);
                glowOutline.effectColor = new Color(0.3f, 0.85f, 1f, pulse);
            }
        }
    }

    /// <summary>
    /// إرسال الجنية للطيران نحو الكائن في العالم ثلاثي الأبعاد
    /// </summary>
    public void FlyToWorldTarget(Vector3 worldTargetPosition)
    {
        if (flightCoroutine != null) StopCoroutine(flightCoroutine);
        flightCoroutine = StartCoroutine(FlyToTargetRoutine(worldTargetPosition));
    }

    private IEnumerator FlyToTargetRoutine(Vector3 worldPos)
    {
        isFlyingToTarget = true;
        Camera cam = Camera.main;
        if (cam == null)
        {
            isFlyingToTarget = false;
            yield break;
        }

        Vector2 startPos = fairyRect.anchoredPosition;
        Canvas parentCanvas = fairyRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : (fairyRect.parent as RectTransform);

        // دالة مساعدة لحساب موضع الهدف على الشاشة بدقة مع حركة الكاميرا
        System.Func<Vector2> getScreenTarget = () =>
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return startPos;

            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            // إذا كان الهدف في زاوية الكاميرا أو خلفها، يظهر في جانب الشاشة لتوجيه اللاعب
            if (sp.z < 0)
            {
                sp.x = (sp.x < Screen.width * 0.5f) ? 50f : Screen.width - 50f;
                sp.y = Screen.height * 0.5f;
            }

            Vector2 localPoint;
            RectTransform parentRect = fairyRect.parent as RectTransform;
            Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? cam : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, sp, uiCamera, out localPoint))
            {
                // إذا كان الـ Anchor في الزاوية العلوية اليمنى (Top-Right):
                // نحول localPoint (المقاسة من منتصف parentRect) إلى إحداثيات الـ Top-Right anchor
                if (fairyRect.anchorMin == Vector2.one && fairyRect.anchorMax == Vector2.one)
                {
                    Vector2 pSize = parentRect.rect.size;
                    return new Vector2(localPoint.x - (pSize.x * 0.5f), localPoint.y - (pSize.y * 0.5f));
                }
                return localPoint;
            }
            return startPos;
        };

        // 1. الطيران المباشر والسريع نحو الهدف
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector2 currentTarget = getScreenTarget();
            Vector2 midPoint = (startPos + currentTarget) * 0.5f + new Vector2(0, 120f);

            Vector2 p0 = Vector2.Lerp(startPos, midPoint, smoothT);
            Vector2 p1 = Vector2.Lerp(midPoint, currentTarget, smoothT);
            fairyRect.anchoredPosition = Vector2.Lerp(p0, p1, smoothT);

            float tilt = Mathf.Sin(t * Mathf.PI * 4f) * 18f;
            fairyRect.localRotation = Quaternion.Euler(0, 0, tilt);

            yield return null;
        }

        // 2. التحويم والدوران التفاعلي حول الهدف (تبقى تدور وتتحرك حول الهدف لمدة 4 ثوانٍ واضحة)
        float hoverTime = 4.0f;
        float hElapsed = 0f;
        while (hElapsed < hoverTime)
        {
            hElapsed += Time.deltaTime;
            Vector2 liveTarget = getScreenTarget();
            float angle = hElapsed * 4.5f;
            Vector2 orbit = new Vector2(Mathf.Cos(angle) * 75f, Mathf.Sin(angle) * 45f);
            fairyRect.anchoredPosition = liveTarget + orbit;

            float wobble = Mathf.Sin(hElapsed * 8f) * 12f;
            fairyRect.localRotation = Quaternion.Euler(0, 0, wobble);

            yield return null;
        }

        // 3. العودة الناعمة لموقعها الأصلي في الزاوية العلوية بجانب زر الهنت
        elapsed = 0f;
        Vector2 returnStart = fairyRect.anchoredPosition;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            fairyRect.anchoredPosition = Vector2.Lerp(returnStart, idleRestPosition, smoothT);
            fairyRect.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 3f) * 10f);

            yield return null;
        }

        fairyRect.anchoredPosition = idleRestPosition;
        fairyRect.localRotation = Quaternion.identity;
        isFlyingToTarget = false;
        flightCoroutine = null;
    }
}
