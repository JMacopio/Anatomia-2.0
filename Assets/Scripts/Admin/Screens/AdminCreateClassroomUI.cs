using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminCreateClassroomUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;

    [Header("Form Fields")]
    public TMP_InputField classroomNameField;
    public TMP_InputField descriptionField;

    [Header("Buttons")]
    public Button createBtn;

    [Header("Feedback")]
    public TMP_Text errorText;
    public TMP_Text successText;
    public GameObject loadingSpinner;
    public GameObject successCard;    // shows generated code after creation

    [Header("Success Card")]
    public TMP_Text generatedCodeText;
    public Button copyCodeBtn;
    public Button doneBtn;

    void Start()
    {
        backBtn.onClick.AddListener(() => AdminUIManager.Instance.GoBack());
        createBtn.onClick.AddListener(OnCreateClassroom);
        copyCodeBtn?.onClick.AddListener(CopyCodeToClipboard);
        doneBtn?.onClick.AddListener(() => AdminUIManager.Instance.GoBack());

        if (errorText) errorText.gameObject.SetActive(false);
        if (successCard) successCard.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);
    }

    void OnEnable()
    {
        classroomNameField.text = "";
        descriptionField.text = "";
        if (errorText) errorText.gameObject.SetActive(false);
        if (successCard) successCard.SetActive(false);
    }

    void OnCreateClassroom()
    {
        string name = classroomNameField.text.Trim();
        string desc = descriptionField.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter a classroom name.");
            return;
        }
        if (name.Length < 3)
        {
            ShowError("Classroom name must be at least 3 characters.");
            return;
        }

        SetLoading(true);
        //AdminSessionManager.Instance?.CreateClassroom(name, desc, OnClassroomCreated);
    }

    void OnClassroomCreated(string code)
    {
        SetLoading(false);

        // Show success card with generated code
        if (successCard)
        {
            successCard.SetActive(true);
            if (generatedCodeText) generatedCodeText.text = code;
        }

        Debug.Log($"[CreateClassroom] Created with code: {code}");
    }

    void CopyCodeToClipboard()
    {
        if (generatedCodeText)
            GUIUtility.systemCopyBuffer = generatedCodeText.text;
        Debug.Log("[CreateClassroom] Code copied to clipboard.");
    }

    void ShowError(string msg)
    {
        if (errorText)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }
    }

    void SetLoading(bool loading)
    {
        createBtn.interactable = !loading;
        if (loadingSpinner) loadingSpinner.SetActive(loading);
        if (errorText) errorText.gameObject.SetActive(false);
    }
}
