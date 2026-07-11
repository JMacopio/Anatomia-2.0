using TMPro;
using UnityEngine;

public class MistakeRowUI : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_Text categoryText;
    public TMP_Text errorCountText;

    public void Setup(CommonMistake m)
    {
        questionText.text = m.questionText;
        categoryText.text = m.category;
        errorCountText.text = m.errorCount.ToString();
    }
}
