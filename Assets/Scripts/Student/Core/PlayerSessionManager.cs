using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class StudentData
{
    public string studentName = "";
    public string email = "";
    public int level = 1;
    public int totalXP = 0;
    public int totalPoints = 0;
    public int quizzesCompleted = 0;
    public float avgScore = 0f;
    public int badgesEarned = 0;
    public int badgesTotal = 6;
    public int pointsToNextLevel = 500;

    // Classroom — set when student joins via Join Classroom screen
    public string classroomCode = "";
    public string classroomName = "";

    public Dictionary<string, float> systemProgress = new Dictionary<string, float>()
    {
        { "Skeletal System",       0f },
        { "Muscular System",       0f },
        { "Cardiovascular System", 0f },
        { "Respiratory System",    0f }
    };

    public List<RecentActivity> recentActivities = new List<RecentActivity>();
    public List<BadgeData> badges = new List<BadgeData>()
    {
        new BadgeData("Beginner",        "Complete your first quiz",      "star",   false, 0f),
        new BadgeData("Quiz Master",     "Earn high scores on quizzes",   "trophy", false, 0f),
        new BadgeData("Anatomist",       "Learn anatomy systems",         "brain",  false, 0f),
        new BadgeData("Expert",          "Earn 2,000 points to unlock",   "lock",   false, 0f),
        new BadgeData("First Steps",     "Complete your first quiz",      "star",   false, 0f),
        new BadgeData("Perfect Scholar", "Score 100% on a quiz",          "ribbon", false, 0f),
        new BadgeData("7-Day Streak",    "Study for 7 consecutive days",  "fire",   false, 0f),
    };
}

[System.Serializable]
public class RecentActivity
{
    public string name;
    public string date;
    public int score;
    public int pointsEarned;

    public RecentActivity() { }
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

    public BadgeData() { }
    public BadgeData(string t, string d, string i, bool u, float p)
    { title = t; description = d; iconKey = i; isUnlocked = u; progress = p; }
}

public class PlayerSessionManager : MonoBehaviour
{
    public static PlayerSessionManager Instance { get; private set; }

    // Firebase references
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;

    // App state
    public StudentData studentData = new StudentData();
    public bool isLoggedIn = false;
    public bool isFirebaseReady = false;

