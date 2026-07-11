using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizRowUI : MonoBehaviour
{
    public TMP_Text quizTitleText;
    public TMP_Text categoryText;
    public TMP_Text questionsText;
    public TMP_Text timeText;
    public TMP_Text passingText;
    public Button expandBtn;
    public Button deleteBtn;
    public Button addQuestionBtn;
    public Image expandIcon;
    public Sprite chevronDownSprite;
    public Sprite chevronUpSprite;
    public GameObject expandedSection;

    private QuizRecord data;
    private System.Action<QuizRecord> onAddQuestion;
    private System.Action<QuizRecord> onDelete;
    private bool isExpanded = false;

    public void Setup(QuizRecord quiz,
                      System.Action<QuizRecord> addQuestionCallback,
                      System.Action<QuizRecord> deleteCallback)
    {
        data = quiz;
        onAddQuestion = addQuestionCallback;
        onDelete = deleteCallback;

        quizTitleText.text = quiz.title;
        categoryText.text = quiz.category;
        questionsText.text = $"{quiz.questionCount} questions";
        timeText.text = $"{quiz.timeLimitSecs / 60} min";
        passingText.text = $"{quiz.passingScore}% passing";

        if (expandedSection) expandedSection.SetActive(false);

        expandBtn.onClick.AddListener(ToggleExpand);
        deleteBtn.onClick.AddListener(() => onDelete?.Invoke(data));
        addQuestionBtn?.onClick.AddListener(() => onAddQuestion?.Invoke(data));
    }

    void ToggleExpand()
    {
        isExpanded = !isExpanded;
        if (expandedSection) expandedSection.SetActive(isExpanded);
        if (expandIcon)
            expandIcon.sprite = isExpanded ? chevronUpSprite : chevronDownSprite;
    }
}
