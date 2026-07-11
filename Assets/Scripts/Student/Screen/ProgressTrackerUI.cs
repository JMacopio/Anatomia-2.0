using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressTrackerUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;

    [Header("Level Progress Card")]
    public TMP_Text currentLevelText;
    public TMP_Text currentLevelTitle;
    public TMP_Text nextLevelText;
    public TMP_Text nextLevelTitle;
    public Slider levelProgressBar;
    public TMP_Text pointsToNextText;

    [Header("Stats Grid")]
    public TMP_Text quizzesCompletedText;
    public TMP_Text avgScoreText;
    public TMP_Text totalPointsText;
    public TMP_Text badgesEarnedText;

    [Header("Tabs")]
    public Button weeklyTab;
    public Button performanceTab;
    public GameObject weeklyChartPanel;
    public GameObject performanceChartPanel;
    public Image weeklyTabIndicator;
    public Image performanceTabIndicator;

    private static readonly string[] levelTitles =
        { "Beginner", "Learner", "Intermediate", "Advanced", "Expert", "Master", "Legend" };

    void Start()
    {
        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());
        weeklyTab.onClick.AddListener(() => SwitchTab(true));
        performanceTab.onClick.AddListener(() => SwitchTab(false));
    }

    void OnEnable() => PopulateUI();

    void PopulateUI()
    {
        var data = PlayerSessionManager.Instance?.studentData;
        if (data == null) return;

        int lvl = data.level;
        currentLevelText.text = $"Level {lvl}";
        currentLevelTitle.text = GetLevelTitle(lvl);
        nextLevelText.text = $"Level {lvl + 1}";
        nextLevelTitle.text = GetLevelTitle(lvl + 1);

        float progress = 1f - (data.pointsToNextLevel / 500f);
        levelProgressBar.value = Mathf.Clamp01(progress);
        pointsToNextText.text = $"{data.pointsToNextLevel} points to next level";

        quizzesCompletedText.text = data.quizzesCompleted.ToString();
        avgScoreText.text = $"{data.avgScore}%";
        totalPointsText.text = data.totalPoints.ToString();
        badgesEarnedText.text = data.badgesEarned.ToString();

        SwitchTab(true);
    }

    void SwitchTab(bool showWeekly)
    {
        weeklyChartPanel.SetActive(showWeekly);
        performanceChartPanel.SetActive(!showWeekly);
        weeklyTabIndicator.color = showWeekly
            ? new Color(0.5f, 0.2f, 0.9f) : Color.clear;
        performanceTabIndicator.color = !showWeekly
            ? new Color(0.5f, 0.2f, 0.9f) : Color.clear;
    }

    string GetLevelTitle(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, levelTitles.Length - 1);
        return levelTitles[idx];
    }
}
