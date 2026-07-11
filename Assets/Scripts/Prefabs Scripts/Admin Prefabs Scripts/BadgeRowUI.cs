using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BadgeRowUI : MonoBehaviour
{
    public Image badgeIcon;
    public TMP_Text badgeTitleText;
    public TMP_Text requiresText;
    public Button deleteBtn;

    private BadgeRecord data;
    private System.Action<BadgeRecord> onDelete;

    public void Setup(BadgeRecord badge, System.Action<BadgeRecord> deleteCallback)
    {
        data = badge;
        onDelete = deleteCallback;
        badgeTitleText.text = badge.title;
        requiresText.text = $"Requires {badge.pointsRequired:N0} points";
        deleteBtn.onClick.AddListener(() => onDelete?.Invoke(data));
    }
}
