using System.Collections.Generic;
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

    [Header("Level Progress (inside Header)")]
    public TMP_Text levelFromText;
    public TMP_Text levelToText;
    public Slider levelProgressBar;
    public TMP_Text pointsToNextLevelText;

    [Header("Join Classroom Card")]
    public Button joinBtn;
    public TMP_Text joinTitleText;
    public TMP_Text joinSubText;

    [Header("Stats Row")]
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

    void OnEnable() => PopulateUI();

    void Start()
    {
        logoutBtn.onClick.AddListener(() => UIManager.Instance.LogoutToLogin());
        joinBtn.onClick.AddListener(() => UIManager.Instance.OpenJoinClassroom());
        explore3DBtn.onClick.AddListener(() => UIManager.Instance.ShowPanel(UIManager.Instance.learnPanel));
        takeQuizBtn.onClick.AddListener(() => UIManager.Instance.ShowPanel(UIManager.Instance.quizSelectionPanel));
        badgesBtn.onClick.AddListener(() => UIManager.Instance.NavigateTo(UIManager.Instance.achievementsPanel, 2));
        progressBtn.onClick.AddListener(() => UIManager.Instance.ShowPanel(UIManager.Instance.progressPanel));
    }

    void PopulateUI()
    {
        var data = PlayerSessionManager.Instance?.studentData;
        if (data == null) return;

        // Header
        welcomeText.text = "Welcome back!";
        studentNameText.text = data.studentName;

        // Level progress
        levelFromText.text = $"Level {data.level}";
        levelToText.text = $"Level {data.level + 1}";
        levelProgressBar.value = Mathf.Clamp01(1f - (data.pointsToNextLevel / 500f));
        pointsToNextLevelText.text = $"{data.pointsToNextLevel} points to next level";

        // Join Classroom card
        bool inClassroom = !string.IsNullOrEmpty(data.classroomCode);
        joinTitleText.text = inClassroom ? data.classroomName : "Join a Classroom";
        joinSubText.text = inClassroom ? $"Code: {data.classroomCode}" : "Connect with your teacher";
        joinBtn.GetComponentInChildren<TMP_Text>().text = inClassroom ? "Switch" : "Join";

        // Stats
        quizzesCompletedText.text = data.quizzesCompleted.ToString();
        currentLevelText.text = data.level.ToString();
        totalPointsText.text = data.totalPoints.ToString();

        // Recent Activity
        BuildActivityList(data.recentActivities);
    }

    void BuildActivityList(List<RecentActivity> activities)
    {
        foreach (Transform child in activityListParent)
            Destroy(child.gameObject);

        foreach (var activity in activities)
        {
            var item = Instantiate(activityItemPrefab, activityListParent);
            item.GetComponent<ActivityItemUI>()
                ?.Setup(activity.name, activity.date, activity.score, activity.pointsEarned);
        }
    }
}

