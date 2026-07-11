using UnityEngine;
using System.Collections.Generic;
//using Firebase;
//using Firebase.Auth;
//using Firebase.Firestore;
//using Firebase.Extensions;


[System.Serializable]
public class AdminData
{
    public string adminName = "";
    public string email = "";
    public string role = "admin"; // "admin" | "superadmin"
}

[System.Serializable]
public class StudentRecord
{
    public string uid;
    public string fullName;
    public string email;
    public int level;
    public int totalPoints;
    public int quizzesCompleted;
    public bool isActive;
    public string classroomCode;
}

[System.Serializable]
public class QuizRecord
{
    public string quizId;
    public string title;
    public string category;       // e.g. "Skeletal System"
    public int timeLimitSecs;  // e.g. 600
    public int passingScore;   // e.g. 70 (percent)
    public int questionCount;
    public List<QuestionRecord> questions = new List<QuestionRecord>();
}

[System.Serializable]
public class QuestionRecord
{
    public string questionId;
    public string questionText;
    public string questionType;   // "Multiple Choice" | "True/False"
    public List<string> options = new List<string>();
    public string correctAnswer;
    public string difficulty;     // "Easy" | "Medium" | "Hard"
    public int points;
}

[System.Serializable]
public class BadgeRecord
{
    public string badgeId;
    public string title;
    public int pointsRequired;
    public string iconKey;
}

[System.Serializable]
public class LevelRecord
{
    public int levelNumber;
    public string title;          // e.g. "Novice"
    public int pointsRequired;
}

[System.Serializable]
public class GamificationSettings
{
    public int easyPoints = 10;
    public int mediumPoints = 20;
    public int hardPoints = 30;
    public List<BadgeRecord> badges = new List<BadgeRecord>();
    public List<LevelRecord> levels = new List<LevelRecord>();
}

[System.Serializable]
public class AnalyticsData
{
    public int activeUsers;
    public float avgScore;
    public int quizzesDone;
    public float completionRate;
    public float activeUsersDelta;
    public float avgScoreDelta;
    public float quizzesDelta;
    public float completionDelta;
    public List<TopPerformer> topPerformers = new List<TopPerformer>();
    public List<CommonMistake> commonMistakes = new List<CommonMistake>();
    public StudentActivityStats studentActivity = new StudentActivityStats();
    // Pie chart data
    public float excellentPct = 35f;
    public float goodPct = 42f;
    public float averagePct = 18f;
    public float needsWorkPct = 5f;
}

[System.Serializable]
public class TopPerformer
{
    public int rank;
    public string name;
    public int points;
    public int quizzesCompleted;
    public int level;
}

[System.Serializable]
public class CommonMistake
{
    public string questionText;
    public string category;
    public int errorCount;
}

[System.Serializable]
public class StudentActivityStats
{
    public int totalStudents;
    public int activeThisMonth;
    public float averageLevel;
    public int averagePoints;
}



public class AdminSessionManager : MonoBehaviour
{
    public static AdminSessionManager Instance { get; private set; }

    //private FirebaseAuth auth;
    //private FirebaseFirestore db;
    //private FirebaseUser currentAdmin;

    public AdminData adminData = new AdminData();
    public List<StudentRecord> students = new List<StudentRecord>();
    public List<QuizRecord> quizzes = new List<QuizRecord>();
    public GamificationSettings gamificationSettings = new GamificationSettings();
    public AnalyticsData analyticsData = new AnalyticsData();

    public bool isLoggedIn = false;
    public bool isFirebaseReady = false;

    // Events
    public event System.Action OnAdminLoginSuccess;
    public event System.Action<string> OnAdminLoginFailed;
    public event System.Action OnDataRefreshed;
    public event System.Action OnLogout;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    //void Start() => InitFirebase();

    // ── Firebase init ────────────────────────────────────────
    //void InitFirebase()
    //{
    //    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.Result == DependencyStatus.Available)
    //        {
    //            auth = FirebaseAuth.DefaultInstance;
    //            db = FirebaseFirestore.DefaultInstance;
    //            isFirebaseReady = true;
    //            if (auth.CurrentUser != null)
    //            {
    //                currentAdmin = auth.CurrentUser;
    //                VerifyAdminRole(currentAdmin.UserId);
    //            }
    //            Debug.Log("[AdminSession] Firebase ready.");
    //        }
    //        else Debug.LogError("[AdminSession] Firebase dependency error.");
    //    });
    //}

    // ── Login ────────────────────────────────────────────────
    //public void Login(string email, string password)
    //{
    //    if (!isFirebaseReady) { OnAdminLoginFailed?.Invoke("Firebase not ready."); return; }

