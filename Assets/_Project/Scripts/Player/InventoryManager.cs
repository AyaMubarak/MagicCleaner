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

    private List<InventoryEntry> inventoryItems = new List<InventoryEntry>();
    public string SelectedItem { get; private set; } = "";

    private Coroutine hideCoroutine;
    private bool isManuallyOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isManuallyOpen = false;
        SetInventoryVisible(false);

        if (inspectBookPanel != null) inspectBookPanel.SetActive(false);
        if (notePreviewPanel != null) notePreviewPanel.SetActive(false);

        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                if (slotButtons[i] != null)
                {
                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
                }
            }
        }

        ForceRefreshUI();
    }

    public bool HasItem(string itemName)
    {
        return inventoryItems.Exists(x => x.id.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));
    }

    public void ClearSelection()
    {
        SelectedItem = "";
    }

    public void ToggleInventory()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        isManuallyOpen = !isManuallyOpen;
        SetInventoryVisible(isManuallyOpen);
    }

    public void SetInventoryVisible(bool visible)
    {
        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.gameObject.SetActive(true);
            inventoryCanvasGroup.alpha = visible ? 1f : 0f;
            inventoryCanvasGroup.blocksRaycasts = visible;
            inventoryCanvasGroup.interactable = visible;
        }
    }

    public bool AddItem(string itemName, Sprite itemIcon)
    {
        if (string.IsNullOrEmpty(itemName) || itemIcon == null)
        {
            Debug.LogWarning("[Inventory]: تم رفض الإضافة لأن الأيقونة أو الاسم مفقود!");
            return false;
        }

        if (HasItem(itemName))
        {
            Debug.LogWarning("[Inventory]: العنصر " + itemName + " موجود بالفعل في الحقيبة!");
            return false;
        }

        if (inventoryItems.Count >= slotIcons.Length)
        {
            Debug.LogWarning("[Inventory]: الحقيبة ممتلئة!");
            return false;
        }

        inventoryItems.Add(new InventoryEntry { id = itemName, icon = itemIcon });
        Debug.Log("<color=green>[الحقيبة]:</color> تمت إضافة: " + itemName);

        ForceRefreshUI();
        ShowInventoryTemporarily();
        return true;
    }

    public bool AddItem(Sprite itemIcon)
    {
        return AddItem("Flint", itemIcon);
    }

    public void RemoveItem(string itemName)
    {
        int index = inventoryItems.FindIndex(x => x.id.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));

        if (index != -1)
        {
            inventoryItems.RemoveAt(index);
            if (SelectedItem.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                ClearSelection();
            }

            Debug.Log("<color=red>[الحقيبة]:</color> تم حذف العنصر بنجاح: " + itemName);

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
                // خانة ممتلئة بعنصر حقيقي
                slotIcons[i].gameObject.SetActive(true);
                slotIcons[i].sprite = inventoryItems[i].icon;
                slotIcons[i].color = Color.white;

                if (slotButtons != null && i < slotButtons.Length && slotButtons[i] != null)
                {
                    slotButtons[i].gameObject.SetActive(true);
                    slotButtons[i].interactable = true;
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
            SelectedItem = inventoryItems[slotIndex].id;
            Debug.Log("<color=yellow>[الحقيبة]:</color> تم تحديد الأداة: " + SelectedItem);

            if (SelectedItem.Contains("Book") || SelectedItem.Contains("book"))
            {
                OpenInspectBook();
            }
            else if (SelectedItem.Contains("Code") || SelectedItem.Contains("code"))
            {
                OpenNotePreview();
            }
        }
        else
        {
            ClearSelection();
        }
    }

    public void OpenInspectBook()
    {
        if (inspectBookPanel != null) inspectBookPanel.SetActive(true);
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
            if (noteTextDisplay != null) noteTextDisplay.text = "4729";
        }
    }

    public void CloseNotePreview()
    {
        if (notePreviewPanel != null) notePreviewPanel.SetActive(false);
    }
}