using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemCardUI : MonoBehaviour
{
    [Header("Card Elements")]
    public Image iconBackground;
    public Image systemIcon;
    public TMP_Text systemNameText;
    public TMP_Text structureCountText;
    public Slider progressBar;
    public TMP_Text progressText;
    public Button cardButton;

    private System.Action<AnatomySystemData> onSelectCallback;
    private AnatomySystemData systemData;

    public void Setup(AnatomySystemData data, System.Action<AnatomySystemData> callback)
    {
        systemData = data;
        onSelectCallback = callback;

        systemNameText.text = data.systemName;
        structureCountText.text = $"{data.structureCount} structures";
        progressBar.value = data.progress;
        progressText.text = $"{Mathf.RoundToInt(data.progress * 100)}%";

        // Apply theme color to icon background and progress bar fill
        iconBackground.color = data.themeColor;
        ColorBlock cb = progressBar.colors;
        progressBar.fillRect.GetComponent<Image>().color = data.themeColor;

        cardButton.onClick.AddListener(() => onSelectCallback?.Invoke(systemData));
    }
}
