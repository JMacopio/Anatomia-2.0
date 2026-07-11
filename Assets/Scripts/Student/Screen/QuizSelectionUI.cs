using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizSelectionUI : MonoBehaviour
{
    [Header("List")]
    public Transform quizListParent;
    public GameObject quizCardPrefab;
    public Button backBtn;

    [Header("Stats Card")]
    public TMP_Text completedText;
    public TMP_Text avgScoreText;
    public TMP_Text perfectScoresText;

    // Quiz data — in production load from backend/ScriptableObjects
    private List<QuizData> availableQuizzes = new List<QuizData>()
    {
        new QuizData("Skeletal System Basics",   "Skeletal System",   "easy",   10, 2, 20),
        new QuizData("Muscular System Advanced", "Muscular System",   "medium", 15, 1, 20),
        new QuizData("Cardiovascular Essentials","Cardiovascular System","hard", 20, 5, 50),
        new QuizData("Respiratory System Quiz",  "Respiratory System","easy",   10, 3, 30),
    };

    void Start()
    {
        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());
        BuildQuizList();
        PopulateStats();
    }

    void BuildQuizList()
    {
        foreach (Transform child in quizListParent)
            Destroy(child.gameObject);

        foreach (var quiz in availableQuizzes)
        {
            var card = Instantiate(quizCardPrefab, quizListParent);
            card.GetComponent<QuizCardUI>()?.Setup(quiz, OnStartQuiz);
        }
    }

    void PopulateStats()
    {
        var data = PlayerSessionManager.Instance?.studentData;
        if (data == null) return;
        completedText.text = data.quizzesCompleted.ToString();
        avgScoreText.text = $"{data.avgScore}%";
        perfectScoresText.text = "12"; // Example value
    }

    void OnStartQuiz(QuizData quiz)
    {
        QuizUI quizUI = UIManager.Instance.quizPanel.GetComponent<QuizUI>();
        quizUI?.StartQuiz(quiz);
        UIManager.Instance.ShowPanel(UIManager.Instance.quizPanel);
    }
}

[System.Serializable]
public class QuizData
{
    public string quizTitle;
    public string systemName;
    public string difficulty; // "easy", "medium", "hard"
    public int timeMinutes;
    public int questionCount;
    public int pointsReward;
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    public QuizData(string title, string system, string diff, int time, int qCount, int pts)
    {
        quizTitle = title; systemName = system; difficulty = diff;
        timeMinutes = time; questionCount = qCount; pointsReward = pts;

        // Add sample questions
        questions.Add(new QuizQuestion("The skull protects the brain.", true));
        questions.Add(new QuizQuestion("The trachea is also known as the windpipe.", true));
    }
}

[System.Serializable]
public class QuizQuestion
{
    public string questionText;
    public bool correctAnswer; // true/false quiz style shown in mockup
    public string explanation;

    public QuizQuestion(string text, bool answer, string explain = "")
    { questionText = text; correctAnswer = answer; explanation = explain; }
}