    //    auth.SignInWithEmailAndPasswordAsync(email, password)
    //        .ContinueWithOnMainThread(task =>
    //        {
    //            if (task.IsFaulted || task.IsCanceled)
    //            {
    //                OnAdminLoginFailed?.Invoke("Invalid credentials. Please try again.");
    //                return;
    //            }
    //            currentAdmin = task.Result.User;
    //            VerifyAdminRole(currentAdmin.UserId);
    //        });
    //}

    // ── Verify the user has admin role in Firestore ──────────
    //void VerifyAdminRole(string uid)
    //{
    //    db.Collection("admins").Document(uid).GetSnapshotAsync()
    //        .ContinueWithOnMainThread(task =>
    //        {
    //            if (task.IsFaulted || !task.Result.Exists)
    //            {
    //                auth.SignOut();
    //                OnAdminLoginFailed?.Invoke("Access denied. Admin account required.");
    //                return;
    //            }
    //            var snap = task.Result;
    //            adminData.adminName = snap.ContainsField("name") ? snap.GetValue<string>("name") : "Admin";
    //            adminData.email = snap.ContainsField("email") ? snap.GetValue<string>("email") : currentAdmin.Email;
    //            adminData.role = snap.ContainsField("role") ? snap.GetValue<string>("role") : "admin";

    //            isLoggedIn = true;
    //            LoadAllData();
    //        });
    //}

    // ── Load all admin data ──────────────────────────────────
    //public void LoadAllData()
    //{
    //    LoadStudents();
    //    LoadQuizzes();
    //    LoadGamificationSettings();
    //    LoadAnalytics();
    //}

    // ── Students ─────────────────────────────────────────────
    //public void LoadStudents()
    //{
    //    db.Collection("students").GetSnapshotAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsFaulted) return;
    //        students.Clear();
    //        foreach (var doc in task.Result.Documents)
    //        {
    //            students.Add(new StudentRecord
    //            {
    //                uid = doc.Id,
    //                fullName = doc.ContainsField("studentName") ? doc.GetValue<string>("studentName") : "Unknown",
    //                email = doc.ContainsField("email") ? doc.GetValue<string>("email") : "",
    //                level = doc.ContainsField("level") ? doc.GetValue<int>("level") : 1,
    //                totalPoints = doc.ContainsField("totalPoints") ? doc.GetValue<int>("totalPoints") : 0,
    //                quizzesCompleted = doc.ContainsField("quizzesCompleted") ? doc.GetValue<int>("quizzesCompleted") : 0,
    //                isActive = doc.ContainsField("isActive") ? doc.GetValue<bool>("isActive") : true,
    //                classroomCode = doc.ContainsField("classroomCode") ? doc.GetValue<string>("classroomCode") : "",
    //            });
    //        }
    //        Debug.Log($"[AdminSession] Loaded {students.Count} students.");
    //        OnDataRefreshed?.Invoke();
    //    });
    //}

    //public void ToggleStudentStatus(string uid, bool setActive)
    //{
    //    db.Collection("students").Document(uid)
    //        .UpdateAsync("isActive", setActive)
    //        .ContinueWithOnMainThread(_ => LoadStudents());
    //}

    //public void DeleteStudent(string uid)
    //{
    //    db.Collection("students").Document(uid)
    //        .DeleteAsync()
    //        .ContinueWithOnMainThread(_ => LoadStudents());
    //}

    //// ── Quizzes ──────────────────────────────────────────────
    //public void LoadQuizzes()
    //{
    //    db.Collection("quizzes").GetSnapshotAsync().ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsFaulted) return;
    //        quizzes.Clear();
    //        foreach (var doc in task.Result.Documents)
    //        {
    //            quizzes.Add(new QuizRecord
    //            {
    //                quizId = doc.Id,
    //                title = doc.ContainsField("title") ? doc.GetValue<string>("title") : "Untitled",
    //                category = doc.ContainsField("category") ? doc.GetValue<string>("category") : "",
    //                timeLimitSecs = doc.ContainsField("timeLimitSecs") ? doc.GetValue<int>("timeLimitSecs") : 600,
    //                passingScore = doc.ContainsField("passingScore") ? doc.GetValue<int>("passingScore") : 70,
    //                questionCount = doc.ContainsField("questionCount") ? doc.GetValue<int>("questionCount") : 0,
    //            });
    //        }
    //        OnDataRefreshed?.Invoke();
    //    });
    //}

