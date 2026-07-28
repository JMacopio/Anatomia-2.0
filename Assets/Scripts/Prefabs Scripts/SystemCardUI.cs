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

    public GameObject lockIcon;      // 🔒 icon — shown when no model
    public GameObject comingSoonTag; // "Coming Soon" tag

    public void Setup(AnatomySystemData data, System.Action<AnatomySystemData> callback, bool isAvailable = true)
    {
        systemData = data;
        onSelectCallback = callback;

        systemNameText.text = data.systemName;
        structureCountText.text = $"{data.structureCount} structures";
        progressBar.value = data.progress;
        progressText.text = $"{Mathf.RoundToInt(data.progress * 100)}%";

        // Apply theme color to icon background and progress bar fill
        iconBackground.color = data.themeColor;
        //ColorBlock cb = progressBar.colors;
        progressBar.fillRect.GetComponent<Image>().color = data.themeColor;

        // ── Show lock if not available ────────────────────────
        if (lockIcon != null) lockIcon.SetActive(!isAvailable);
        if (comingSoonTag != null) comingSoonTag.SetActive(!isAvailable);

        // Dim the card if no model available
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = isAvailable ? 1f : 0.5f;

        cardButton.onClick.AddListener(() => onSelectCallback?.Invoke(systemData));
    }
}
