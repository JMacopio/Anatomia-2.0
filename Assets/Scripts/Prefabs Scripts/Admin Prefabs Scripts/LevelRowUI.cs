using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelRowUI : MonoBehaviour
{
    public TMP_Text levelTitleText;
    public TMP_Text pointsRequiredText;
    public Button deleteBtn;

    private LevelRecord data;
    private System.Action<LevelRecord> onDelete;

    public void Setup(LevelRecord level, System.Action<LevelRecord> deleteCallback)
    {
        data = level;
        onDelete = deleteCallback;
        levelTitleText.text = $"Level {level.levelNumber} - {level.title}";
        pointsRequiredText.text = $"{level.pointsRequired:N0} points required";
        deleteBtn.onClick.AddListener(() => onDelete?.Invoke(data));
    }
}

