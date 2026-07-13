using System.Collections.Generic;
using UnityEngine;

public class AdminUIManager : MonoBehaviour
{
    public static AdminUIManager Instance { get; private set; }

    [Header("Admin Panels")]
    public GameObject adminLoginPanel;
    public GameObject adminSignUpPanel;    // NEW
    public GameObject adminDashboardPanel;
    public GameObject adminUserManagementPanel;
    public GameObject adminQuizManagementPanel;
    public GameObject adminGamificationPanel;
    public GameObject adminAnalyticsPanel;
    public GameObject adminCreateClassroomPanel;

    [Header("Modal Overlays")]
    public GameObject createQuizModal;
    public GameObject addQuestionModal;
    public GameObject addBadgeModal;
    public GameObject addLevelModal;
    public GameObject confirmDeleteModal;
    public GameObject modalBlocker;   // dark semi-transparent overlay behind modals

    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        HideAllPanels();
        HideAllModals();
        ShowPanel(adminLoginPanel, false);

        // Subscribe to session events
        if (AdminSessionManager.Instance != null)
        {
            AdminSessionManager.Instance.OnAdminLoginSuccess += OnLoginSuccess;
            AdminSessionManager.Instance.OnLogout += OnLogout;
        }
    }

    // ── Panel Navigation ─────────────────────────────────────
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
            currentPanel?.SetActive(false);
            currentPanel = panelHistory.Pop();
            currentPanel.SetActive(true);
        }
    }

    // Shortcuts used by buttons
    public void OpenAdminSignUp() => ShowPanel(adminSignUpPanel);
    public void OpenDashboard() => ShowPanel(adminDashboardPanel, false);
    public void OpenUserManagement() => ShowPanel(adminUserManagementPanel);
    public void OpenQuizManagement() => ShowPanel(adminQuizManagementPanel);
    public void OpenGamification() => ShowPanel(adminGamificationPanel);
    public void OpenAnalytics() => ShowPanel(adminAnalyticsPanel);
    public void OpenCreateClassroom() => ShowPanel(adminCreateClassroomPanel);

    // ── Modal Control ────────────────────────────────────────
    public void ShowModal(GameObject modal)
    {
        HideAllModals();
        if (modalBlocker) modalBlocker.SetActive(true);
        if (modal) modal.SetActive(true);
    }

    public void CloseAllModals()
    {
        HideAllModals();
        if (modalBlocker) modalBlocker.SetActive(false);
    }

    void HideAllModals()
    {
        GameObject[] modals = {
            createQuizModal, addQuestionModal,
            addBadgeModal,   addLevelModal, confirmDeleteModal
        };
        foreach (var m in modals)
            if (m != null) m.SetActive(false);
    }

    public void ShowCreateQuizModal() => ShowModal(createQuizModal);
    public void ShowAddQuestionModal() => ShowModal(addQuestionModal);
    public void ShowAddBadgeModal() => ShowModal(addBadgeModal);
    public void ShowAddLevelModal() => ShowModal(addLevelModal);

    // ── Session Callbacks ────────────────────────────────────
    void OnLoginSuccess()
    {
        panelHistory.Clear();
        ShowPanel(adminDashboardPanel, false);
        // Refresh dashboard data
        adminDashboardPanel.GetComponent<AdminDashboardUI>()?.Refresh();
    }

    void OnLogout()
    {
        panelHistory.Clear();
        HideAllPanels();
        ShowPanel(adminLoginPanel, false);
    }

    void HideAllPanels()
    {
        GameObject[] panels = {
            adminLoginPanel, adminSignUpPanel, adminDashboardPanel,
            adminUserManagementPanel, adminQuizManagementPanel,
            adminGamificationPanel,  adminAnalyticsPanel,
            adminCreateClassroomPanel
        };
        foreach (var p in panels)
            if (p != null) p.SetActive(false);
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
        {
            AdminSessionManager.Instance.OnAdminLoginSuccess -= OnLoginSuccess;
            AdminSessionManager.Instance.OnLogout -= OnLogout;
        }
    }
}
