using UnityEngine;

public class MenuButtonsGlow : MonoBehaviour
{
    public RectTransform glowImage;

    public void HighlightButton(RectTransform targetButton)
    {
        glowImage.gameObject.SetActive(true);
        glowImage.position = targetButton.position;
        glowImage.sizeDelta = targetButton.sizeDelta;
    }
}