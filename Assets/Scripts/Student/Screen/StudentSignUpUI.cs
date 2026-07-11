using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StudentSignUpUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField fullNameField;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_InputField confirmPasswordField;

    [Header("Password Toggle")]
    public Button togglePasswordBtn;
    public Button toggleConfirmPasswordBtn;
    public Image eyeIcon1;
    public Image eyeIcon2;
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    [Header("Password Strength")]
    public Slider passwordStrengthBar;
    public TMP_Text passwordStrengthText;

    [Header("Buttons")]
    public Button signUpBtn;
    public Button googleSignUpBtn;
    public Button signInBtn;       // navigate back to Login

    [Header("Feedback")]
    public TMP_Text errorText;
    public GameObject loadingSpinner;
    public GameObject successOverlay;
    public TMP_Text successMessage;

    private bool pass1Visible = false;
    private bool pass2Visible = false;

    // Strength bar colors
    private Color weakColor = new Color(0.94f, 0.27f, 0.27f); // red
    private Color fairColor = new Color(0.95f, 0.61f, 0.07f); // amber
    private Color goodColor = new Color(0.13f, 0.69f, 0.30f); // green
    private Color strongColor = new Color(0.40f, 0.20f, 0.90f); // purple

    void Start()
    {
        // Password mode
        passwordField.contentType = TMP_InputField.ContentType.Password;
        confirmPasswordField.contentType = TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();
        confirmPasswordField.ForceLabelUpdate();

        // Buttons
        signUpBtn.onClick.AddListener(OnSignUp);
        googleSignUpBtn.onClick.AddListener(OnGoogleSignUp);
        signInBtn.onClick.AddListener(OnGoToSignIn);
        togglePasswordBtn.onClick.AddListener(() => TogglePassword(ref pass1Visible, passwordField, eyeIcon1));
        toggleConfirmPasswordBtn.onClick.AddListener(() => TogglePassword(ref pass2Visible, confirmPasswordField, eyeIcon2));

        // Live password strength
        passwordField.onValueChanged.AddListener(OnPasswordChanged);

        // Hide feedback elements
        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);
        if (successOverlay) successOverlay.SetActive(false);
        if (passwordStrengthBar) passwordStrengthBar.value = 0f;
        if (passwordStrengthText) passwordStrengthText.text = "";

        // Subscribe to session events
        if (PlayerSessionManager.Instance != null)
            PlayerSessionManager.Instance.OnLoginFailed += ShowError;
    }

    void OnEnable()
    {
        ClearForm();
    }

    // ?? Toggle password visibility ???????????????????????????
    void TogglePassword(ref bool visible, TMP_InputField field, Image icon)
    {
        visible = !visible;
        field.contentType = visible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
        if (icon) icon.sprite = visible ? eyeOpenSprite : eyeClosedSprite;
    }

    // ?? Live password strength indicator ????????????????????
    void OnPasswordChanged(string password)
    {
        int strength = CalculateStrength(password);

        if (passwordStrengthBar)
            passwordStrengthBar.value = strength / 4f;

        if (passwordStrengthText)
        {
            switch (strength)
            {
                case 0:
                    passwordStrengthText.text = "";
                    break;
                case 1:
                    passwordStrengthText.text = "Weak";
                    passwordStrengthText.color = weakColor;
                    if (passwordStrengthBar)
                        passwordStrengthBar.fillRect.GetComponent<Image>().color = weakColor;
                    break;
                case 2:
                    passwordStrengthText.text = "Fair";
                    passwordStrengthText.color = fairColor;
                    if (passwordStrengthBar)
                        passwordStrengthBar.fillRect.GetComponent<Image>().color = fairColor;
                    break;
                case 3:
                    passwordStrengthText.text = "Good";
                    passwordStrengthText.color = goodColor;
                    if (passwordStrengthBar)
                        passwordStrengthBar.fillRect.GetComponent<Image>().color = goodColor;
                    break;
                case 4:
                    passwordStrengthText.text = "Strong ??";
                    passwordStrengthText.color = strongColor;
                    if (passwordStrengthBar)
                        passwordStrengthBar.fillRect.GetComponent<Image>().color = strongColor;
                    break;
            }
        }
    }

    int CalculateStrength(string password)
    {
        if (string.IsNullOrEmpty(password)) return 0;
        int score = 0;
        if (password.Length >= 8) score++;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"\d")) score++; // has number
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]")) score++; // has uppercase
        if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score++; // has symbol
        return score;
    }

    // ?? Sign Up ??????????????????????????????????????????????
    void OnSignUp()
    {
        string name = fullNameField.text.Trim();
        string email = emailField.text.Trim();
        string pass = passwordField.text;
        string confirm = confirmPasswordField.text;

        // Validate
        if (string.IsNullOrEmpty(name))
        { ShowError("Please enter your full name."); return; }

        if (name.Length < 2)
        { ShowError("Name must be at least 2 characters."); return; }

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        { ShowError("Please enter a valid email address."); return; }

        if (string.IsNullOrEmpty(pass) || pass.Length < 6)
        { ShowError("Password must be at least 6 characters."); return; }

        if (pass != confirm)
        { ShowError("Passwords do not match."); return; }

        if (CalculateStrength(pass) < 2)
        { ShowError("Password is too weak. Add numbers or symbols."); return; }

        SetLoading(true);
        PlayerSessionManager.Instance?.Register(email, pass, name);
    }

    void OnGoogleSignUp()
    {
       // PlayerSessionManager.Instance?.LoginWithGoogle();
    }

    void OnGoToSignIn()
    {
        UIManager.Instance?.ShowPanel(UIManager.Instance.loginPanel);
    }

    // ?? Feedback ?????????????????????????????????????????????
    void ShowError(string msg)
    {
        SetLoading(false);
        if (errorText)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }
    }

    void SetLoading(bool loading)
    {
        signUpBtn.interactable = !loading;
        googleSignUpBtn.interactable = !loading;
        if (loadingSpinner) loadingSpinner.SetActive(loading);
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void ClearForm()
    {
        if (fullNameField) fullNameField.text = "";
        if (emailField) emailField.text = "";
        if (passwordField) passwordField.text = "";
        if (confirmPasswordField) confirmPasswordField.text = "";
        if (errorText) errorText.gameObject.SetActive(false);
        if (successOverlay) successOverlay.SetActive(false);
        if (passwordStrengthBar) passwordStrengthBar.value = 0f;
        if (passwordStrengthText) passwordStrengthText.text = "";
    }

    bool IsValidEmail(string email) =>
        System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    void OnDestroy()
    {
        if (PlayerSessionManager.Instance != null)
            PlayerSessionManager.Instance.OnLoginFailed -= ShowError;
    }
}
