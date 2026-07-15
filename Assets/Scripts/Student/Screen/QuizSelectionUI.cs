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

    // Sample quiz data — in production load from Firestore
    private List<QuizData> availableQuizzes = new List<QuizData>()
    {
        new QuizData("Skeletal System Basics",    "Skeletal System",   "easy",   10, 4, 40),
        new QuizData("Muscular System Advanced",  "Muscular System",   "medium", 15, 4, 60),
        new QuizData("Cardiovascular Essentials", "Cardiovascular",    "hard",   20, 4, 80),
    };

    void Start()
    {
        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());

        // Add sample questions to quizzes
        SetupSampleQuestions();
        BuildQuizList();
        PopulateStats();
    }

    void SetupSampleQuestions()
    {
        // Quiz 1 — mix of True/False and Multiple Choice
        availableQuizzes[0].questions = new List<QuizQuestion>
        {
            new QuizQuestion("The skull protects the brain.", true,
                "The cranium is the part of the skull that encloses the brain."),

            new QuizQuestion("The trachea is also known as the windpipe.", true,
                "The trachea connects the larynx to the lungs."),

            new QuizQuestion("How many bones are in the adult human body?",
                new List<string> { "150", "186", "206", "256" },
                "206", "Medium", 20,
                "An adult human body has 206 bones."),

            new QuizQuestion("Which bone is the longest in the human body?",
                new List<string> { "Tibia", "Femur", "Humerus", "Fibula" },
                "Femur", "Easy", 20,
                "The femur (thigh bone) is the longest bone in the body."),
        };

        // Quiz 2 — mainly Multiple Choice
        availableQuizzes[1].questions = new List<QuizQuestion>
        {
            new QuizQuestion("How many muscles are in the human body?",
                new List<string> { "Over 200", "Over 400", "Over 600", "Over 800" },
                "Over 600", "Medium", 20,
                "The human body has over 600 muscles."),

            new QuizQuestion("The bicep is located in the upper arm.", true),

            new QuizQuestion("Which muscle is responsible for breathing?",
                new List<string> { "Trapezius", "Diaphragm", "Pectoralis", "Deltoid" },
                "Diaphragm", "Medium", 20,
                "The diaphragm is the primary muscle used for breathing."),

            new QuizQuestion("Tendons connect muscles to bones.", true,
                "Tendons are fibrous connective tissue that attach muscle to bone."),
        };
    }

    void BuildQuizList()
    {
        foreach (Transform child in quizListParent) Destroy(child.gameObject);
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
        if (completedText) completedText.text = data.quizzesCompleted.ToString();
        if (avgScoreText) avgScoreText.text = $"{data.avgScore}%";
        if (perfectScoresText) perfectScoresText.text = "12";
    }

    void OnStartQuiz(QuizData quiz)
    {
        var quizUI = UIManager.Instance.quizPanel.GetComponent<QuizUI>();
        quizUI?.StartQuiz(quiz);
        UIManager.Instance.ShowPanel(UIManager.Instance.quizPanel);
    }
}

[System.Serializable]
public class QuizData
{
    public string quizId;
    public string quizTitle;
    public string systemName;
    public string difficulty;       // "easy" | "medium" | "hard"
    public int timeMinutes;
    public int questionCount;
    public int pointsReward;
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    public QuizData() { }

    public QuizData(string title, string system, string diff,
                    int time, int qCount, int pts)
    {
        quizTitle = title;
        systemName = system;
        difficulty = diff;
        timeMinutes = time;
        questionCount = qCount;
        pointsReward = pts;
    }
}

[System.Serializable]
public class QuizQuestion
{
    public string questionId;
    public string questionText;
    public string questionType;    // "True/False" | "Multiple Choice"
    public List<string> options = new List<string>(); // for MC only
    public string correctAnswer;   // "true"/"false" or exact option text
    public string difficulty;      // "Easy" | "Medium" | "Hard"
    public int points;
    public string explanation;     // optional explanation shown after answer

    // True/False constructor
    public QuizQuestion(string text, bool correct, string explain = "")
    {
        questionText = text;
        questionType = "True/False";
        correctAnswer = correct ? "true" : "false";
        difficulty = "Easy";
        points = 10;
        explanation = explain;
    }

    // Multiple Choice constructor
    public QuizQuestion(string text, List<string> opts,
                        string correct, string diff = "Medium",
                        int pts = 20, string explain = "")
    {
        questionText = text;
        questionType = "Multiple Choice";
        options = opts;
        correctAnswer = correct;
        difficulty = diff;
        points = pts;
        explanation = explain;
    }
}

[System.Serializable]
public class QuizAnswerRecord
{
    public string questionText;
    public string questionType;
    public string correctAnswer;
    public string playerAnswer;
    public bool wasCorrect;
    public int pointsEarned;

    public QuizAnswerRecord(string qt, string qType,
                            string ca, string pa, bool correct, int pts)
    {
        questionText = qt;
        questionType = qType;
        correctAnswer = ca;
        playerAnswer = pa;
        wasCorrect = correct;
        pointsEarned = pts;
    }
}