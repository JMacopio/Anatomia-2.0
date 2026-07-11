using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminGamificationUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;
    public Button saveChangesBtn;

    [Header("Points Config")]
    public TMP_InputField easyPointsField;
    public TMP_InputField mediumPointsField;
    public TMP_InputField hardPointsField;

    [Header("Badges")]
    public Button addBadgeBtn;
    public Transform badgeListParent;
    public GameObject badgeRowPrefab;

    [Header("Level Progression")]
    public Button addLevelBtn;
    public Transform levelListParent;
    public GameObject levelRowPrefab;

    void Start()
    {
        backBtn.onClick.AddListener(() => AdminUIManager.Instance.GoBack());
        saveChangesBtn.onClick.AddListener(OnSaveChanges);
        addBadgeBtn.onClick.AddListener(() => AdminUIManager.Instance.ShowAddBadgeModal());
        addLevelBtn.onClick.AddListener(() => AdminUIManager.Instance.ShowAddLevelModal());

        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed += Refresh;
    }

    void OnEnable()
    {
        //AdminSessionManager.Instance?.LoadGamificationSettings();
        Refresh();
    }

    public void Refresh()
    {
        var session = AdminSessionManager.Instance;
        if (session == null) return;

        var gs = session.gamificationSettings;
        easyPointsField.text = gs.easyPoints.ToString();
        mediumPointsField.text = gs.mediumPoints.ToString();
        hardPointsField.text = gs.hardPoints.ToString();

        BuildBadgeList(gs.badges);
        BuildLevelList(gs.levels);
    }

    void OnSaveChanges()
    {
        int easy = ParseField(easyPointsField, 10);
        int medium = ParseField(mediumPointsField, 20);
        int hard = ParseField(hardPointsField, 30);
        //AdminSessionManager.Instance?.SavePointsConfig(easy, medium, hard);
    }

    void BuildBadgeList(List<BadgeRecord> badges)
    {
        foreach (Transform child in badgeListParent) Destroy(child.gameObject);
        foreach (var badge in badges)
        {
            var row = Instantiate(badgeRowPrefab, badgeListParent);
            row.GetComponent<BadgeRowUI>()?.Setup(badge, OnDeleteBadge);
        }
    }

    void BuildLevelList(List<LevelRecord> levels)
    {
        foreach (Transform child in levelListParent) Destroy(child.gameObject);
        foreach (var level in levels)
        {
            var row = Instantiate(levelRowPrefab, levelListParent);
            row.GetComponent<LevelRowUI>()?.Setup(level, OnDeleteLevel);
        }
    }

    void OnDeleteBadge(BadgeRecord badge)
    {
        //AdminSessionManager.Instance?.DeleteBadge(badge.badgeId, Refresh);
    }

    void OnDeleteLevel(LevelRecord level)
    {
        Debug.Log($"[Gamification] Delete level {level.levelNumber}");
        // AdminSessionManager.Instance?.DeleteLevel(docId, Refresh);
    }

    int ParseField(TMP_InputField field, int fallback)
    {
        return int.TryParse(field.text, out int val) ? val : fallback;
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnDataRefreshed -= Refresh;
    }
}
