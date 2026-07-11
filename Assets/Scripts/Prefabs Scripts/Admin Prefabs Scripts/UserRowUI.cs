using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserRowUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text statusText;
    public Image statusBadge;
    public TMP_Text emailText;
    public TMP_Text levelText;
    public TMP_Text pointsText;
    public TMP_Text quizzesText;
    public Button editBtn;
    public Button toggleStatusBtn;
    public Button deleteBtn;
    public Image toggleIcon;
    public Sprite checkSprite;
    public Sprite crossSprite;

    private StudentRecord data;
    private System.Action<StudentRecord, bool> onToggle;
    private System.Action<StudentRecord> onDelete;

    static readonly Color activeColor = new Color(0.13f, 0.13f, 0.13f);
    static readonly Color inactiveColor = new Color(0.60f, 0.60f, 0.60f);

    public void Setup(StudentRecord student,
                      System.Action<StudentRecord, bool> toggleCallback,
                      System.Action<StudentRecord> deleteCallback)
    {
        data = student;
        onToggle = toggleCallback;
        onDelete = deleteCallback;

        nameText.text = student.fullName;
        emailText.text = student.email;
        levelText.text = $"Level {student.level}";
        pointsText.text = $"{student.totalPoints} points";
        quizzesText.text = $"{student.quizzesCompleted} quizzes";

        // Status badge
        bool active = student.isActive;
        statusText.text = active ? "Active" : "Inactive";
        statusBadge.color = active ? activeColor : inactiveColor;

        // Toggle button icon
        if (toggleIcon)
            toggleIcon.sprite = active ? crossSprite : checkSprite;
        if (toggleStatusBtn)
            toggleStatusBtn.image.color = active
                ? new Color(0.94f, 0.27f, 0.27f)
                : new Color(0.13f, 0.69f, 0.30f);

        toggleStatusBtn.onClick.AddListener(() => onToggle?.Invoke(data, !data.isActive));
        deleteBtn.onClick.AddListener(() => onDelete?.Invoke(data));
        editBtn.onClick.AddListener(OnEdit);
    }

    void OnEdit() => Debug.Log($"[UserMgmt] Edit user: {data.fullName}");
}
