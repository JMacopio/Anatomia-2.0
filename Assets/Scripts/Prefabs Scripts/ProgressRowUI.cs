using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressRowUI : MonoBehaviour
{
    public Image dotColor;
    public TMP_Text systemNameText;
    public TMP_Text percentText;
    public Color dotActiveColor = new Color(0.4f, 0.7f, 1f);

    public void Setup(string systemName, float progress)
    {
        systemNameText.text = systemName;
        percentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        if (dotColor) dotColor.color = dotActiveColor;
    }
}

