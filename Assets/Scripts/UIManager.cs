using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screen Panels")]
    public GameObject loginPanel;
    public GameObject dashboardPanel;
    public GameObject learnPanel;         // Anatomy Systems list
    public GameObject model3DPanel;       // 3D Viewer
    public GameObject quizSelectionPanel;
    public GameObject quizPanel;
    public GameObject quizResultPanel;
    public GameObject progressPanel;
    public GameObject achievementsPanel;
    //public GameObject rewardPanel;
    public GameObject profilePanel;

    [Header("Bottom Navigation Bar")]
    public GameObject bottomNavBar;
    public Button navHomeBtn;
    public Button navLearnBtn;
    public Button navAchievementsBtn;
    public Button navProfileBtn;
    public Image[] navIcons;
    public Color navActiveColor = new Color(0.5f, 0.2f, 0.9f);
    public Color navInactiveColor = new Color(0.6f, 0.6f, 0.6f);

    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Start with login screen
        ShowPanel(loginPanel, false);
        bottomNavBar.SetActive(false);
        SetupNavButtons();

    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void LogoutToLogin()
    {
        panelHistory.Clear();
        ShowPanel(loginPanel, false);
        ShowBottomNav(false);
        //PlayerSessionManager.Instance?.ClearSession();
    }

}
