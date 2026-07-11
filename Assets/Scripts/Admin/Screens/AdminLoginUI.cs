using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminLoginUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button togglePasswordBtn;
    public Image eyeIcon;
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    [Header("Buttons")]
    public Button secureLoginBtn;
    public Button backToStudentBtn;
    public Button createAdminAccountBtn;  // NEW — "Create Admin Account"

    [Header("Feedback")]
    public TMP_Text errorText;
    public GameObject loadingSpinner;

    private bool passwordVisible = false;

    void Start()
    {
        passwordField.contentType = TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();

        secureLoginBtn.onClick.AddListener(OnSecureLogin);
        backToStudentBtn.onClick.AddListener(OnBackToStudent);
        togglePasswordBtn.onClick.AddListener(TogglePassword);
        createAdminAccountBtn?.onClick.AddListener(() =>
            AdminUIManager.Instance?.OpenAdminSignUp());

        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);

        // Subscribe to login events
        if (AdminSessionManager.Instance != null)
        {
            AdminSessionManager.Instance.OnAdminLoginFailed += ShowError;
        }
    }

    void OnEnable()
    {
        if (emailField) emailField.text = "";
        if (passwordField) passwordField.text = "";
        ClearError();
    }

    void TogglePassword()
    {
        passwordVisible = !passwordVisible;
        passwordField.contentType = passwordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();
        if (eyeIcon)
            eyeIcon.sprite = passwordVisible ? eyeOpenSprite : eyeClosedSprite;
    }

    void OnSecureLogin()
    {
        string email = emailField.text.Trim();
        string pass = passwordField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            ShowError("Please enter your email and password.");
            return;
        }

        SetLoading(true);
       // AdminSessionManager.Instance?.Login(email, pass);
    }

    void OnBackToStudent()
    {
        // Switch back to student canvas/login
        // If both canvases exist in scene, disable admin canvas and enable student canvas
        gameObject.SetActive(false);
        // Alternatively: SceneManager.LoadScene("StudentScene");
        Debug.Log("[AdminLogin] Back to Student Login pressed.");
    }

    void ShowError(string msg)
    {
        SetLoading(false);
        if (errorText)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }
    }

    void ClearError()
    {
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void SetLoading(bool loading)
    {
        secureLoginBtn.interactable = !loading;
        if (loadingSpinner) loadingSpinner.SetActive(loading);
        ClearError();
    }

    void OnDestroy()
    {
        if (AdminSessionManager.Instance != null)
            AdminSessionManager.Instance.OnAdminLoginFailed -= ShowError;
    }
}
