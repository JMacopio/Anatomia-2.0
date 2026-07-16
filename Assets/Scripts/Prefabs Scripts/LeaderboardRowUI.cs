using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LeaderboardUI;

public class LeaderboardRowUI : MonoBehaviour
{
    [Header("Rank")]
    public TMP_Text rankText;
    public Image rankBadge;         // circle for top 3
    public Image rowBackground;     // highlight if current user

    [Header("Avatar")]
    public Image avatarBG;
    public TMP_Text avatarText;        // initials

    [Header("Info")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text quizzesText;
    public TMP_Text avgScoreText;

    [Header("Points")]
    public TMP_Text pointsText;

    // Rank colors
    private static readonly Color goldColor = new Color(1.00f, 0.84f, 0.00f);
    private static readonly Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    private static readonly Color bronzeColor = new Color(0.80f, 0.50f, 0.20f);
    private static readonly Color purpleColor = new Color(0.49f, 0.23f, 0.93f);

    // Avatar colors — cycles through a palette
    private static readonly Color[] avatarColors =
    {
        new Color(0.49f, 0.23f, 0.93f), // purple
        new Color(0.23f, 0.51f, 0.96f), // blue
        new Color(0.13f, 0.69f, 0.30f), // green
        new Color(0.95f, 0.61f, 0.07f), // amber
        new Color(0.94f, 0.27f, 0.27f), // red
        new Color(0.06f, 0.71f, 0.80f), // teal
    };

    public void Setup(LeaderboardEntry entry)
    {
        // ── Rank ─────────────────────────────────────────────
        if (rankText)
        {
            rankText.text = $"#{entry.rank}";
            rankText.color = entry.rank switch
            {
                1 => goldColor,
                2 => silverColor,
                3 => bronzeColor,
                _ => new Color(0.40f, 0.40f, 0.40f)
            };
            rankText.fontStyle = entry.rank <= 3
                ? FontStyles.Bold : FontStyles.Normal;
        }

        // Show badge circle for top 3
        if (rankBadge)
        {
            rankBadge.gameObject.SetActive(entry.rank <= 3);
            rankBadge.color = entry.rank switch
            {
                1 => goldColor,
                2 => silverColor,
                3 => bronzeColor,
                _ => Color.grey
            };
        }

        // ── Avatar ────────────────────────────────────────────
        if (avatarText) avatarText.text = GetInitials(entry.studentName);
        if (avatarBG)
            avatarBG.color = avatarColors[(entry.rank - 1) % avatarColors.Length];

        // ── Info ──────────────────────────────────────────────
        if (nameText)
        {
            nameText.text = entry.isCurrentUser
                ? entry.studentName + " (You)"
                : entry.studentName;
            nameText.color = entry.isCurrentUser ? purpleColor : Color.black;
        }

        if (levelText) levelText.text = $"Lv.{entry.level}";
        if (quizzesText) quizzesText.text = $"{entry.quizzesCompleted} quizzes";
        if (avgScoreText) avgScoreText.text = $"{entry.avgScore:F0}% avg";

        // ── Points ────────────────────────────────────────────
        if (pointsText) pointsText.text = $"{entry.totalPoints:N0}";

        // ── Highlight current user row ─────────────────────────
        if (rowBackground)
            rowBackground.color = entry.isCurrentUser
                ? new Color(0.49f, 0.23f, 0.93f, 0.08f) // light purple tint
                : Color.white;
    }

    string GetInitials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        var parts = name.Trim().Split(' ');
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
            : name[0].ToString().ToUpper();
    }
}