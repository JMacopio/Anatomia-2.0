using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddLevelModalUI : MonoBehaviour
{
    public Button closeBtn;
    public TMP_InputField levelNumberField;
    public TMP_InputField levelTitleField;
    public TMP_InputField pointsField;
    public Button addLevelBtn;
    public Button cancelBtn;
    public TMP_Text errorText;

    void Start()
    {
        closeBtn.onClick.AddListener(Close);
        cancelBtn.onClick.AddListener(Close);
        addLevelBtn.onClick.AddListener(OnAddLevel);
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // Auto-fill next level number
        var levels = AdminSessionManager.Instance?.gamificationSettings.levels;
        int nextNum = (levels != null && levels.Count > 0) ? levels.Count + 1 : 1;
        levelNumberField.text = nextNum.ToString();
        levelTitleField.text = "";
        pointsField.text = "";
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void OnAddLevel()
    {
        string title = levelTitleField.text.Trim();
        if (string.IsNullOrEmpty(title)) 
        { ShowError("Please enter a level title."); 
            return; 
        }

        if (!int.TryParse(levelNumberField.text, out int num))
        { ShowError("Invalid level number."); 
            return; 
        }

        if (!int.TryParse(pointsField.text, out int pts)) 
        { ShowError("Invalid points value."); 
            return; 
        }

        AdminSessionManager.Instance?.AddLevel(new LevelRecord
        {
            levelNumber = num,
            title = title,
            pointsRequired = pts
        }, () => { Close(); });
    }

    void ShowError(string msg)
    {
        if (errorText) { errorText.text = msg; errorText.gameObject.SetActive(true); }
    }

    void Close() => AdminUIManager.Instance.CloseAllModals();
}