using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AlchemyCauldron : MonoBehaviour
{
    [Header("Recipe Settings (بالترتيب)")]
    [SerializeField] private List<string> orderedIngredients = new List<string> { "WaterFlask", "Herb" };

    [Header("Result Item")]
    [SerializeField] private string resultItemID = "AcidPotion";
    [SerializeField] private Sprite resultItemIcon;

    [Header("Liquid Visuals (مجسم السائل وتغيير الألوان)")]
    [Tooltip("اسحبي هنا مجسم السائل الداخلي Ingredient04")]
    [SerializeField] private GameObject liquidObject;

    [Tooltip("لون الماء الأزرق")]
    [SerializeField] private Color waterBlueColor = new Color(0.1f, 0.5f, 1f, 1f);

    [Tooltip("لون السائل الأخضر النهائي")]
    [SerializeField] private Color herbGreenColor = new Color(0.1f, 0.9f, 0.2f, 1f);

    [Header("Visual & Audio Feedback (اختياري)")]
    [SerializeField] private ParticleSystem craftParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip addIngredientSound;
    [SerializeField] private AudioClip successSound;

    [Header("Interaction Distance")]
    [SerializeField] private float maxInteractionDistance = 5f;

    private int currentIngredientIndex = 0;
    private bool isPotionReadyToCollect = false;
    private Renderer liquidRenderer;

    public Sprite ResultItemIcon => resultItemIcon;

    private void Awake()
    {
        EnsureGenerousHitCollider();
    }

    private void EnsureGenerousHitCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            if (c is MeshCollider mc)
            {
                if (mc.sharedMesh == null) mc.enabled = false;
                else mc.convex = true;
            }
        }

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();
        box.enabled = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds worldBounds = new Bounds(transform.position, Vector3.one * 0.65f);
        bool hasRenderer = false;
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            if (!hasRenderer)
            {
                worldBounds = r.bounds;
                hasRenderer = true;
            }
            else
            {
                worldBounds.Encapsulate(r.bounds);
            }
        }

        Vector3 minWorldSize = new Vector3(0.75f, 0.75f, 0.75f);
        Vector3 targetWorldSize = Vector3.Max(worldBounds.size * 1.35f, minWorldSize);
        Vector3 lossy = transform.lossyScale;
        float sx = Mathf.Max(Mathf.Abs(lossy.x), 0.001f);
        float sy = Mathf.Max(Mathf.Abs(lossy.y), 0.001f);
        float sz = Mathf.Max(Mathf.Abs(lossy.z), 0.001f);

        box.center = transform.InverseTransformPoint(worldBounds.center);
        box.size = new Vector3(targetWorldSize.x / sx, targetWorldSize.y / sy, targetWorldSize.z / sz);
    }

    private void Start()
    {
        bool alchemyAlreadyDone = PlayerPrefs.GetInt("Room2_Task_alchemy", 0) == 1 ||
                                  PlayerPrefs.GetInt("HasItem_AcidPotion", 0) == 1 ||
                                  (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(resultItemID)) ||
                                  PlayerPrefs.GetInt("Room2_Task_water", 0) == 1;

        if (alchemyAlreadyDone)
        {
            gameObject.SetActive(false);
            return;
        }

        // إخفاء السائل في البداية ليكون الوعاء فارغاً تماماً
        if (liquidObject != null)
        {
            liquidRenderer = liquidObject.GetComponent<Renderer>();
            liquidObject.SetActive(false);
        }

        // ضبط الكولايدر والفيزياء
        EnsureGenerousHitCollider();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnMouseDown()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            float effectiveDist = Mathf.Max(maxInteractionDistance, 12f);
            if (Vector3.Distance(cam.transform.position, transform.position) <= effectiveDist)
            {
                HandleCauldronInteraction();
            }
        }
    }

    private void Update()
    {
        // استقبال اللمس أو النقر بالماوس
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTouchRay(Input.mousePosition);
        }
        else if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                ProcessTouchRay(t.position);
            }
        }
    }

    private void ProcessTouchRay(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        float effectiveDist = Mathf.Max(maxInteractionDistance, 12f);
        Ray ray = cam.ScreenPointToRay(screenPos);

        RaycastHit[] hits = Physics.RaycastAll(ray, effectiveDist);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<AlchemyCauldron>() == this)
            {
                HandleCauldronInteraction();
                return;
            }
        }

        RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.25f, effectiveDist);
        foreach (var hit in sphereHits)
        {
            if (hit.collider == null) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<AlchemyCauldron>() == this)
            {
                HandleCauldronInteraction();
                return;
            }
        }
    }

    private void HandleCauldronInteraction()
    {
        // إذا كانت الخلطة جاهزة باللون الأخضر، فالنقرة التالية هي لالتقاطها
        if (isPotionReadyToCollect)
        {
            CollectFinishedPotion();
        }
        else
        {
            TryAddCurrentItem();
        }
    }

    public void TryAddCurrentItem()
    {
        if (InventoryManager.Instance == null) return;

        string selected = InventoryManager.Instance.SelectedItem;

        if (string.IsNullOrEmpty(selected))
        {
            Debug.Log("<color=yellow>[الوعاء]:</color> حددي المكون من الحقيبة أولاً، ثم اضغطي على الوعاء.");
            return;
        }

        if (currentIngredientIndex >= orderedIngredients.Count) return;

        string expectedIngredient = orderedIngredients[currentIngredientIndex];

        if (selected.Equals(expectedIngredient, System.StringComparison.OrdinalIgnoreCase))
        {
            // استهلاك العنصر من الحقيبة
            InventoryManager.Instance.RemoveItem(selected);

            if (audioSource && addIngredientSound)
            {
                audioSource.PlayOneShot(addIngredientSound);
            }

            // تطبيق تغيير اللون
            ApplyStepVisuals(currentIngredientIndex);

            currentIngredientIndex++;

            // عند اكتمال إضافة المكونين (الماء والعشبة)
            if (currentIngredientIndex >= orderedIngredients.Count)
            {
                OnRecipeCompleted();
            }
        }
        else
        {
            if (orderedIngredients.Exists(x => x.Equals(selected, System.StringComparison.OrdinalIgnoreCase)))
            {
                Debug.LogWarning($"<color=orange>[الوعاء]:</color> الترتيب غير صحيح! ضعي ({expectedIngredient}) أولاً.");
            }
            else
            {
                Debug.LogWarning($"<color=red>[الوعاء]:</color> هذا العنصر لا يصلح لهذه الوصفة.");
            }
        }
    }

    private void ApplyStepVisuals(int step)
    {
        if (liquidObject == null) return;

        liquidObject.SetActive(true);

        if (liquidRenderer != null)
        {
            if (step == 0)
            {
                SetLiquidColor(waterBlueColor);
                Debug.Log("<color=cyan>[الوعاء]:</color> تمت إضافة الماء وتحول السائل للأزرق!");
            }
            else if (step == 1)
            {
                SetLiquidColor(herbGreenColor);
                Debug.Log("<color=green>[الوعاء]:</color> تمت إضافة العشبة وتحول السائل للأخضر!");
            }
        }
    }

    private void SetLiquidColor(Color color)
    {
        if (liquidRenderer == null) return;

        if (liquidRenderer.material.HasProperty("_BaseColor"))
        {
            liquidRenderer.material.SetColor("_BaseColor", color);
        }
        else if (liquidRenderer.material.HasProperty("_Color"))
        {
            liquidRenderer.material.SetColor("_Color", color);
        }
    }

    private void OnRecipeCompleted()
    {
        isPotionReadyToCollect = true;

        if (craftParticles != null) craftParticles.Play();
        if (audioSource && successSound) audioSource.PlayOneShot(successSound);

        Debug.Log("<color=green>[الوعاء]:</color> الخلطة اكتملت! تشغيل فيديو خلط العشبة والماء...");

        // تشغيل فيديو خلط العشبة والماء في الصحن فور اكتمال الخلط
        if (UniversalVideoPlayer.Instance != null)
        {
            UniversalVideoPlayer.Instance.PlayHerbVideo();
        }
        else
        {
            var uvp = FindAnyObjectByType<UniversalVideoPlayer>();
            if (uvp != null) uvp.PlayHerbVideo();
        }
    }

    private void CollectFinishedPotion()
    {
        if (resultItemIcon == null)
        {
            Debug.LogError("[الوعاء]: ضعي صورة الجرعة في خانة Result Item Icon حتى تقبلها الحقيبة!");
            return;
        }

        // إضافة الجرعة إلى الحقيبة
        bool added = InventoryManager.Instance.AddItem(resultItemID, resultItemIcon);
        if (added)
        {
            Debug.Log("<color=green>[الوعاء]:</color> تم أخذ الجرعة وتخزينها في الحقيبة بنجاح!");
            PlayerPrefs.SetInt("HasItem_AcidPotion", 1);
            PlayerPrefs.Save();
            LevelProgressGuide.ReportTask("alchemy");
            // إخفاء الوعاء فوراً من الطاولة
            gameObject.SetActive(false);
        }
    }
}