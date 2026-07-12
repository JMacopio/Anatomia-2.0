using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminDashboardUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text adminNameText;
    public Button logoutBtn;

    [Header("Stats Row")]
    public TMP_Text activeUsersText;
    public TMP_Text totalQuizzesText;

    [Header("Management Buttons")]
    public Button userManagementBtn;
    public Button quizManagementBtn;
    public Button gamificationBtn;
    public Button analyticsBtn;
    public Button createClassroomBtn;

    [Header("Pie Chart")]
    public PieChartRenderer pieChart;
    public TMP_Text excellentPctText;
    public TMP_Text goodPctText;
    public TMP_Text averagePctText;
    public TMP_Text needsWorkPctText;

    void Start()
    {
        //Header
        logoutBtn.onClick.AddListener(OnLogout);

        //Management Buttons
        userManagementBtn.onClick.AddListener(() => AdminUIManager.Instance.OpenUserManagement());
        quizManagementBtn.onClick.AddListener(() => AdminUIManager.Instance.OpenQuizManagement());
        gamificationBtn.onClick.AddListener(() => AdminUIManager.Instance.OpenGamification());
        analyticsBtn.onClick.AddListener(() => AdminUIManager.Instance.OpenAnalytics());
        createClassroomBtn.onClick.AddListener(() => AdminUIManager.Instance.OpenCreateClassroom());

        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed += Refresh;
    }

    void OnEnable() => Refresh();

    public void Refresh()
    {
        var session = AdminSessionManager.Instance;
        if (session == null) return;

        adminNameText.text = $"Welcome back, {session.adminData.adminName}";
        activeUsersText.text = session.analyticsData.activeUsers.ToString();
        totalQuizzesText.text = session.quizzes.Count.ToString();

        // Pie chart
        var a = session.analyticsData;
        pieChart?.SetData(
            new float[] { a.excellentPct, a.goodPct, a.averagePct, a.needsWorkPct },
            new Color[]
            {
                new Color(0.20f, 0.78f, 0.35f), // green  — Excellent
                new Color(0.23f, 0.51f, 0.96f), // blue   — Good
                new Color(0.95f, 0.61f, 0.07f), // amber  — Average
                new Color(0.94f, 0.27f, 0.27f)  // red    — Needs Work
            }
        );

        if (excellentPctText) excellentPctText.text = $"{a.excellentPct}%";
        if (goodPctText) goodPctText.text = $"{a.goodPct}%";
        if (averagePctText) averagePctText.text = $"{a.averagePct}%";
        if (needsWorkPctText) needsWorkPctText.text = $"{a.needsWorkPct}%";
    }

    void OnLogout()
    {
        //AdminSessionManager.Instance?.Logout();
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed -= Refresh;
    }
}


// ─────────────────────────────────────────────────────────────
// Simple Pie Chart Renderer using Unity UI Image (filled)
// Attach to an empty GO. Add one Image child per slice,
// each set to Image Type = Filled, Fill Method = Radial360
// ─────────────────────────────────────────────────────────────
public class PieChartRenderer : MonoBehaviour
{
    [System.Serializable]
    public class PieSlice
    {
        public Image sliceImage;
        public TMP_Text labelText;
    }

    public PieSlice[] slices;   // assign 4 slices in Inspector

    public void SetData(float[] percentages, Color[] colors)
    {
        if (slices == null || percentages == null) return;
        float rotation = 0f;
        for (int i = 0; i < slices.Length && i < percentages.Length; i++)
        {
            float fill = percentages[i] / 100f;
            slices[i].sliceImage.fillAmount = fill;
            slices[i].sliceImage.color = colors[i];

            // Rotate each slice to start where the last one ended
            slices[i].sliceImage.transform.localEulerAngles = new Vector3(0, 0, -rotation * 360f);
            rotation += fill;
            if (slices[i].labelText)
                slices[i].labelText.text = $"{percentages[i]}%";
        }
    }
}

