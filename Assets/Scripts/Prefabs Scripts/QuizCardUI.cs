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

    static readonly Dictionary<string, Color> diffColors = new()
    {
        { "easy",   new Color(0.20f, 0.78f, 0.35f) },
        { "medium", new Color(0.95f, 0.61f, 0.07f) },
        { "hard",   new Color(0.94f, 0.27f, 0.27f) },
    };

    private QuizData data;
    private System.Action<QuizData> onStart;

    public void Setup(QuizData quiz, System.Action<QuizData> callback)
    {
        data = quiz;
        onStart = callback;

        titleText.text = quiz.quizTitle;
        systemText.text = quiz.systemName;
        difficultyText.text = quiz.difficulty;
        timeText.text = $"{quiz.timeMinutes} min";
        questionsText.text = quiz.questions.Count.ToString();
        pointsText.text = quiz.pointsReward.ToString();

        if (difficultyBadge && diffColors.ContainsKey(quiz.difficulty))
            difficultyBadge.color = diffColors[quiz.difficulty];

        startBtn.onClick.AddListener(() => onStart?.Invoke(data));
    }
}