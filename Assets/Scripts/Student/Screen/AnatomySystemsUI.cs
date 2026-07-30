using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnatomySystemsUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;
    public TMP_Text titleText;

    [Header("Pre-placed System Cards — drag each card here")]
    public SystemCardUI skeletalCard;
    public SystemCardUI muscularCard;
    public SystemCardUI cardiovascularCard;

    // Fixed data for each system
    private AnatomySystemData skeletalData = new AnatomySystemData(
        "Skeletal System", 206, 0.75f,
        new Color(0.2f, 0.4f, 0.9f), "bone_icon");

    private AnatomySystemData muscularData = new AnatomySystemData(
        "Muscular System", 640, 0.45f,
        new Color(0.9f, 0.2f, 0.2f), "heart_icon");

    private AnatomySystemData cardiovascularData = new AnatomySystemData(
        "Cardiovascular System", 124, 0.30f,
        new Color(0.8f, 0.2f, 0.7f), "pulse_icon");

    void Start()
    {
        backBtn?.onClick.AddListener(() => UIManager.Instance.GoBack());

        // Setup each card directly — no spawning
        skeletalCard?.Setup(skeletalData, OnSystemSelected);
        muscularCard?.Setup(muscularData, OnSystemSelected);
        cardiovascularCard?.Setup(cardiovascularData, OnSystemSelected);
    }

    void OnSystemSelected(AnatomySystemData system)
    {
        Model3DViewerUI viewer = UIManager.Instance.model3DPanel
            .GetComponent<Model3DViewerUI>();
        viewer?.LoadSystem(system);
        UIManager.Instance.ShowPanel(UIManager.Instance.model3DPanel);
    }
}

[System.Serializable]
public class AnatomySystemData
{
    public string systemName;
    public int structureCount;
    public float progress; // 0-1
    public Color themeColor;
    public string iconKey;

    public AnatomySystemData(string name, int count, float prog, Color color, string icon)
    {
        systemName = name;
        structureCount = count;
        progress = prog;
        themeColor = color;
        iconKey = icon;
    }
}