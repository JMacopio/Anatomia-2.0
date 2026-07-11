using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BadgeCardUI : MonoBehaviour
{
    public Image badgeBackground;    // orange gradient when unlocked, grey when locked
    public Image badgeIcon;
    public TMP_Text badgeTitleText;
    public TMP_Text badgeDescText;
    public GameObject unlockedBadge; // "✓ Unlocked" green tag
    public Slider progressBar;       // shown when locked + partial progress
    public TMP_Text progressText;    // "1250 / 2000"
    public TMP_Text progressPercent; // "63%"

    public Color unlockedBgColor = new Color(1f, 0.65f, 0f);  // orange
    public Color lockedBgColor = new Color(0.7f, 0.7f, 0.7f);

    public void Setup(BadgeData badge)
    {
        badgeTitleText.text = badge.title;
        if (badgeDescText) badgeDescText.text = badge.description;

        if (badge.isUnlocked)
        {
            badgeBackground.color = unlockedBgColor;
            unlockedBadge?.SetActive(true);
            progressBar?.gameObject.SetActive(false);
            progressText?.gameObject.SetActive(false);
            progressPercent?.gameObject.SetActive(false);
        }
        else
        {
            badgeBackground.color = lockedBgColor;
            unlockedBadge?.SetActive(false);
            bool hasProgress = badge.progress > 0f && badge.progress < 1f;
            progressBar?.gameObject.SetActive(hasProgress);
            if (hasProgress && progressBar != null)
            {
                progressBar.value = badge.progress;
                if (progressPercent) progressPercent.text = $"{Mathf.RoundToInt(badge.progress * 100)}%";
            }
        }
    }
}
