using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopPerformerRowUI : MonoBehaviour
{
    public TMP_Text rankText;
    public Image rankBadge;
    public TMP_Text nameText;
    public TMP_Text quizzesText;
    public TMP_Text pointsText;
    public TMP_Text levelText;

    static readonly Color[] rankColors =
    {
        new Color(1.00f, 0.84f, 0.00f), // gold
        new Color(0.75f, 0.75f, 0.75f), // silver
        new Color(0.80f, 0.50f, 0.20f), // bronze
    };

    public void Setup(TopPerformer p)
    {
        rankText.text = p.rank.ToString();
        nameText.text = p.name;
        quizzesText.text = $"{p.quizzesCompleted} quizzes completed";
        pointsText.text = $"{p.points:N0} pts";
        levelText.text = $"Level {p.level}";

        if (rankBadge && p.rank <= 3)
            rankBadge.color = rankColors[p.rank - 1];
    }
}

