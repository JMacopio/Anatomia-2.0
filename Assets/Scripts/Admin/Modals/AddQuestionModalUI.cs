using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddQuestionModalUI : MonoBehaviour
{
    // Static — set by QuizManagementUI before opening modal
    public static string TargetQuizId = "";

    [Header("Header")]
    public Button closeBtn;
    public TMP_Text modalTitleText;

    [Header("Question")]
    public TMP_InputField questionField;
    public TMP_Dropdown typeDropdown;

    [Header("Multiple Choice Section")]
    public GameObject mcSection;
    public TMP_InputField option1Field;
    public TMP_InputField option2Field;
    public TMP_InputField option3Field;
    public TMP_InputField option4Field;
    public TMP_Dropdown mcCorrectDropdown;  // "Option A" | "B" | "C" | "D"

    [Header("True/False Section")]
    public GameObject tfSection;
    public TMP_Dropdown tfCorrectDropdown;  // "True" | "False"

    [Header("Common Settings")]
    public TMP_Dropdown difficultyDropdown;
    public TMP_InputField pointsField;
    public TMP_InputField explanationField;

    [Header("Buttons & Feedback")]
    public Button addQuestionBtn;
    public Button cancelBtn;
    public TMP_Text errorText;

    // Question type constants
    private const string TYPE_MC = "Multiple Choice";
    private const string TYPE_TF = "True/False";

    void Start()
    {
        closeBtn.onClick.AddListener(Close);
        cancelBtn.onClick.AddListener(Close);
        addQuestionBtn.onClick.AddListener(OnAddQuestion);
        typeDropdown.onValueChanged.AddListener(OnTypeChanged);

        // Setup dropdowns
        SetupTypeDropdown();
        SetupDifficultyDropdown();
        SetupTFDropdown();
        SetupMCCorrectDropdown();

        if (errorText) errorText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        ClearForm();
        OnTypeChanged(0); // Default to Multiple Choice
    }

    // ── Dropdown Setup ───────────────────────────────────────
    void SetupTypeDropdown()
    {
        typeDropdown.ClearOptions();
        typeDropdown.AddOptions(new List<string> { TYPE_MC, TYPE_TF });
    }

    void SetupDifficultyDropdown()
    {
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new List<string> { "Easy", "Medium", "Hard" });
        difficultyDropdown.value = 1; // Default: Medium
    }

    void SetupTFDropdown()
    {
        tfCorrectDropdown.ClearOptions();
        tfCorrectDropdown.AddOptions(new List<string> { "True", "False" });
    }

    void SetupMCCorrectDropdown()
    {
        mcCorrectDropdown.ClearOptions();
        mcCorrectDropdown.AddOptions(new List<string>
            { "Option A", "Option B", "Option C", "Option D" });
    }

    // ── Toggle MC / TF sections ──────────────────────────────
    void OnTypeChanged(int index)
    {
        bool isMC = (index == 0); // 0 = Multiple Choice, 1 = True/False

        mcSection?.SetActive(isMC);
        tfSection?.SetActive(!isMC);

        // Update default points based on type
        if (pointsField)
            pointsField.text = isMC ? "20" : "10";

        if (errorText) errorText.gameObject.SetActive(false);
    }

    // ── Add Question ─────────────────────────────────────────
    void OnAddQuestion()
    {
        string questionText = questionField.text.Trim();
        if (string.IsNullOrEmpty(questionText))
        { ShowError("Please enter a question."); return; }

        if (string.IsNullOrEmpty(TargetQuizId))
        { ShowError("No quiz selected."); return; }

        bool isMC = typeDropdown.value == 0;
        string type = isMC ? TYPE_MC : TYPE_TF;
        string difficulty = difficultyDropdown.options[difficultyDropdown.value].text;
        int points = int.TryParse(pointsField.text, out int p) ? p : 10;
        string explanation = explanationField?.text.Trim() ?? "";

        QuestionRecord q;

        if (isMC)
        {
            // Validate MC options
            string opt1 = option1Field.text.Trim();
            string opt2 = option2Field.text.Trim();
            string opt3 = option3Field.text.Trim();
            string opt4 = option4Field.text.Trim();

            if (string.IsNullOrEmpty(opt1) || string.IsNullOrEmpty(opt2))
            { ShowError("Please enter at least 2 options."); return; }

            // Build options list (only non-empty ones)
            var options = new List<string>();
            if (!string.IsNullOrEmpty(opt1)) options.Add(opt1);
            if (!string.IsNullOrEmpty(opt2)) options.Add(opt2);
            if (!string.IsNullOrEmpty(opt3)) options.Add(opt3);
            if (!string.IsNullOrEmpty(opt4)) options.Add(opt4);

            // Get correct answer from dropdown selection
            int correctIdx = mcCorrectDropdown.value;
            if (correctIdx >= options.Count)
            { ShowError("Correct answer selection is out of range."); return; }
            string correctAnswer = options[correctIdx];

            q = new QuestionRecord
            {
                questionText = questionText,
                questionType = TYPE_MC,
                options = options,
                correctAnswer = correctAnswer,
                difficulty = difficulty,
                points = points,
                explanation = explanation
            };
        }
        else
        {
            // True/False — correct answer from TF dropdown
            string correctAnswer = tfCorrectDropdown.value == 0 ? "true" : "false";

            q = new QuestionRecord
            {
                questionText = questionText,
                questionType = TYPE_TF,
                options = new List<string>(),
                correctAnswer = correctAnswer,
                difficulty = difficulty,
                points = points,
                explanation = explanation
            };
        }

        // Save to Firestore
        AdminSessionManager.Instance?.AddQuestion(TargetQuizId, q, () =>
        {
            Debug.Log($"[AddQuestion] Added: {questionText} ({type})");
            Close();
        });
    }

    // ── Helpers ──────────────────────────────────────────────
    void ShowError(string msg)
    {
        if (errorText) { errorText.text = msg; errorText.gameObject.SetActive(true); }
    }

    void ClearForm()
    {
        questionField.text = "";
        option1Field.text = "";
        option2Field.text = "";
        option3Field.text = "";
        option4Field.text = "";
        if (explanationField) explanationField.text = "";
        if (pointsField) pointsField.text = "20";
        typeDropdown.value = 0;
        difficultyDropdown.value = 1; // Medium
        mcCorrectDropdown.value = 0;
        tfCorrectDropdown.value = 0;
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void Close() => AdminUIManager.Instance.CloseAllModals();
}