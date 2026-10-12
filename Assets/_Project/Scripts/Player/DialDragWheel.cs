using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DialDragWheel : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("UI & References")]
    [SerializeField] private int dialIndex = 0;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private ChestDialLock chestLockController;

    [Header("Drag Sensitivity")]
    [SerializeField] private float dragThreshold = 30f;

    private int currentValue = 0;
    private float dragAccumulator = 0f;

    private void Start()
    {
        // قراءة الرقم المكتوب حالياً في الـ UI عند التشغيل إن وجد
        if (numberText != null && int.TryParse(numberText.text.Trim(), out int parsedVal))
        {
            currentValue = parsedVal;
        }
        else
        {
            currentValue = 0;
        }

        UpdateUI();
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragAccumulator += eventData.delta.y;

        if (dragAccumulator >= dragThreshold)
        {
            Increment();
            dragAccumulator = 0f;
        }
        else if (dragAccumulator <= -dragThreshold)
        {
            Decrement();
            dragAccumulator = 0f;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragAccumulator = 0f;
    }

    private void Increment()
    {
        currentValue = (currentValue + 1) % 10;
        UpdateUI();
    }

    private void Decrement()
    {
        currentValue = (currentValue - 1 + 10) % 10;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (numberText != null)
        {
            numberText.text = currentValue.ToString();
        }

        if (chestLockController != null)
        {
            chestLockController.SetDialValue(dialIndex, currentValue);
        }
    }

    public int GetCurrentValue()
    {
        return currentValue;
    }
}