using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerSessionManager;

public class DashboardUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text welcomeText;
    public TMP_Text studentNameText;
    public Button logoutBtn;

    [Header("Level Progress Card")]
    public TMP_Text levelFromText;
    public TMP_Text levelToText;
    public Slider levelProgressBar;
    public TMP_Text pointsToNextLevelText;

    [Header("Stats")]
    public TMP_Text quizzesCompletedText;
    public TMP_Text currentLevelText;
    public TMP_Text totalPointsText;

    [Header("Quick Actions")]
    public Button explore3DBtn;
    public Button takeQuizBtn;
    public Button badgesBtn;
    public Button progressBtn;

    [Header("Recent Activity")]
    public Transform activityListParent;
    public GameObject activityItemPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        logoutBtn.onClick.AddListener(() => UIManager.Instance.LogoutToLogin());

        explore3DBtn.onClick.AddListener(() =>
            UIManager.Instance.ShowPanel(UIManager.Instance.learnPanel));

        takeQuizBtn.onClick.AddListener(() =>
            UIManager.Instance.ShowPanel(UIManager.Instance.quizSelectionPanel));

        badgesBtn.onClick.AddListener(() =>
            UIManager.Instance.NavigateTo(UIManager.Instance.achievementsPanel, 2));

        progressBtn.onClick.AddListener(() =>
            UIManager.Instance.ShowPanel(UIManager.Instance.progressPanel));
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnEnable()
    {
        // Refresh data every time panel becomes visible
        PopulateUI();
    }

    void PopulateUI()
    {
        var data = PlayerSessionManager.Instance?.studentData;
        if (data == null) return;

        welcomeText.text = "Welcome back!";
        studentNameText.text = data.studentName;

        // Level progress
        int currentLevel = data.level;
        levelFromText.text = $"Level {currentLevel}";
        levelToText.text = $"Level {currentLevel + 1}";

        // Calculate progress 0-1 within current level (assuming 500 XP per level)
        float levelProgress = 1f - (data.pointsToNextLevel / 500f);
        levelProgressBar.value = Mathf.Clamp01(levelProgress);
        pointsToNextLevelText.text = $"{data.pointsToNextLevel} points to next level";

        // Stats
        quizzesCompletedText.text = data.quizzesCompleted.ToString();
        currentLevelText.text = data.level.ToString();
        totalPointsText.text = data.totalPoints.ToString();

        // Recent Activity
        BuildActivityList(data.recentActivities);
    }

    void BuildActivityList(System.Collections.Generic.List<RecentActivity> activities)
    {
        // Clear existing
        foreach (Transform child in activityListParent)
            Destroy(child.gameObject);

        foreach (var activity in activities)
        {
            var item = Instantiate(activityItemPrefab, activityListParent);
            var ui = item.GetComponent<ActivityItemUI>();
            if (ui != null)
                ui.Setup(activity.name, activity.date, activity.score, activity.pointsEarned);
        }
    }
}
