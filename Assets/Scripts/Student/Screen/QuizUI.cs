using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizUI : MonoBehaviour
{
    [Header("Quiz UI")]
    public TMP_Text quizTitleText;
    public TMP_Text timerText;
    public TMP_Text questionCountText;
    public TMP_Text questionText;
    public Slider timerBar;

    [Header("True/False Buttons")]
    public Button trueBtn;
    public Button falseBtn;

    [Header("Feedback")]
    public GameObject correctFeedback;
    public GameObject incorrectFeedback;

    private QuizData currentQuiz;
    private int currentQuestionIndex = 0;
    private int correctCount = 0;
    private int wrongCount = 0;
    private float timeRemaining;
    private float totalTime;
    private bool answerGiven = false;
    private List<QuizAnswerRecord> answers = new List<QuizAnswerRecord>();

    void Start()
    {
        trueBtn.onClick.AddListener(() => AnswerQuestion(true));
        falseBtn.onClick.AddListener(() => AnswerQuestion(false));
        correctFeedback?.SetActive(false);
        incorrectFeedback?.SetActive(false);
    }

    public void StartQuiz(QuizData quiz)
    {
        currentQuiz = quiz;
        currentQuestionIndex = 0;
        correctCount = 0;
        wrongCount = 0;
        answers.Clear();
        totalTime = quiz.timeMinutes * 60f;
        timeRemaining = totalTime;
        quizTitleText.text = quiz.quizTitle;
        ShowCurrentQuestion();
        StartCoroutine(TimerRoutine());
    }

    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= currentQuiz.questions.Count)
        {
            EndQuiz();
            return;
        }

        answerGiven = false;
        trueBtn.interactable = true;
        falseBtn.interactable = true;
        correctFeedback?.SetActive(false);
        incorrectFeedback?.SetActive(false);

        var q = currentQuiz.questions[currentQuestionIndex];
        questionText.text = q.questionText;
        questionCountText.text = $"Q{currentQuestionIndex + 1}/{currentQuiz.questions.Count}";
    }

    void AnswerQuestion(bool answer)
    {
        if (answerGiven) return;
        answerGiven = true;

        var q = currentQuiz.questions[currentQuestionIndex];
        bool isCorrect = (answer == q.correctAnswer);

        trueBtn.interactable = false;
        falseBtn.interactable = false;

        if (isCorrect)
        {
            correctCount++;
            correctFeedback?.SetActive(true);
        }
        else
        {
            wrongCount++;
            incorrectFeedback?.SetActive(true);
        }

        answers.Add(new QuizAnswerRecord(q.questionText, q.correctAnswer, answer, isCorrect));
        StartCoroutine(NextQuestionAfterDelay(1.2f));
    }

    IEnumerator NextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0 && currentQuestionIndex < currentQuiz.questions.Count)
        {
            yield return null;
            timeRemaining -= Time.deltaTime;
            timerBar.value = timeRemaining / totalTime;
            int mins = Mathf.FloorToInt(timeRemaining / 60);
            int secs = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"{mins}:{secs:00}";
        }
        if (currentQuestionIndex < currentQuiz.questions.Count)
            EndQuiz(); // Time's up
    }

    void EndQuiz()
    {
        StopAllCoroutines();
        float timeTaken = totalTime - timeRemaining;
        int score = currentQuiz.questions.Count > 0
            ? Mathf.RoundToInt((float)correctCount / currentQuiz.questions.Count * 100)
            : 0;
        int pointsEarned = score >= 50 ? currentQuiz.pointsReward / 2 : 0;
        if (score == 100) pointsEarned = currentQuiz.pointsReward;

        // Update student data
        var data = PlayerSessionManager.Instance?.studentData;
        if (data != null)
        {
            data.totalPoints += pointsEarned;
            data.quizzesCompleted++;
        }

        // Go to results
        QuizResultUI resultUI = UIManager.Instance.quizResultPanel.GetComponent<QuizResultUI>();
        resultUI?.ShowResults(score, correctCount, wrongCount,
            Mathf.RoundToInt(timeTaken), pointsEarned, answers, currentQuiz);
        UIManager.Instance.ShowPanel(UIManager.Instance.quizResultPanel);
    }
}

[System.Serializable]
public class QuizAnswerRecord
{
    public string questionText;
    public bool correctAnswer;
    public bool playerAnswer;
    public bool wasCorrect;

    public QuizAnswerRecord(string q, bool ca, bool pa, bool wc)
    { questionText = q; correctAnswer = ca; playerAnswer = pa; wasCorrect = wc; }
}

