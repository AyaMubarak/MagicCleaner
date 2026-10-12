using UnityEngine;

public enum InteractionType
{
    Examine,   // فحص
    PickUp,    // التقاط
    Activate,  // تفعيل آلية
    Move       // تحريك
}

public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Info")]
    public string objectName = "Object";
    public InteractionType interactionType = InteractionType.Examine;
    [TextArea] public string examineMessage = "شيء غامض يبدو قديماً ومغطى بالسحر...";

    public virtual void OnInteract()
    {
        Debug.Log($"[Interaction]: Clicked on {objectName} - Type: {interactionType}");
    }
}