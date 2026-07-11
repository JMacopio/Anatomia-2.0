using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizResultUI : MonoBehaviour
{
    [Header("Header Section")]
    public Image headerBackground;      // orange-red gradient image
    public Image trophyIconCircle;      // orange circle behind trophy
    public TMP_Text resultMessageText;  // "Keep Practicing!" / "Excellent!"
    public TMP_Text quizNameText;       // "Skeletal System Basics"

    [Header("Score Card")]
    public TMP_Text scorePercentText;   // large "50%"
    public TMP_Text yourScoreLabel;     // "Your Score"

    [Header("Stats Row")]
    public TMP_Text correctCountText;
    public TMP_Text incorrectCountText;
    public TMP_Text totalCountText;

    [Header("Points Earned")]
    public TMP_Text pointsValueText;    // "10 / 20"
    public Slider pointsProgressBar;    // fills based on earned/total

    [Header("Reward Cards")]
    public TMP_Text pointsEarnedText;   // "+10"
    public TMP_Text bonusXPText;        // "+25"

    [Header("Buttons")]
    public Button backToDashboardBtn;
    public Button retryBtn;
    public Button shareBtn;

    [Header("Header Gradient Colors")]
    public Color excellentColorA = new Color(0.13f, 0.69f, 0.30f); // green
    public Color excellentColorB = new Color(0.07f, 0.53f, 0.25f);
    public Color goodColorA = new Color(0.20f, 0.60f, 0.90f); // blue
    public Color goodColorB = new Color(0.10f, 0.40f, 0.80f);
    public Color poorColorA = new Color(0.98f, 0.45f, 0.20f); // orange-red
    public Color poorColorB = new Color(0.85f, 0.25f, 0.10f);

    private QuizData lastQuiz;
    private List<QuizAnswerRecord> lastAnswers;

    void Start()
    {
        backToDashboardBtn.onClick.AddListener(() =>
            UIManager.Instance.NavigateTo(UIManager.Instance.dashboardPanel, 0));

        retryBtn.onClick.AddListener(OnRetry);
        shareBtn.onClick.AddListener(OnShare);
    }

    /// <summary>
    /// Called from QuizUI.cs after quiz ends
    /// </summary>
    public void ShowResults(int score, int correct, int incorrect, int timeSecs,
        int pointsEarned, List<QuizAnswerRecord> answers, QuizData quiz)
    {
        lastQuiz = quiz;
        lastAnswers = answers;

        int total = correct + incorrect;
        int totalPoints = quiz.pointsReward;
        int bonusXP = Mathf.RoundToInt(pointsEarned * 2.5f); // bonus XP multiplier

        // ── Header ─────────────────────────────────────────────
        quizNameText.text = quiz.quizTitle;

        resultMessageText.text = score >= 90 ? "Excellent! 🏆"
                               : score >= 70 ? "Good Job! 👍"
                               : score >= 50 ? "Keep Practicing! 💪"
                               : "Try Again! 📚";

        // Tint header based on score
        if (headerBackground)
            headerBackground.color = score >= 70 ? goodColorA : poorColorA;

        // ── Score Card ─────────────────────────────────────────
        scorePercentText.text = $"{score}%";
        correctCountText.text = correct.ToString();
        incorrectCountText.text = incorrect.ToString();
        totalCountText.text = total.ToString();

        // Points earned row
        pointsValueText.text = $"{pointsEarned} / {totalPoints}";
        if (pointsProgressBar)
            pointsProgressBar.value = totalPoints > 0
                ? (float)pointsEarned / totalPoints : 0f;

        // ── Reward Cards ───────────────────────────────────────
        if (pointsEarnedText) pointsEarnedText.text = $"+{pointsEarned}";
        if (bonusXPText) bonusXPText.text = $"+{bonusXP}";

        // ── Animate score ──────────────────────────────────────
        StopAllCoroutines();
        StartCoroutine(AnimateScore(0, score, 1.2f));
        StartCoroutine(AnimateCounter(pointsEarnedText, 0, pointsEarned, 1.0f, "+"));
        StartCoroutine(AnimateCounter(bonusXPText, 0, bonusXP, 1.0f, "+"));
    }

    void OnRetry()
    {
        if (lastQuiz == null) return;
        // Re-start the same quiz
        QuizUI quizUI = UIManager.Instance.quizPanel.GetComponent<QuizUI>();
        quizUI?.StartQuiz(lastQuiz);
        UIManager.Instance.ShowPanel(UIManager.Instance.quizPanel);
    }

    void OnShare()
    {
        // Native share on Android/iOS
        string scoreText = scorePercentText.text;
        string msg = $"I scored {scoreText} on {lastQuiz?.quizTitle ?? "an anatomy quiz"} " +
                     $"in Anatomia 3D! 🦴";

#if UNITY_ANDROID || UNITY_IOS
        // Use Unity's native share plugin or NativeShare package
        // NativeShare example (requires NativeShare plugin):
        // new NativeShare().SetText(msg).Share();
        Debug.Log($"[Share] {msg}");
#else
        GUIUtility.systemCopyBuffer = msg;
        Debug.Log("[Share] Copied to clipboard: " + msg);
#endif
    }

    // ── Animators ───────────────────────────────────────────────
    IEnumerator AnimateScore(int from, int to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int cur = Mathf.RoundToInt(Mathf.Lerp(from, to,
                      Mathf.SmoothStep(0f, 1f, elapsed / duration)));
            scorePercentText.text = $"{cur}%";
            yield return null;
        }
        scorePercentText.text = $"{to}%";
    }

    IEnumerator AnimateCounter(TMP_Text label, int from, int to,
                               float duration, string prefix = "")
    {
        if (label == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int cur = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
            label.text = $"{prefix}{cur}";
            yield return null;
        }
        label.text = $"{prefix}{to}";
    }
}
