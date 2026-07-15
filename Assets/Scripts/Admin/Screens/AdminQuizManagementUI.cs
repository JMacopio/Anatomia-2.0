using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminQuizManagementUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;
    public Button newQuizBtn;

    [Header("Stats")]
    public TMP_Text totalQuizzesText;
    public TMP_Text totalQuestionsText;
    public TMP_Text categoriesText;

    [Header("Quiz List")]
    public Transform quizListParent;
    public GameObject quizRowPrefab;

    void Start()
    {
        backBtn.onClick.AddListener(() => AdminUIManager.Instance.GoBack());
        newQuizBtn.onClick.AddListener(() => AdminUIManager.Instance.ShowCreateQuizModal());

        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed += Refresh;
    }

    void OnEnable()
    {
        AdminSessionManager.Instance?.LoadQuizzes();
        Refresh();
    }

    public void Refresh()
    {
        var session = AdminSessionManager.Instance;
        if (session == null) return;

        var quizzes = session.quizzes;

        // Stats
        int totalQ = 0;
        var categories = new System.Collections.Generic.HashSet<string>();
        foreach (var q in quizzes)
        {
            totalQ += q.questionCount;
            if (!string.IsNullOrEmpty(q.category)) categories.Add(q.category);
        }

        totalQuizzesText.text = quizzes.Count.ToString();
        totalQuestionsText.text = totalQ.ToString();
        categoriesText.text = categories.Count.ToString();

        BuildQuizList(quizzes);
    }

    void BuildQuizList(List<QuizRecord> quizzes)
    {
        foreach (Transform child in quizListParent) Destroy(child.gameObject);

        foreach (var quiz in quizzes)
        {
            var row = Instantiate(quizRowPrefab, quizListParent);
            row.GetComponent<QuizRowUI>()?.Setup(quiz, OnAddQuestion, OnDeleteQuiz);
        }
    }

    void OnAddQuestion(QuizRecord quiz)
    {
        // Store selected quiz reference then open modal
        AddQuestionModalUI.TargetQuizId = quiz.quizId;
        AdminUIManager.Instance.ShowAddQuestionModal();
    }

    void OnDeleteQuiz(QuizRecord quiz)
    {
        AdminSessionManager.Instance?.DeleteQuiz(quiz.quizId);
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed -= Refresh;
    }
}
