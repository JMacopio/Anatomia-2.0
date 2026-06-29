using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivityItemUI : MonoBehaviour
{
    public TMP_Text quizNameText;
    public TMP_Text dateText;
    public TMP_Text scoreText;
    public TMP_Text pointsText;
    public Image scoreColor; // tinted based on score

    public void Setup(string name, string date, int score, int points)
    {
        quizNameText.text = name;
        dateText.text = date;
        scoreText.text = $"{score}%";
        pointsText.text = $"+{points} pts";

        // Color code score
        if (scoreColor != null)
        {
            scoreColor.color = score >= 90
                ? new Color(0.4f, 0.2f, 0.9f) // purple - excellent
                : score >= 70
                    ? new Color(0.2f, 0.7f, 0.4f) // green - good
                    : new Color(1f, 0.6f, 0.1f);   // amber - needs work
        }
    }
}