    // Events other scripts can listen to
    public event System.Action OnLoginSuccess;
    public event System.Action<string> OnLoginFailed;
    public event System.Action OnLogout;
    public event System.Action OnDataLoaded;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeFirebase();
    }

    // FIREBASE INITIALIZATION
    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                isFirebaseReady = true;

                // Auto-login if user was previously signed in
                if (auth.CurrentUser != null)
                {
                    currentUser = auth.CurrentUser;
                    LoadStudentDataFromFirestore(currentUser.UserId);
                }

                Debug.Log("[Firebase] Initialized successfully.");
            }
            else
            {
                Debug.LogError($"[Firebase] Could not resolve dependencies: {task.Result}");
            }
        });
    }

    // LOGIN - Email & Password
    public void Login(string email, string password)
    {
        if (!isFirebaseReady)
        {
            OnLoginFailed?.Invoke("Firebase is not ready. Please try again.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string error = GetAuthErrorMessage(task.Exception);
                    Debug.LogWarning($"[Firebase] Login failed: {error}");
                    OnLoginFailed?.Invoke(error);
                    return;
                }

                currentUser = task.Result.User;
                isLoggedIn = true;
                Debug.Log($"[Firebase] Logged in: {currentUser.Email}");

                LoadStudentDataFromFirestore(currentUser.UserId);
            });
    }

    //added
    //Google login
    public void HandleGoogleSignIn(string uid, string displayName,
                                string email, string photoUrl)
    {
        isLoggedIn = true;

        // Try to load existing student document
        db.Collection("students").Document(uid)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("[GoogleSignIn] Firestore load failed.");
                    NavigateToDashboard();
                    return;
                }

                if (task.Result.Exists)
                {
                    // Existing user — load their data normally
                    LoadStudentDataFromFirestore(uid);
                }
                else
                {
                    // New Google user — create their profile
                    studentData = new StudentData();
                    studentData.studentName = displayName;
                    studentData.email = email;
                    // photoUrl can be saved too if you display avatars
                    SaveStudentDataToFirestore();

                    Debug.Log($"[GoogleSignIn] New student profile created: {displayName}");
                    OnDataLoaded?.Invoke();
                    OnLoginSuccess?.Invoke();
                    NavigateToDashboard();
                }
            });
    }

    // REGISTER - Create new student account
    public void Register(string email, string password, string displayName)
    {
        if (!isFirebaseReady)
        {
            OnLoginFailed?.Invoke("Firebase is not ready.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string error = GetAuthErrorMessage(task.Exception);
                    Debug.LogWarning($"[Firebase] Registration failed: {error}");
                    OnLoginFailed?.Invoke(error);
                    return;
                }

                currentUser = task.Result.User;
                isLoggedIn = true;

                UserProfile profile = new UserProfile { DisplayName = displayName };
                currentUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(_ =>
                {
                    studentData = new StudentData();
                    studentData.studentName = displayName;
                    studentData.email = email;
                    SaveStudentDataToFirestore();
                    OnLoginSuccess?.Invoke();
                    NavigateToDashboard();
                });
            });
    }

    // LOAD student data from Firestore
    void LoadStudentDataFromFirestore(string userId)
    {
        DocumentReference docRef = db.Collection("students").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[Firestore] Load failed: {task.Exception}");
                NavigateToDashboard();
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                studentData.studentName = snapshot.GetValue<string>("studentName");
                studentData.email = snapshot.GetValue<string>("email");
                studentData.level = snapshot.GetValue<int>("level");
                studentData.totalXP = snapshot.GetValue<int>("totalXP");
                studentData.totalPoints = snapshot.GetValue<int>("totalPoints");
                studentData.quizzesCompleted = snapshot.GetValue<int>("quizzesCompleted");
                studentData.avgScore = (float)snapshot.GetValue<double>("avgScore");
                studentData.badgesEarned = snapshot.GetValue<int>("badgesEarned");
                studentData.pointsToNextLevel = snapshot.GetValue<int>("pointsToNextLevel");
                studentData.classroomCode = snapshot.ContainsField("classroomCode") ? snapshot.GetValue<string>("classroomCode") : "";
                studentData.classroomName = snapshot.ContainsField("classroomName") ? snapshot.GetValue<string>("classroomName") : "";

                // System Progress
                if (snapshot.TryGetValue("systemProgress",
                    out Dictionary<string, object> progress))
                {
                    foreach (var kvp in progress)
                        if (studentData.systemProgress.ContainsKey(kvp.Key))
                            studentData.systemProgress[kvp.Key] =
                                (float)System.Convert.ToDouble(kvp.Value);
                }

                // Recent Activities
                if (snapshot.TryGetValue("recentActivities",
                    out List<object> activities))
                {
                    studentData.recentActivities.Clear();
                    foreach (var item in activities)
                    {
                        var dict = item as Dictionary<string, object>;
                        if (dict == null) continue;
                        studentData.recentActivities.Add(new RecentActivity(
                            dict["name"].ToString(),
                            dict["date"].ToString(),
                            System.Convert.ToInt32(dict["score"]),
                            System.Convert.ToInt32(dict["pointsEarned"])
                        ));
                    }
                }

                // Badges
                if (snapshot.TryGetValue("badges", out List<object> badges))
                {
                    for (int i = 0; i < badges.Count && i < studentData.badges.Count; i++)
                    {
                        var dict = badges[i] as Dictionary<string, object>;
                        if (dict == null) continue;
                        studentData.badges[i].isUnlocked =
                            System.Convert.ToBoolean(dict["isUnlocked"]);
                        studentData.badges[i].progress =
                            (float)System.Convert.ToDouble(dict["progress"]);
                    }
                }

                Debug.Log($"[Firestore] Data loaded for: {studentData.studentName}");
            }
            else
            {
                // New user - create document with defaults
                studentData.email = currentUser.Email;
                studentData.studentName = currentUser.DisplayName ?? "Student";
                SaveStudentDataToFirestore();
                Debug.Log("[Firestore] New user document created.");
            }

            isLoggedIn = true;
            OnDataLoaded?.Invoke();
            OnLoginSuccess?.Invoke();
            NavigateToDashboard();
        });
    }

    // SAVE student data to Firestore
    public void SaveStudentDataToFirestore()
    {
        if (currentUser == null || db == null) return;

        var badgeList = new List<Dictionary<string, object>>();
        foreach (var badge in studentData.badges)
            badgeList.Add(new Dictionary<string, object>
            {
                { "title",      badge.title },
                { "isUnlocked", badge.isUnlocked },
                { "progress",   badge.progress }
            });

        var activityList = new List<Dictionary<string, object>>();
        foreach (var a in studentData.recentActivities)
            activityList.Add(new Dictionary<string, object>
            {
                { "name",         a.name },
                { "date",         a.date },
                { "score",        a.score },
                { "pointsEarned", a.pointsEarned }
            });

        var progressMap = new Dictionary<string, object>();
        foreach (var kvp in studentData.systemProgress)
            progressMap[kvp.Key] = (double)kvp.Value;

        var data = new Dictionary<string, object>
        {
            { "studentName",       studentData.studentName },
            { "email",             studentData.email },
            { "level",             studentData.level },
            { "totalXP",           studentData.totalXP },
            { "totalPoints",       studentData.totalPoints },
            { "quizzesCompleted",  studentData.quizzesCompleted },
            { "avgScore",          (double)studentData.avgScore },
            { "badgesEarned",      studentData.badgesEarned },
            { "pointsToNextLevel", studentData.pointsToNextLevel },
            { "systemProgress",    progressMap },
            { "recentActivities",  activityList },
            { "badges",            badgeList },
            { "classroomCode",     studentData.classroomCode },
            { "classroomName",     studentData.classroomName },
            { "lastUpdated",       FieldValue.ServerTimestamp }
        };

        db.Collection("students")
          .Document(currentUser.UserId)
          .SetAsync(data, SetOptions.MergeAll)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
                  Debug.LogError($"[Firestore] Save failed: {task.Exception}");
              else
                  Debug.Log("[Firestore] Student data saved.");
          });
    }

    // SAVE after a quiz - call this from QuizUI.cs
    public void SaveQuizResult(string quizName, int score, int pointsEarned)
    {
        studentData.quizzesCompleted++;
        studentData.totalPoints += pointsEarned;
        studentData.totalXP += pointsEarned;

        float total = studentData.avgScore * (studentData.quizzesCompleted - 1) + score;
        studentData.avgScore = total / studentData.quizzesCompleted;

        studentData.recentActivities.Insert(0, new RecentActivity(
            quizName,
            System.DateTime.Now.ToString("yyyy-MM-dd"),
            score,
            pointsEarned
        ));
        if (studentData.recentActivities.Count > 10)
            studentData.recentActivities.RemoveAt(10);

        CheckLevelUp();
        CheckBadgeUnlocks(score);
        SaveStudentDataToFirestore();
    }

    // LEVEL UP logic
    void CheckLevelUp()
    {
        int xpForNextLevel = studentData.level * 500;
        if (studentData.totalXP >= xpForNextLevel)
        {
            studentData.level++;
            studentData.pointsToNextLevel = (studentData.level * 500) - studentData.totalXP;
            Debug.Log($"[Level Up] Now Level {studentData.level}!");
        }
        else
        {
            studentData.pointsToNextLevel = xpForNextLevel - studentData.totalXP;
        }
    }

    // BADGE unlock logic
    void CheckBadgeUnlocks(int lastScore)
    {
        foreach (var badge in studentData.badges)
        {
            if (badge.isUnlocked) continue;

            switch (badge.title)
            {
                case "Beginner":
                case "First Steps":
                    if (studentData.quizzesCompleted >= 1) UnlockBadge(badge);
                    break;
                case "Quiz Master":
                    if (studentData.avgScore >= 85f) UnlockBadge(badge);
                    break;
                case "Perfect Scholar":
                    if (lastScore == 100) UnlockBadge(badge);
                    break;
                case "Expert":
                    badge.progress = Mathf.Clamp01(studentData.totalPoints / 2000f);
                    if (studentData.totalPoints >= 2000) UnlockBadge(badge);
                    break;
            }
        }

        int count = 0;
        foreach (var b in studentData.badges)
            if (b.isUnlocked) count++;
        studentData.badgesEarned = count;
    }

    void UnlockBadge(BadgeData badge)
    {
        badge.isUnlocked = true;
        badge.progress = 1f;
        Debug.Log($"[Badge Unlocked] {badge.title}");
    }

    // LOGOUT
    public void ClearSession()
    {
        if (auth != null) auth.SignOut();
        currentUser = null;
        isLoggedIn = false;
        studentData = new StudentData();
        OnLogout?.Invoke();
        Debug.Log("[Firebase] Signed out.");
    }

    void NavigateToDashboard()
    {
        UIManager.Instance?.ShowBottomNav(true);
        UIManager.Instance?.NavigateTo(UIManager.Instance.dashboardPanel, 0);
    }

    string GetAuthErrorMessage(System.AggregateException exception)
    {
        if (exception == null) return "Unknown error.";
        Firebase.FirebaseException fbEx = exception.GetBaseException()
            as Firebase.FirebaseException;
        if (fbEx == null) return exception.Message;

        AuthError errorCode = (AuthError)fbEx.ErrorCode;
        return errorCode switch
        {
            AuthError.WrongPassword => "Incorrect password.",
            AuthError.InvalidEmail => "Invalid email address.",
            AuthError.UserNotFound => "No account found with this email.",
            AuthError.EmailAlreadyInUse => "This email is already registered.",
            AuthError.WeakPassword => "Password must be at least 6 characters.",
            AuthError.NetworkRequestFailed => "No internet connection.",
            _ => "Authentication error. Please try again."
        };
    }
}