    //public void CreateQuiz(QuizRecord quiz, System.Action<string> onSuccess)
    //{
    //    var data = new Dictionary<string, object>
    //    {
    //        { "title",         quiz.title },
    //        { "category",      quiz.category },
    //        { "timeLimitSecs", quiz.timeLimitSecs },
    //        { "passingScore",  quiz.passingScore },
    //        { "questionCount", 0 },
    //        { "createdAt",     FieldValue.ServerTimestamp }
    //    };
    //    db.Collection("quizzes").AddAsync(data).ContinueWithOnMainThread(task =>
    //    {
    //        if (!task.IsFaulted) { LoadQuizzes(); onSuccess?.Invoke(task.Result.Id); }
    //    });
    //}

    //public void DeleteQuiz(string quizId)
    //{
    //    db.Collection("quizzes").Document(quizId)
    //        .DeleteAsync()
    //        .ContinueWithOnMainThread(_ => LoadQuizzes());
    //}

    //public void AddQuestion(string quizId, QuestionRecord q, System.Action onDone)
    //{
    //    var data = new Dictionary<string, object>
    //    {
    //        { "questionText",  q.questionText },
    //        { "questionType",  q.questionType },
    //        { "options",       q.options },
    //        { "correctAnswer", q.correctAnswer },
    //        { "difficulty",    q.difficulty },
    //        { "points",        q.points }
    //    };
    //    db.Collection("quizzes").Document(quizId)
    //        .Collection("questions").AddAsync(data)
    //        .ContinueWithOnMainThread(task =>
    //        {
    //            if (!task.IsFaulted)
    //            {
    //                // Increment question count
    //                db.Collection("quizzes").Document(quizId)
    //                    .UpdateAsync("questionCount", FieldValue.Increment(1));
    //                onDone?.Invoke();
    //            }
    //        });
    //}

    //// ── Gamification ─────────────────────────────────────────
    //public void LoadGamificationSettings()
    //{
    //    db.Collection("settings").Document("gamification")
    //        .GetSnapshotAsync().ContinueWithOnMainThread(task =>
    //        {
    //            if (task.IsFaulted || !task.Result.Exists) return;
    //            var snap = task.Result;
    //            gamificationSettings.easyPoints = snap.ContainsField("easyPoints") ? snap.GetValue<int>("easyPoints") : 10;
    //            gamificationSettings.mediumPoints = snap.ContainsField("mediumPoints") ? snap.GetValue<int>("mediumPoints") : 20;
    //            gamificationSettings.hardPoints = snap.ContainsField("hardPoints") ? snap.GetValue<int>("hardPoints") : 30;

    //            // Load badges sub-collection
    //            db.Collection("settings").Document("gamification")
    //                .Collection("badges").GetSnapshotAsync()
    //                .ContinueWithOnMainThread(bt =>
    //                {
    //                    if (bt.IsFaulted) return;
    //                    gamificationSettings.badges.Clear();
    //                    foreach (var doc in bt.Result.Documents)
    //                        gamificationSettings.badges.Add(new BadgeRecord
    //                        {
    //                            badgeId = doc.Id,
    //                            title = doc.GetValue<string>("title"),
    //                            pointsRequired = doc.GetValue<int>("pointsRequired"),
    //                            iconKey = doc.ContainsField("iconKey") ? doc.GetValue<string>("iconKey") : "star"
    //                        });

    //                    // Load levels sub-collection
    //                    db.Collection("settings").Document("gamification")
    //                    .Collection("levels").GetSnapshotAsync()
    //                    .ContinueWithOnMainThread(lt =>
    //                    {
    //                        if (lt.IsFaulted) return;
    //                        gamificationSettings.levels.Clear();
    //                        foreach (var doc in lt.Result.Documents)
    //                            gamificationSettings.levels.Add(new LevelRecord
    //                            {
    //                                levelNumber = doc.GetValue<int>("levelNumber"),
    //                                title = doc.GetValue<string>("title"),
    //                                pointsRequired = doc.GetValue<int>("pointsRequired")
    //                            });
    //                        gamificationSettings.levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
    //                        OnDataRefreshed?.Invoke();
    //                    });
    //                });
    //        });
    //}

    //public void SavePointsConfig(int easy, int medium, int hard)
    //{
    //    var data = new Dictionary<string, object>
    //        { { "easyPoints", easy }, { "mediumPoints", medium }, { "hardPoints", hard } };
    //    db.Collection("settings").Document("gamification").SetAsync(data, SetOptions.MergeAll);
    //    gamificationSettings.easyPoints = easy;
    //    gamificationSettings.mediumPoints = medium;
    //    gamificationSettings.hardPoints = hard;
    //}

