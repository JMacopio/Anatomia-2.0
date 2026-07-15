using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddBadgeModalUI : MonoBehaviour
{
    public Button closeBtn;
    public TMP_InputField nameField;
    public TMP_InputField pointsField;
    public Button[] iconButtons;    // 12 icon buttons in a grid
    public Sprite[] iconSprites;    // matching icon sprites
    public string[] iconKeys;       // matching icon key strings
    public Button addBadgeBtn;
    public Button cancelBtn;
    public TMP_Text errorText;

    private string selectedIconKey = "star";
    private int selectedIconIndex = 0;

    void Start()
    {
        closeBtn.onClick.AddListener(Close);
        cancelBtn.onClick.AddListener(Close);
        addBadgeBtn.onClick.AddListener(OnAddBadge);

        for (int i = 0; i < iconButtons.Length; i++)
        {
            int idx = i;
            iconButtons[i].onClick.AddListener(() => SelectIcon(idx));
        }

        if (errorText) errorText.gameObject.SetActive(false);
        SelectIcon(0);
    }

    void OnEnable()
    {
        nameField.text = "";
        pointsField.text = "100";
        if (errorText) errorText.gameObject.SetActive(false);
        SelectIcon(0);
    }

    void SelectIcon(int index)
    {
        selectedIconIndex = index;
        if (iconKeys != null && index < iconKeys.Length)
            selectedIconKey = iconKeys[index];

        // Highlight selected icon button
        for (int i = 0; i < iconButtons.Length; i++)
        {
            var outline = iconButtons[i].GetComponent<Outline>();
            if (outline) outline.enabled = (i == index);

            // Or tint the button
            iconButtons[i].image.color = (i == index)
                ? new Color(0.95f, 0.90f, 1.00f)
                : Color.white;
        }
    }

    void OnAddBadge()
    {
        string name = nameField.text.Trim();
        if (string.IsNullOrEmpty(name)) 
        { 
            ShowError("Please enter a badge name."); 
            return; 
        }

        if (!int.TryParse(pointsField.text, out int pts) || pts < 0)
        { 
            ShowError("Please enter a valid points value."); 
            return; 
        }

        AdminSessionManager.Instance?.AddBadge(new BadgeRecord
        {
            title = name,
            pointsRequired = pts,
            iconKey = selectedIconKey
        }, () => { Close(); });
    }

    void ShowError(string msg)
    {
        if (errorText) { errorText.text = msg; errorText.gameObject.SetActive(true); }
    }

    void Close() => AdminUIManager.Instance.CloseAllModals();
}
