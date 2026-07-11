using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminAnalyticsUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;
    public Button exportExcelBtn;
    public Button exportPDFBtn;

    [Header("Stats Cards")]
    public TMP_Text activeUsersText;
    public TMP_Text activeUsersDeltaText;
    public TMP_Text avgScoreText;
    public TMP_Text avgScoreDeltaText;
    public TMP_Text quizzesDoneText;
    public TMP_Text quizzesDeltaText;
    public TMP_Text completionText;
    public TMP_Text completionDeltaText;

    [Header("Tabs")]
    public Button performanceTab;
    public Button studentsTab;
    public Button mistakesTab;
    public GameObject performancePanel;
    public GameObject studentsPanel;
    public GameObject mistakesPanel;
    public Image performanceTabIndicator;
    public Image studentsTabIndicator;
    public Image mistakesTabIndicator;

    [Header("Students Tab")]
    public Transform topPerformersParent;
    public GameObject topPerformerRowPrefab;
    public TMP_Text totalStudentsText;
    public TMP_Text activeThisMonthText;
    public TMP_Text averageLevelText;
    public TMP_Text averagePointsText;

    [Header("Mistakes Tab")]
    public Transform mistakesListParent;
    public GameObject mistakeRowPrefab;
    public GameObject recommendationCard;
    public TMP_Text recommendationText;

    private Color activeTabColor = new Color(0.13f, 0.13f, 0.13f);
    private Color inactiveTabColor = new Color(0.87f, 0.87f, 0.87f);

    void Start()
    {
        backBtn.onClick.AddListener(() => AdminUIManager.Instance.GoBack());
        exportExcelBtn.onClick.AddListener(() => ExportToExcel());
        exportPDFBtn.onClick.AddListener(() => ExportToPDF());
        performanceTab.onClick.AddListener(() => SwitchTab(0));
        studentsTab.onClick.AddListener(() => SwitchTab(1));
        mistakesTab.onClick.AddListener(() => SwitchTab(2));

        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed += Refresh;
    }

    void OnEnable()
    {
        AdminSessionManager.Instance?.LoadAnalytics();
        Refresh();
        SwitchTab(0);
    }

    public void Refresh()
    {
        var a = AdminSessionManager.Instance?.analyticsData;
        if (a == null) return;

        // Stats cards
        SetStat(activeUsersText, activeUsersDeltaText, a.activeUsers.ToString(), a.activeUsersDelta);
        SetStat(avgScoreText, avgScoreDeltaText, $"{a.avgScore}%", a.avgScoreDelta);
        SetStat(quizzesDoneText, quizzesDeltaText, a.quizzesDone.ToString(), a.quizzesDelta);
        SetStat(completionText, completionDeltaText, $"{a.completionRate}%", a.completionDelta);

        BuildTopPerformers(a.topPerformers);
        BuildStudentActivity(a.studentActivity);
        BuildMistakesList();
    }

    void SetStat(TMP_Text valueText, TMP_Text deltaText, string value, float delta)
    {
        if (valueText) valueText.text = value;
        if (deltaText)
        {
            deltaText.text = $"+{delta}% from last month";
            deltaText.color = delta >= 0
                ? new Color(0.13f, 0.69f, 0.30f)
                : new Color(0.94f, 0.27f, 0.27f);
        }
    }

    void SwitchTab(int index)
    {
        performancePanel?.SetActive(index == 0);
        studentsPanel?.SetActive(index == 1);
        mistakesPanel?.SetActive(index == 2);

        Color[] colors = { inactiveTabColor, inactiveTabColor, inactiveTabColor };
        colors[index] = activeTabColor;
        if (performanceTabIndicator) performanceTabIndicator.color = colors[0];
        if (studentsTabIndicator) studentsTabIndicator.color = colors[1];
        if (mistakesTabIndicator) mistakesTabIndicator.color = colors[2];
    }

    void BuildTopPerformers(List<TopPerformer> performers)
    {
        if (topPerformersParent == null) return;
        foreach (Transform child in topPerformersParent) Destroy(child.gameObject);

        foreach (var p in performers)
        {
            var row = Instantiate(topPerformerRowPrefab, topPerformersParent);
            row.GetComponent<TopPerformerRowUI>()?.Setup(p);
        }
    }

    void BuildStudentActivity(StudentActivityStats activity)
    {
        if (totalStudentsText) totalStudentsText.text = activity.totalStudents.ToString();
        if (activeThisMonthText) activeThisMonthText.text = activity.activeThisMonth.ToString();
        if (averageLevelText) averageLevelText.text = activity.averageLevel.ToString("F1");
        if (averagePointsText) averagePointsText.text = activity.averagePoints.ToString("N0");
    }

    void BuildMistakesList()
    {
        if (mistakesListParent == null) return;
        foreach (Transform child in mistakesListParent) Destroy(child.gameObject);

        // Sample data — in production load from Firestore analytics
        var mistakes = new List<CommonMistake>
        {
            new CommonMistake { questionText = "How many bones in adult body?", category = "Skeletal System",    errorCount = 45 },
            new CommonMistake { questionText = "Function of mitochondria",       category = "Cell Biology",       errorCount = 38 },
            new CommonMistake { questionText = "Types of muscle tissue",          category = "Muscular System",    errorCount = 32 },
            new CommonMistake { questionText = "Cardiac cycle phases",            category = "Circulatory System", errorCount = 28 },
        };

        foreach (var m in mistakes)
        {
            var row = Instantiate(mistakeRowPrefab, mistakesListParent);
            row.GetComponent<MistakeRowUI>()?.Setup(m);
        }

        // Recommendation
        if (mistakes.Count > 0 && recommendationText)
            recommendationText.text =
                $"Focus on {mistakes[0].category}\nAdd more practice questions about this topic.";
    }

    void ExportToExcel()
    {
        // In production: generate CSV/XLSX via plugin (e.g., EPPlus for Unity)
        Debug.Log("[Analytics] Export to Excel pressed.");
    }

    void ExportToPDF()
    {
        Debug.Log("[Analytics] Export to PDF pressed.");
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed -= Refresh;
    }
}
