using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    // LEADERBOARD DATA MODEL
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string uid;
        public string studentName;
        public int totalPoints;
        public int level;
        public int quizzesCompleted;
        public float avgScore;
        public string classroomCode;
        public bool isCurrentUser;
    }

    [Header("Header")]
    public Button backBtn;
    public TMP_Text subtitleText;

    [Header("Filter Buttons")]
    public Button globalBtn;
    public Button classroomBtn;
    public Image globalBtnBG;
    public Image classroomBtnBG;
    public TMP_Text globalBtnText;
    public TMP_Text classroomBtnText;

    [Header("Top 3 Podium")]
    public GameObject topThreeSection;

    [Header("First Place")]
    public TMP_Text firstNameText;
    public TMP_Text firstPointsText;
    public TMP_Text firstAvatarText;
    public Image firstAvatarBG;

    [Header("Second Place")]
    public TMP_Text secondNameText;
    public TMP_Text secondPointsText;
    public TMP_Text secondAvatarText;
    public Image secondAvatarBG;

    [Header("Third Place")]
    public TMP_Text thirdNameText;
    public TMP_Text thirdPointsText;
    public TMP_Text thirdAvatarText;
    public Image thirdAvatarBG;

    [Header("Current User Card")]
    public GameObject currentUserCard;
    public TMP_Text yourRankText;
    public TMP_Text yourNameText;
    public TMP_Text yourPointsText;
    public TMP_Text yourLevelText;

    [Header("Leaderboard List")]
    public Transform listParent;
    public GameObject leaderboardRowPrefab;

    [Header("Loading")]
    public GameObject loadingOverlay;
    public GameObject emptyStateText;

    // Filter state
    private bool isGlobalMode = true;

    // Colors
    private Color activeFilterColor = new Color(0.49f, 0.23f, 0.93f); // purple
    private Color inactiveFilterColor = new Color(0.93f, 0.93f, 0.93f); // light grey
    private Color activeTextColor = Color.white;
    private Color inactiveTextColor = new Color(0.40f, 0.40f, 0.40f);

    // Podium colors
    private Color goldColor = new Color(1.00f, 0.84f, 0.00f);
    private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    private Color bronzeColor = new Color(0.80f, 0.50f, 0.20f);

    private List<LeaderboardEntry> currentEntries = new List<LeaderboardEntry>();
    private FirebaseFirestore db;

    // ─────────────────────────────────────────────────────────
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());
        globalBtn.onClick.AddListener(() => SwitchFilter(true));
        classroomBtn.onClick.AddListener(() => SwitchFilter(false));

        // Check if student is in a classroom
        var data = PlayerSessionManager.Instance?.studentData;
        bool inClassroom = !string.IsNullOrEmpty(data?.classroomCode);
        classroomBtn.interactable = inClassroom;
        if (!inClassroom && classroomBtnText)
            classroomBtnText.text = "My Classroom\n(Not joined)";
    }

    void OnEnable()
    {
        SwitchFilter(true); // Default to global
    }

    // ── Filter Switch ─────────────────────────────────────────
    void SwitchFilter(bool global)
    {
        isGlobalMode = global;

        // Style active/inactive filter buttons
        globalBtnBG.color = global ? activeFilterColor : inactiveFilterColor;
        classroomBtnBG.color = global ? inactiveFilterColor : activeFilterColor;
        globalBtnText.color = global ? activeTextColor : inactiveTextColor;
        classroomBtnText.color = global ? inactiveTextColor : activeTextColor;

        subtitleText.text = global
            ? "Top students globally"
            : "Top students in your classroom";

        LoadLeaderboard(global);
    }

    // ── Load Leaderboard from Firestore ───────────────────────
    void LoadLeaderboard(bool global)
    {
        SetLoading(true);

        var data = PlayerSessionManager.Instance?.studentData;

        Query query;

        if (global)
        {
            // Global — top 20 students by points
            query = db.Collection("students")
                      .OrderByDescending("totalPoints")
                      .Limit(20);
        }
        else
        {
            // Classroom — filter by classroom code
            string code = data?.classroomCode ?? "";
            if (string.IsNullOrEmpty(code))
            {
                SetLoading(false);
                ShowEmptyState("You haven't joined a classroom yet.");
                return;
            }
            query = db.Collection("students")
                      .WhereEqualTo("classroomCode", code)
                      .OrderByDescending("totalPoints")
                      .Limit(20);
        }

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            SetLoading(false);

            if (task.IsFaulted)
            {
                ShowEmptyState("Failed to load leaderboard. Check connection.");
                Debug.LogError("[Leaderboard] " + task.Exception);
                return;
            }

            var entries = new List<LeaderboardEntry>();
            int rank = 1;
            string myUid = Firebase.Auth.FirebaseAuth.DefaultInstance
                           .CurrentUser?.UserId ?? "";

            foreach (var doc in task.Result.Documents)
            {
                entries.Add(new LeaderboardEntry
                {
                    rank = rank++,
                    uid = doc.Id,
                    studentName = doc.ContainsField("studentName")
                                       ? doc.GetValue<string>("studentName") : "Student",
                    totalPoints = doc.ContainsField("totalPoints")
                                       ? doc.GetValue<int>("totalPoints") : 0,
                    level = doc.ContainsField("level")
                                       ? doc.GetValue<int>("level") : 1,
                    quizzesCompleted = doc.ContainsField("quizzesCompleted")
                                       ? doc.GetValue<int>("quizzesCompleted") : 0,
                    avgScore = doc.ContainsField("avgScore")
                                       ? (float)doc.GetValue<double>("avgScore") : 0f,
                    classroomCode = doc.ContainsField("classroomCode")
                                       ? doc.GetValue<string>("classroomCode") : "",
                    isCurrentUser = doc.Id == myUid
                });
            }

            if (entries.Count == 0)
            {
                ShowEmptyState("No students found yet.");
                return;
            }

            currentEntries = entries;
            PopulateLeaderboard(entries, myUid, data);
        });
    }

    // ── Populate UI ──────────────────────────────────────────
    void PopulateLeaderboard(List<LeaderboardEntry> entries,
                              string myUid, StudentData myData)
    {
        // ── Top 3 Podium ─────────────────────────────────────
        bool hasTop3 = entries.Count >= 3;
        topThreeSection?.SetActive(hasTop3);

        if (hasTop3)
        {
            SetPodiumEntry(1, entries[0], firstNameText,
                firstPointsText, firstAvatarText, firstAvatarBG, goldColor);
            SetPodiumEntry(2, entries[1], secondNameText,
                secondPointsText, secondAvatarText, secondAvatarBG, silverColor);
            SetPodiumEntry(3, entries[2], thirdNameText,
                thirdPointsText, thirdAvatarText, thirdAvatarBG, bronzeColor);
        }
        else if (entries.Count >= 1)
        {
            topThreeSection?.SetActive(false);
        }

        // ── Current User Card ─────────────────────────────────
        var myEntry = entries.Find(e => e.uid == myUid);
        if (myEntry != null && currentUserCard != null)
        {
            currentUserCard.SetActive(true);
            yourRankText.text = $"#{myEntry.rank}";
            yourNameText.text = myEntry.studentName + " (You)";
            yourPointsText.text = $"{myEntry.totalPoints:N0} pts";
            if (yourLevelText)
                yourLevelText.text = $"Level {myEntry.level}";

            // Highlight card purple if in top 3
            var cardImg = currentUserCard.GetComponent<Image>();
            if (cardImg)
                cardImg.color = myEntry.rank <= 3
                    ? new Color(0.49f, 0.23f, 0.93f, 0.15f)
                    : new Color(0.49f, 0.23f, 0.93f, 0.08f);
        }
        else if (currentUserCard != null)
        {
            // Current user not in list — show with local data
            currentUserCard.SetActive(myData != null);
            if (myData != null)
            {
                yourRankText.text = "#--";
                yourNameText.text = $"{myData.studentName} (You)";
                yourPointsText.text = $"{myData.totalPoints:N0} pts";
                if (yourLevelText)
                    yourLevelText.text = $"Level {myData.level}";
            }
        }

        // ── Main list (starts from rank 4 or 1 if no podium) ──
        BuildList(entries);
    }

    void SetPodiumEntry(int rank, LeaderboardEntry entry,
        TMP_Text nameText, TMP_Text pointsText,
        TMP_Text avatarText, Image avatarBG, Color color)
    {
        if (nameText) nameText.text = ShortenName(entry.studentName);
        if (pointsText) pointsText.text = $"{entry.totalPoints:N0} pts";
        if (avatarText) avatarText.text = GetInitials(entry.studentName);
        if (avatarBG) avatarBG.color = color;
    }

    void BuildList(List<LeaderboardEntry> entries)
    {
        foreach (Transform child in listParent) Destroy(child.gameObject);

        // Show all entries in the scrollable list
        // (top 3 still appear in podium above)
        foreach (var entry in entries)
        {
            var row = Instantiate(leaderboardRowPrefab, listParent);
            row.GetComponent<LeaderboardRowUI>()?.Setup(entry);
        }
    }

    // ── Helpers ──────────────────────────────────────────────
    void SetLoading(bool loading)
    {
        if (loadingOverlay) loadingOverlay.SetActive(loading);
        if (emptyStateText) emptyStateText.SetActive(false);
    }

    void ShowEmptyState(string msg)
    {
        if (loadingOverlay) loadingOverlay.SetActive(false);
        if (emptyStateText)
        {
            emptyStateText.SetActive(true);
            emptyStateText.GetComponent<TMP_Text>().text = msg;
        }
        topThreeSection?.SetActive(false);
        foreach (Transform child in listParent) Destroy(child.gameObject);
    }

    string GetInitials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        var parts = name.Trim().Split(' ');
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        return name.Length > 0 ? name[0].ToString().ToUpper() : "?";
    }

    string ShortenName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Student";
        var parts = name.Trim().Split(' ');
        return parts.Length >= 2
            ? $"{parts[0]} {parts[1][0]}."
            : parts[0];
    }
}

