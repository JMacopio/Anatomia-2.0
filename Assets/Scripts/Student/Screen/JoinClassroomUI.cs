using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinClassroomUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;
    public TMP_Text titleText;

    [Header("Code Input")]
    public TMP_InputField codeInputField;
    public TMP_Text hintText;
    public TMP_Text errorText;

    [Header("Buttons")]
    public Button joinClassroomBtn;

    [Header("Feedback")]
    public GameObject loadingSpinner;
    public GameObject successOverlay;
    public TMP_Text successText;

    // Colors
    private Color normalBorderColor = new Color(0.85f, 0.85f, 0.85f);
    private Color errorBorderColor = new Color(0.94f, 0.27f, 0.27f);
    private Color successBorderColor = new Color(0.13f, 0.69f, 0.30f);

    void Start()
    {
        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());
        joinClassroomBtn.onClick.AddListener(OnJoinClassroom);

        // Auto uppercase the input
        codeInputField.onValueChanged.AddListener(val =>
        {
            int caretPos = codeInputField.caretPosition;
            codeInputField.SetTextWithoutNotify(val.ToUpper());
            codeInputField.caretPosition = caretPos;
            ClearError();
        });

        // Hide feedback elements
        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);
        if (successOverlay) successOverlay.SetActive(false);
    }

    void OnEnable()
    {
        // Clear field every time screen opens
        if (codeInputField) codeInputField.text = "";
        ClearError();
    }

    void OnJoinClassroom()
    {
        string code = codeInputField.text.Trim().ToUpper();

        // Validate input
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Please enter a classroom code.");
            return;
        }
        if (code.Length < 6 || code.Length > 8)
        {
            ShowError("Codes are usually 6-8 characters long.");
            return;
        }

        StartCoroutine(JoinClassroomRoutine(code));
    }

    IEnumerator JoinClassroomRoutine(string code)
    {
        SetLoading(true);

        // In production: call Firebase/backend to validate the classroom code
        // For now simulate a network call
        yield return new WaitForSeconds(1.5f);

        SetLoading(false);

        // Simulate: code found (in production check against Firestore)
        bool codeValid = IsValidCodeFormat(code);

        if (codeValid)
        {
            OnJoinSuccess(code);
        }
        else
        {
            ShowError("Classroom not found. Please check the code and try again.");
        }
    }

    void OnJoinSuccess(string code)
    {
        // Save classroom code to student data
        var data = PlayerSessionManager.Instance?.studentData;
        if (data != null)
        {
            data.classroomCode = code;
            data.classroomName = $"Classroom {code}"; // Replace with real name from Firestore
            PlayerSessionManager.Instance.SaveStudentDataToFirestore();
        }

        // Show success feedback
        if (successOverlay)
        {
            successOverlay.SetActive(true);
            if (successText)
                successText.text = $"Successfully joined!\nClassroom: {code}";
        }

        // Navigate back to Dashboard after delay
        StartCoroutine(NavigateAfterSuccess());
    }

    IEnumerator NavigateAfterSuccess()
    {
        yield return new WaitForSeconds(2f);
        if (successOverlay) successOverlay.SetActive(false);
        UIManager.Instance.NavigateTo(UIManager.Instance.dashboardPanel, 0);
    }

    void ShowError(string message)
    {
        if (errorText)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        // Tint input field border red
        var img = codeInputField.GetComponent<Image>();
        if (img) img.color = new Color(1f, 0.9f, 0.9f);
    }

    void ClearError()
    {
        if (errorText) errorText.gameObject.SetActive(false);
        var img = codeInputField?.GetComponent<Image>();
        if (img) img.color = Color.white;
    }

    void SetLoading(bool isLoading)
    {
        joinClassroomBtn.interactable = !isLoading;
        if (loadingSpinner) loadingSpinner.SetActive(isLoading);
        if (isLoading) ClearError();
    }

    // Basic format check — alphanumeric only
    bool IsValidCodeFormat(string code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        foreach (char c in code)
            if (!char.IsLetterOrDigit(c)) return false;
        // In production: validate against Firestore classrooms collection
        return true;
    }
}
