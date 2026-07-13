using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screen Panels")]
    public GameObject loginPanel;
    public GameObject studentSignUpPanel;   // NEW
    public GameObject dashboardPanel;
    public GameObject learnPanel;
    public GameObject model3DPanel;
    public GameObject quizSelectionPanel;
    public GameObject quizPanel;
    public GameObject quizResultPanel;
    public GameObject progressPanel;
    public GameObject achievementsPanel;
    public GameObject rewardPanel;
    public GameObject profilePanel;
    public GameObject joinClassroomPanel;   // NEW

    [Header("Bottom Navigation Bar")]
    public GameObject bottomNavBar;
    public Button navHomeBtn;
    public Button navLearnBtn;
    public Button navAchievementsBtn;
    public Button navProfileBtn;
    public Image[] navIcons;
    public Color navActiveColor = new Color(0.5f, 0.2f, 0.9f);
    public Color navInactiveColor = new Color(0.6f, 0.6f, 0.6f);

    [Header("Canvas")]
    public GameObject studentCanvas; //added
    public GameObject adminCanvas;  //added

    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        studentCanvas.SetActive(true); //added
        adminCanvas.SetActive(false); //added

        HideAllPanels();
        ShowPanel(loginPanel, false);
        bottomNavBar.SetActive(false);
        SetupNavButtons();
    }

    void HideAllPanels()
    {
        GameObject[] allPanels = {
            loginPanel, studentSignUpPanel, dashboardPanel, learnPanel,
            model3DPanel, quizSelectionPanel, quizPanel, quizResultPanel,
            progressPanel, achievementsPanel, rewardPanel,
            profilePanel, joinClassroomPanel
        };
        foreach (var p in allPanels)
            if (p != null) p.SetActive(false);
    }

    void SetupNavButtons()
    {
        navHomeBtn.onClick.AddListener(() => NavigateTo(dashboardPanel, 0));
        navLearnBtn.onClick.AddListener(() => NavigateTo(learnPanel, 1));
        navAchievementsBtn.onClick.AddListener(() => NavigateTo(achievementsPanel, 2));
        navProfileBtn.onClick.AddListener(() => NavigateTo(profilePanel, 3));
    }

    public void ShowPanel(GameObject panel, bool addToHistory = true)
    {
        if (panel == null) return;
        if (currentPanel != null)
        {
            if (addToHistory) panelHistory.Push(currentPanel);
            currentPanel.SetActive(false);
        }
        currentPanel = panel;
        currentPanel.SetActive(true);
    }

    public void GoBack()
    {
        if (panelHistory.Count > 0)
        {
            currentPanel.SetActive(false);
            currentPanel = panelHistory.Pop();
            currentPanel.SetActive(true);
        }
    }

    public void NavigateTo(GameObject panel, int navIndex)
    {
        panelHistory.Clear();
        ShowPanel(panel, false);
        UpdateNavBar(navIndex);
    }

    void UpdateNavBar(int activeIndex)
    {
        for (int i = 0; i < navIcons.Length; i++)
            navIcons[i].color = (i == activeIndex) ? navActiveColor : navInactiveColor;
    }

    public void ShowBottomNav(bool show) => bottomNavBar.SetActive(show);

    // Called from DashboardUI Join button
    public void OpenJoinClassroom() => ShowPanel(joinClassroomPanel);

    // Called from Login screen "Sign Up" button
    public void OpenStudentSignUp() => ShowPanel(studentSignUpPanel);

    public void LogoutToLogin()
    {
        panelHistory.Clear();
        HideAllPanels();
        ShowPanel(loginPanel, false);
        ShowBottomNav(false);
        PlayerSessionManager.Instance?.ClearSession();
    }

    public void ShowStudentUI() //added
    {
        studentCanvas.SetActive(true);
        adminCanvas.SetActive(false);
        // Optionally reset to login panel
        HideAllPanels();
        ShowPanel(loginPanel, false);
        ShowBottomNav(false);
    }

    public void ShowAdminUI() //added
    {
        studentCanvas.SetActive(false);
        adminCanvas.SetActive(true);
        // Optionally show admin login panel via AdminUIManager
        AdminUIManager.Instance?.ShowPanel(AdminUIManager.Instance.adminLoginPanel, false);
    }
}

