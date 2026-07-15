using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text quizTitleText;
    public TMP_Text timerText;
    public Button backBtn;

    [Header("Progress")]
    public TMP_Text questionCountText;
    public Slider timerBar;

    [Header("Question Card")]
    public TMP_Text questionTypeText;   // "TRUE / FALSE" or "MULTIPLE CHOICE"
    public Image questionTypeTag;    // pill background color
    public TMP_Text questionText;

    [Header("True / False Section")]
    public GameObject trueFalseSection;
    public Button trueBtn;
    public Button falseBtn;

    [Header("Multiple Choice Section")]
    public GameObject multipleChoiceSection;
    public Button[] optionBtns;       // 4 buttons: A B C D
    public TMP_Text[] optionTexts;      // text inside each button
    public Image[] optionBGs;        // background image of each button

    [Header("Feedback")]
    public GameObject correctFeedback;
    public GameObject incorrectFeedback;
    public TMP_Text feedbackExplanationText;

    // ── Colors ──────────────────────────────────────────────
    private Color optionDefaultColor = Color.white;
    private Color optionCorrectColor = new Color(0.90f, 1.00f, 0.93f); // light green
    private Color optionIncorrectColor = new Color(1.00f, 0.90f, 0.90f); // light red
    private Color optionCorrectBorder = new Color(0.13f, 0.69f, 0.30f); // green
    private Color optionIncorrectBorder = new Color(0.94f, 0.27f, 0.27f); // red
    private Color tfTagColor = new Color(0.48f, 0.80f, 0.98f); // light blue
    private Color mcTagColor = new Color(0.83f, 0.73f, 0.99f); // light purple

    // ── State ────────────────────────────────────────────────
    private QuizData currentQuiz;
    private int currentIndex = 0;
    private int correctCount = 0;
    private int incorrectCount = 0;
    private float timeRemaining;
    private float totalTime;
    private bool answerGiven = false;
    private List<QuizAnswerRecord> answers = new List<QuizAnswerRecord>();

    void Start()
    {
        backBtn?.onClick.AddListener(OnBackPressed);

        // True/False buttons
        trueBtn.onClick.AddListener(() => SubmitTrueFalseAnswer("true"));
        falseBtn.onClick.AddListener(() => SubmitTrueFalseAnswer("false"));

        // Multiple Choice buttons
        for (int i = 0; i < optionBtns.Length; i++)
        {
            int idx = i;
            optionBtns[i].onClick.AddListener(() => SubmitMultipleChoiceAnswer(idx));
        }

        HideFeedback();
    }

    // ── Start Quiz ───────────────────────────────────────────
    public void StartQuiz(QuizData quiz)
    {
        currentQuiz = quiz;
        currentIndex = 0;
        correctCount = 0;
        incorrectCount = 0;
        answers.Clear();
        totalTime = quiz.timeMinutes * 60f;
        timeRemaining = totalTime;

        quizTitleText.text = quiz.quizTitle;

        ShowQuestion();
        StartCoroutine(TimerRoutine());
    }

    // ── Show Current Question ─────────────────────────────────
    void ShowQuestion()
    {
        if (currentIndex >= currentQuiz.questions.Count)
        {
            EndQuiz();
            return;
        }

        answerGiven = false;
        HideFeedback();
        ResetOptionColors();

        var q = currentQuiz.questions[currentIndex];

        // Question number
        questionCountText.text = $"Q{currentIndex + 1} / {currentQuiz.questions.Count}";

        // Question text
        questionText.text = q.questionText;

        // Show correct section based on type
        bool isTrueFalse = q.questionType == "True/False";

        trueFalseSection?.SetActive(isTrueFalse);
        multipleChoiceSection?.SetActive(!isTrueFalse);

        // Question type tag
        if (questionTypeText)
            questionTypeText.text = isTrueFalse ? "TRUE / FALSE" : "MULTIPLE CHOICE";
        if (questionTypeTag)
            questionTypeTag.color = isTrueFalse ? tfTagColor : mcTagColor;

        // Populate MC options
        if (!isTrueFalse)
            PopulateOptions(q.options);
    }

    // ── Populate MC Options ──────────────────────────────────
    void PopulateOptions(List<string> options)
    {
        string[] labels = { "A", "B", "C", "D" };

        for (int i = 0; i < optionBtns.Length; i++)
        {
            bool hasOption = options != null && i < options.Count;

            optionBtns[i].gameObject.SetActive(hasOption);

            if (hasOption && optionTexts != null && i < optionTexts.Length)
                optionTexts[i].text = options[i];
        }
    }

    // ── Submit True/False Answer ─────────────────────────────
    void SubmitTrueFalseAnswer(string answer)
    {
        if (answerGiven) return;
        answerGiven = true;

        var q = currentQuiz.questions[currentIndex];
        bool isCorrect = answer == q.correctAnswer;
        int pts = isCorrect ? q.points : 0;

        answers.Add(new QuizAnswerRecord(
            q.questionText, q.questionType,
            q.correctAnswer, answer, isCorrect, pts));

        // Highlight buttons
        if (answer == "true")
            HighlightTFButton(trueBtn, isCorrect);
        else
            HighlightTFButton(falseBtn, isCorrect);

        if (isCorrect) correctCount++;
        else incorrectCount++;

        ShowFeedback(isCorrect, q.explanation);
        StartCoroutine(NextQuestionDelay(1.5f));
    }

    void HighlightTFButton(Button btn, bool correct)
    {
        var img = btn.GetComponent<Image>();
        if (img) img.color = correct ? optionCorrectBorder : optionIncorrectBorder;
    }

    // ── Submit Multiple Choice Answer ────────────────────────
    void SubmitMultipleChoiceAnswer(int index)
    {
        if (answerGiven) return;
        answerGiven = true;

        var q = currentQuiz.questions[currentIndex];
        string answer = (q.options != null && index < q.options.Count)
                            ? q.options[index] : "";
        bool isCorrect = answer == q.correctAnswer;
        int pts = isCorrect ? q.points : 0;

        answers.Add(new QuizAnswerRecord(
            q.questionText, q.questionType,
            q.correctAnswer, answer, isCorrect, pts));

        // Color all options — show correct in green, wrong in red
        for (int i = 0; i < optionBtns.Length; i++)
        {
            if (!optionBtns[i].gameObject.activeSelf) continue;

            bool isThisCorrect = (q.options != null && i < q.options.Count)
                                 && q.options[i] == q.correctAnswer;
            bool isThisSelected = (i == index);

            if (optionBGs != null && i < optionBGs.Length)
            {
                if (isThisCorrect)
                    optionBGs[i].color = optionCorrectColor;
                else if (isThisSelected && !isCorrect)
                    optionBGs[i].color = optionIncorrectColor;
            }
        }

        if (isCorrect) correctCount++;
        else incorrectCount++;

        ShowFeedback(isCorrect, q.explanation);
        StartCoroutine(NextQuestionDelay(1.8f));
    }

    // ── Feedback ─────────────────────────────────────────────
    void ShowFeedback(bool correct, string explanation)
    {
        if (correct)
        {
            correctFeedback?.SetActive(true);
            incorrectFeedback?.SetActive(false);
        }
        else
        {
            correctFeedback?.SetActive(false);
            incorrectFeedback?.SetActive(true);
        }

        if (feedbackExplanationText && !string.IsNullOrEmpty(explanation))
        {
            feedbackExplanationText.gameObject.SetActive(true);
            feedbackExplanationText.text = explanation;
        }
    }

    void HideFeedback()
    {
        correctFeedback?.SetActive(false);
        incorrectFeedback?.SetActive(false);
        if (feedbackExplanationText)
            feedbackExplanationText.gameObject.SetActive(false);
    }

    // ── Reset option colors ──────────────────────────────────
    void ResetOptionColors()
    {
        if (optionBGs == null) return;
        foreach (var bg in optionBGs)
            if (bg) bg.color = optionDefaultColor;

        // Reset T/F button colors
        var trueImg = trueBtn?.GetComponent<Image>();
        var falseImg = falseBtn?.GetComponent<Image>();
        if (trueImg) trueImg.color = new Color(0.13f, 0.69f, 0.30f); // green
        if (falseImg) falseImg.color = new Color(0.94f, 0.27f, 0.27f); // red
    }

    // ── Next Question ────────────────────────────────────────
    IEnumerator NextQuestionDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentIndex++;
        ShowQuestion();
    }

    // ── Timer ────────────────────────────────────────────────
    IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0 && currentIndex < currentQuiz.questions.Count)
        {
            yield return null;
            timeRemaining -= Time.deltaTime;

            if (timerBar) timerBar.value = timeRemaining / totalTime;
            if (timerText)
            {
                int m = Mathf.FloorToInt(timeRemaining / 60);
                int s = Mathf.FloorToInt(timeRemaining % 60);
                timerText.text = $"{m}:{s:00}";
            }
        }
        if (currentIndex < currentQuiz.questions.Count)
            EndQuiz(); // Time's up
    }

    // ── End Quiz ─────────────────────────────────────────────
    void EndQuiz()
    {
        StopAllCoroutines();

        int total = currentQuiz.questions.Count;
        int score = total > 0
            ? Mathf.RoundToInt((float)correctCount / total * 100) : 0;
        int pointsEarned = answers.FindAll(a => a.wasCorrect)
            .ConvertAll(a => a.pointsEarned)
            .Aggregate(0, (sum, p) => sum + p);
        int timeTaken = Mathf.RoundToInt(totalTime - timeRemaining);

        // Save to player data
        PlayerSessionManager.Instance?.SaveQuizResult(
            currentQuiz.quizTitle, score, pointsEarned);

        // Go to results
        var resultUI = UIManager.Instance.quizResultPanel
            .GetComponent<QuizResultUI>();
        resultUI?.ShowResults(score, correctCount, incorrectCount,
            timeTaken, pointsEarned, answers, currentQuiz);

        UIManager.Instance.ShowPanel(UIManager.Instance.quizResultPanel);
    }

    void OnBackPressed()
    {
        StopAllCoroutines();
        UIManager.Instance.GoBack();
    }
}
