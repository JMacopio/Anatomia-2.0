using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text studentNameText;
    public TMP_Text emailText;
    public Image avatarImage;

    [Header("Stats Row")]
    public TMP_Text levelStatText;
    public TMP_Text xpStatText;
    public TMP_Text badgesStatText;

    [Header("Learning Progress")]
    public Transform progressListParent;
    public GameObject progressRowPrefab;

    [Header("Account Actions")]
    public Button editProfileBtn;
    public Button viewAchievementsBtn;
    public Button logoutBtn;

    [Header("Footer")]
    public TMP_Text versionText;

    void Start()
    {
        logoutBtn.onClick.AddListener(() => UIManager.Instance.LogoutToLogin());
        viewAchievementsBtn.onClick.AddListener(() =>
            UIManager.Instance.NavigateTo(UIManager.Instance.achievementsPanel, 2));
        editProfileBtn.onClick.AddListener(() => Debug.Log("Edit Profile pressed"));
        versionText.text = "Anatomia v1.0.0\nFor RadTech Students";
    }

    void OnEnable() => PopulateUI();

    void PopulateUI()
    {
        var data = PlayerSessionManager.Instance?.studentData;
        if (data == null) return;

        studentNameText.text = data.studentName;
        emailText.text = data.email;

        levelStatText.text = data.level.ToString();
        xpStatText.text = data.totalXP.ToString();
        badgesStatText.text = data.badgesEarned.ToString();

        BuildProgressList(data);
    }

    void BuildProgressList(StudentData data)
    {
        foreach (Transform child in progressListParent)
            Destroy(child.gameObject);

        foreach (var kvp in data.systemProgress)
        {
            var row = Instantiate(progressRowPrefab, progressListParent);
            row.GetComponent<ProgressRowUI>()?.Setup(kvp.Key, kvp.Value);
        }
    }
}
