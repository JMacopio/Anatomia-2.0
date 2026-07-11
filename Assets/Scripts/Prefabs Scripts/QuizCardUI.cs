using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text systemText;
    public TMP_Text difficultyText;
    public Image difficultyBadge;
    public TMP_Text timeText;
    public TMP_Text questionsText;
    public TMP_Text pointsText;
    public Button startBtn;

    private static readonly Dictionary<string, Color> diffColors = new Dictionary<string, Color>
    {
        { "easy",   new Color(0.2f, 0.8f, 0.4f) },
        { "medium", new Color(1.0f, 0.7f, 0.0f) },
        { "hard",   new Color(0.9f, 0.2f, 0.2f) },
    };

    private QuizData quizData;
    private System.Action<QuizData> onStartCallback;

    public void Setup(QuizData data, System.Action<QuizData> callback)
    {
        quizData = data;
        onStartCallback = callback;

        titleText.text = data.quizTitle;
        systemText.text = data.systemName;
        difficultyText.text = data.difficulty;
        timeText.text = $"{data.timeMinutes} min";
        questionsText.text = data.questionCount.ToString();
        pointsText.text = data.pointsReward.ToString();

        if (difficultyBadge && diffColors.ContainsKey(data.difficulty))
            difficultyBadge.color = diffColors[data.difficulty];

        startBtn.onClick.AddListener(() => onStartCallback?.Invoke(quizData));
    }
}
