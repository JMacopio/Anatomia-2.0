using System.Collections.Generic;
using UnityEngine;

public class PlayerSessionManager : MonoBehaviour
{
    [System.Serializable]
    public class StudentData
    {
        public string studentName = "Jeysi";
        public string email = "student@anatomia.com";
        public int level = 5;
        public int totalXP = 2850;
        public int totalPoints = 1250;
        public int quizzesCompleted = 24;
        public float avgScore = 87f;
        public int badgesEarned = 3;
        public int badgesTotal = 6;
        public int pointsToNextLevel = 350;

        public Dictionary<string, float> systemProgress = new Dictionary<string, float>()
    {
        { "Skeletal System", 0.75f },
        { "Muscular System", 0.45f },
        { "Cardiovascular System", 0.60f },
        { "Respiratory System", 0.30f }
    };

        public List<RecentActivity> recentActivities = new List<RecentActivity>()
    {
        new RecentActivity("Skeletal System Quiz", "2026-03-10", 85, 85),
        new RecentActivity("Muscular System Quiz", "2026-03-09", 92, 92),
        new RecentActivity("Nervous System Quiz", "2026-03-08", 78, 78)
    };

        public List<BadgeData> badges = new List<BadgeData>()
    {
        new BadgeData("Beginner", "Complete your first quiz", "star", true, 1f),
        new BadgeData("Quiz Master", "Earn high scores on quizzes", "trophy", true, 1f),
        new BadgeData("Anatomist", "Learn anatomy systems", "brain", true, 1f),
        new BadgeData("Expert", "Earn 2,000 points to unlock", "lock", false, 0.63f),
        new BadgeData("First Steps", "Complete your first quiz", "star", true, 1f),
        new BadgeData("Perfect Scholar", "Score 100% on a quiz", "ribbon", true, 1f),
        new BadgeData("7-Day Streak", "Study for 7 consecutive days", "fire", true, 1f),
    };
    }

    [System.Serializable]
    public class RecentActivity
    {
        public string name;
        public string date;
        public int score;
        public int pointsEarned;

        public RecentActivity(string n, string d, int s, int p)
        { name = n; date = d; score = s; pointsEarned = p; }
    }

    [System.Serializable]
    public class BadgeData
    {
        public string title;
        public string description;
        public string iconKey;
        public bool isUnlocked;
        public float progress;

        public BadgeData(string t, string d, string i, bool u, float p)
        { title = t; description = d; iconKey = i; isUnlocked = u; progress = p; }
    }

    public static PlayerSessionManager Instance { get; private set; }
    public StudentData studentData = new StudentData();
    public bool isLoggedIn = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void Login(string email, string password)
    {
        // In production: call your backend API
        isLoggedIn = true;
        studentData.email = email;
        UIManager.Instance.ShowBottomNav(true);
        UIManager.Instance.NavigateTo(
        UIManager.Instance.dashboardPanel, 0);
    }

    public void ClearSession()
    {
        isLoggedIn = false;
    }

}
