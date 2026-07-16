using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementsUI : MonoBehaviour
{
    [Header("Button")]
    public Button backBtn;

    [Header("Header Stats")]
    public TMP_Text headerLevelText;
    public TMP_Text headerXPText;
    public TMP_Text headerBadgesText;

    [Header("Level Card")]
    public TMP_Text levelText;
    public TMP_Text levelMessageText;
    public Slider xpProgressBar;
    public TMP_Text xpProgressText;

    [Header("Badges")]
    public Transform unlockedBadgesGrid;
    public Transform lockedBadgesGrid;
    public GameObject badgeCardPrefab;
    public TMP_Text badgeCountText;

    void OnEnable() => PopulateUI();

    void Start()
    {
        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());
    }

        void PopulateUI()
    {
        var data = PlayerSessionManager.Instance?.studentData;
        if (data == null) return;

        headerLevelText.text = data.level.ToString();
        headerXPText.text = data.totalXP.ToString();
        headerBadgesText.text = data.badgesEarned.ToString();

        levelText.text = $"Level {data.level}";
        levelMessageText.text = "Keep learning to level up!";
        float xpProgress = 1f - (data.pointsToNextLevel / 500f);
        xpProgressBar.value = Mathf.Clamp01(xpProgress);
        xpProgressText.text = $"{500 - data.pointsToNextLevel} / 500 XP";

        int unlocked = 0;
        foreach (Transform child in unlockedBadgesGrid) Destroy(child.gameObject);
        foreach (Transform child in lockedBadgesGrid) Destroy(child.gameObject);

        foreach (var badge in data.badges)
        {
            var parent = badge.isUnlocked ? unlockedBadgesGrid : lockedBadgesGrid;
            var card = Instantiate(badgeCardPrefab, parent); //
            card.GetComponent<BadgeCardUI>()?.Setup(badge);
            if (badge.isUnlocked) unlocked++;
        }
        badgeCountText.text = $"{unlocked} / {data.badges.Count}";
        }
}