    //public void AddBadge(BadgeRecord badge, System.Action onDone)
    //{
    //    var data = new Dictionary<string, object>
    //    {
    //        { "title", badge.title }, { "pointsRequired", badge.pointsRequired }, { "iconKey", badge.iconKey }
    //    };
    //    db.Collection("settings").Document("gamification")
    //        .Collection("badges").AddAsync(data)
    //        .ContinueWithOnMainThread(_ => { LoadGamificationSettings(); onDone?.Invoke(); });
    //}

    //public void DeleteBadge(string badgeId, System.Action onDone)
    //{
    //    db.Collection("settings").Document("gamification")
    //        .Collection("badges").Document(badgeId).DeleteAsync()
    //        .ContinueWithOnMainThread(_ => { LoadGamificationSettings(); onDone?.Invoke(); });
    //}

    //public void AddLevel(LevelRecord level, System.Action onDone)
    //{
    //    var data = new Dictionary<string, object>
    //    {
    //        { "levelNumber", level.levelNumber }, { "title", level.title }, { "pointsRequired", level.pointsRequired }
    //    };
    //    db.Collection("settings").Document("gamification")
    //        .Collection("levels").AddAsync(data)
    //        .ContinueWithOnMainThread(_ => { LoadGamificationSettings(); onDone?.Invoke(); });
    //}

    //public void DeleteLevel(string levelDocId, System.Action onDone)
    //{
    //    db.Collection("settings").Document("gamification")
    //        .Collection("levels").Document(levelDocId).DeleteAsync()
    //        .ContinueWithOnMainThread(_ => { LoadGamificationSettings(); onDone?.Invoke(); });
    //}

    // ── Analytics ────────────────────────────────────────────
    public void LoadAnalytics()
    {
        // Aggregate from students collection
        int active = 0; int totalQ = 0; float totalScore = 0;
        var performers = new List<TopPerformer>();

        foreach (var s in students)
        {
            if (s.isActive) active++;
            totalQ += s.quizzesCompleted;
            totalScore += s.totalPoints;
            performers.Add(new TopPerformer
            {
                name = s.fullName,
                points = s.totalPoints,
                quizzesCompleted = s.quizzesCompleted,
                level = s.level
            });
        }

        performers.Sort((a, b) => b.points.CompareTo(a.points));
        for (int i = 0; i < performers.Count; i++) performers[i].rank = i + 1;

        analyticsData.activeUsers = active;
        analyticsData.quizzesDone = totalQ;
        analyticsData.topPerformers = performers;
        analyticsData.studentActivity = new StudentActivityStats
        {
            totalStudents = students.Count,
            activeThisMonth = active,
            averageLevel = students.Count > 0
                ? (float)students.ConvertAll(s => s.level).FindAll(l => l > 0).Count / students.Count * 5f
                : 0f,
            averagePoints = students.Count > 0 ? (int)(totalScore / students.Count) : 0
        };

        // Avg score — load from quiz results (simplified)
        analyticsData.avgScore = 82f;       // Replace with real Firestore aggregation
        analyticsData.completionRate = 78f; // Replace with real data
        analyticsData.activeUsersDelta = 12f;
        analyticsData.avgScoreDelta = 5f;
        analyticsData.quizzesDelta = 18f;
        analyticsData.completionDelta = 3f;

        OnDataRefreshed?.Invoke();
    }

    // ── Create Classroom ─────────────────────────────────────
    //public void CreateClassroom(string name, string description, System.Action<string> onSuccess)
    //{
    //    string code = GenerateClassroomCode();
    //    var data = new Dictionary<string, object>
    //    {
    //        { "className",   name },
    //        { "description", description },
    //        { "adminId",     currentAdmin?.UserId ?? "" },
    //        { "createdAt",   FieldValue.ServerTimestamp },
    //        { "students",    new List<string>() }
    //    };
    //    db.Collection("classrooms").Document(code).SetAsync(data)
    //        .ContinueWithOnMainThread(task =>
    //        {
    //            if (!task.IsFaulted) onSuccess?.Invoke(code);
    //        });
    //}

    string GenerateClassroomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rand = new System.Random();
        char[] code = new char[7];
        for (int i = 0; i < 7; i++)
            code[i] = chars[rand.Next(chars.Length)];
        return new string(code);
    }

    // ── Logout ───────────────────────────────────────────────
    //public void Logout()
    //{
    //    auth?.SignOut();
    //    isLoggedIn = false;
    //    adminData = new AdminData();
    //    OnLogout?.Invoke();
    //}
}
