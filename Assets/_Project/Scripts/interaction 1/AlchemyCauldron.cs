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
        Collider col = GetComponent<Collider>();
        if (col != null && col is MeshCollider meshCol)
        {
            meshCol.convex = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void Update()
    {
        // استقبال اللمس أو النقر بالماوس
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTouchRay();
        }
    }

    private void ProcessTouchRay()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
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