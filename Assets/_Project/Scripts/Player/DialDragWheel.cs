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
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string savedCodeStr = PlayerPrefs.GetString($"{sceneName}_Chest_EnteredCode", "");

        if (!string.IsNullOrEmpty(savedCodeStr) && dialIndex < savedCodeStr.Length && int.TryParse(savedCodeStr[dialIndex].ToString(), out int savedVal))
        {
            currentValue = savedVal;
        }
        else if (numberText != null && int.TryParse(numberText.text.Trim(), out int parsedVal))
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
        ProceduralAudioGenerator.PlaySound("dial_tick", 0.7f);
        UpdateUI();
    }

    private void Decrement()
    {
        currentValue = (currentValue - 1 + 10) % 10;
        ProceduralAudioGenerator.PlaySound("dial_tick", 0.7f);
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