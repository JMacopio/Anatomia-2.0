using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateQuizModalUI : MonoBehaviour
{
    public Button closeBtn;
    public TMP_InputField titleField;
    public TMP_InputField categoryField;
    public TMP_InputField timeLimitField;
    public TMP_InputField passingScoreField;
    public Button createQuizBtn;
    public Button cancelBtn;
    public TMP_Text errorText;

    void Start()
    {
        closeBtn.onClick.AddListener(Close);
        cancelBtn.onClick.AddListener(Close);
        createQuizBtn.onClick.AddListener(OnCreateQuiz);
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        titleField.text = "";
        categoryField.text = "";
        timeLimitField.text = "600";
        passingScoreField.text = "70";
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void OnCreateQuiz()
    {
        string title = titleField.text.Trim();
        string category = categoryField.text.Trim();

        if (string.IsNullOrEmpty(title))
        {
            ShowError("Please enter a quiz title."); return;
        }
        if (string.IsNullOrEmpty(category))
        {
            ShowError("Please enter a category."); return;
        }

        int timeLimit = int.TryParse(timeLimitField.text, out int t) ? t : 600;
        int passingScore = int.TryParse(passingScoreField.text, out int p) ? p : 70;

        var quiz = new QuizRecord
        {
            title = title,
            category = category,
            timeLimitSecs = timeLimit,
            passingScore = passingScore
        };

        AdminSessionManager.Instance?.CreateQuiz(quiz, createdId =>
        {
            Debug.Log($"[CreateQuiz] Created quiz ID: {createdId}");
            Close();
        });
    }

    void ShowError(string msg)
    {
        if (errorText) { errorText.text = msg; errorText.gameObject.SetActive(true); }
    }

    void Close() => AdminUIManager.Instance.CloseAllModals();
}
