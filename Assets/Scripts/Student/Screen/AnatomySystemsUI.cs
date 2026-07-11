using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnatomySystemsUI : MonoBehaviour
{
    [Header("List")]
    public Transform systemsListParent;
    public GameObject systemCardPrefab;

    [Header("Header")]
    public Button backBtn;
    public TMP_Text titleText;

    // Data — in production load from ScriptableObjects or API
    private List<AnatomySystemData> systems = new List<AnatomySystemData>()
    {
        new AnatomySystemData("Skeletal System",   206, 0.75f, new Color(0.2f, 0.4f, 0.9f),  "bone_icon"),
        new AnatomySystemData("Muscular System",   640, 0.45f, new Color(0.9f, 0.2f, 0.2f),  "heart_icon"),
        new AnatomySystemData("Cardiovascular System", 124, 0.30f, new Color(0.8f, 0.2f, 0.7f), "pulse_icon"),
        new AnatomySystemData("Respiratory System",  42, 0.55f, new Color(0.1f, 0.7f, 0.8f), "wind_icon"),
        new AnatomySystemData("Nervous System",     100, 0.20f, new Color(0.4f, 0.3f, 0.9f), "brain_icon"),
        new AnatomySystemData("Digestive System",    50, 0.10f, new Color(0.2f, 0.7f, 0.3f), "stomach_icon"),
    };

    void Start()
    {
        backBtn?.onClick.AddListener(() => UIManager.Instance.GoBack());
        BuildSystemsList();
    }

    void BuildSystemsList()
    {
        foreach (Transform child in systemsListParent)
            Destroy(child.gameObject);

        foreach (var system in systems)
        {
            var card = Instantiate(systemCardPrefab, systemsListParent);
            var ui = card.GetComponent<SystemCardUI>();
            ui?.Setup(system, OnSystemSelected);
        }
    }

    void OnSystemSelected(AnatomySystemData system)
    {
        // Pass selected system to 3D viewer
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