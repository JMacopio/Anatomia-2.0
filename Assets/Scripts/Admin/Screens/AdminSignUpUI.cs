using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class AdminSignUpUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField fullNameField;
    public TMP_InputField emailField;
    public TMP_InputField registrationKeyField;
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
    public Button registerBtn;
    public Button backToSignInBtn;

    [Header("Feedback")]
    public TMP_Text errorText;
    public TMP_Text successText;
    public GameObject loadingSpinner;
    public GameObject successOverlay;

    // The secret registration key your institution provides to admins
    // In production: store this in Firebase Remote Config, not hardcoded
    private const string VALID_REGISTRATION_KEY = "ANATOMIA-ADMIN-2024";

    private bool pass1Visible = false;
    private bool pass2Visible = false;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        // Setup password fields
        passwordField.contentType = TMP_InputField.ContentType.Password;
        confirmPasswordField.contentType = TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();
        confirmPasswordField.ForceLabelUpdate();

        // Bind buttons
        registerBtn.onClick.AddListener(OnRegister);
        backToSignInBtn.onClick.AddListener(OnBackToSignIn);
        togglePasswordBtn.onClick.AddListener(
            () => TogglePassword(ref pass1Visible, passwordField, eyeIcon1));
        toggleConfirmPasswordBtn.onClick.AddListener(
            () => TogglePassword(ref pass2Visible, confirmPasswordField, eyeIcon2));

        // Live password strength
        passwordField.onValueChanged.AddListener(OnPasswordChanged);

        // Hide feedback
        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);
        if (successOverlay) successOverlay.SetActive(false);
        if (passwordStrengthBar) passwordStrengthBar.value = 0f;
        if (passwordStrengthText) passwordStrengthText.text = "";

        // Init Firebase directly (AdminSessionManager may not be logged in yet)
        InitFirebase();
    }

    void OnEnable() => ClearForm();

    void InitFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
            }
        });
    }

    // ── Toggle password visibility ───────────────────────────
    void TogglePassword(ref bool visible, TMP_InputField field, Image icon)
    {
        visible = !visible;
        field.contentType = visible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
        if (icon) icon.sprite = visible ? eyeOpenSprite : eyeClosedSprite;
    }

    // ── Live password strength ───────────────────────────────
    void OnPasswordChanged(string password)
    {
        int strength = CalculateStrength(password);
        if (passwordStrengthBar) passwordStrengthBar.value = strength / 4f;

        if (passwordStrengthText)
        {
            var (label, color) = strength switch
            {
                0 => ("", Color.grey),
                1 => ("Weak", new Color(0.94f, 0.27f, 0.27f)),
                2 => ("Fair", new Color(0.95f, 0.61f, 0.07f)),
                3 => ("Good", new Color(0.13f, 0.69f, 0.30f)),
                _ => ("Strong 💪", new Color(0.55f, 0.30f, 0.90f)),
            };
            passwordStrengthText.text = label;
            passwordStrengthText.color = color;
            if (passwordStrengthBar && strength > 0)
                passwordStrengthBar.fillRect.GetComponent<Image>().color = color;
        }
    }

    int CalculateStrength(string p)
    {
        if (string.IsNullOrEmpty(p)) return 0;
        int s = 0;
        if (p.Length >= 8) s++;
        if (System.Text.RegularExpressions.Regex.IsMatch(p, @"\d")) s++;
        if (System.Text.RegularExpressions.Regex.IsMatch(p, @"[A-Z]")) s++;
        if (System.Text.RegularExpressions.Regex.IsMatch(p, @"[^a-zA-Z0-9]")) s++;
        return s;
    }

    // ── Register ─────────────────────────────────────────────
    void OnRegister()
    {
        string name = fullNameField.text.Trim();
        string email = emailField.text.Trim();
        string regKey = registrationKeyField.text.Trim();
        string pass = passwordField.text;
        string confirm = confirmPasswordField.text;

        // Validate all fields
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        { ShowError("Please enter your full name (min 2 characters)."); return; }

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        { ShowError("Please enter a valid email address."); return; }

        if (string.IsNullOrEmpty(regKey))
        { ShowError("Please enter the registration key."); return; }

        if (regKey != VALID_REGISTRATION_KEY)
        { ShowError("Invalid registration key. Contact your institution."); return; }

        if (string.IsNullOrEmpty(pass) || pass.Length < 6)
        { ShowError("Password must be at least 6 characters."); return; }

        if (CalculateStrength(pass) < 2)
        { ShowError("Password too weak. Add uppercase letters or symbols."); return; }

        if (pass != confirm)
        { ShowError("Passwords do not match."); return; }

        SetLoading(true);
        RegisterAdminAccount(name, email, pass);
    }

    void RegisterAdminAccount(string name, string email, string pass)
    {
        if (auth == null)
        { ShowError("Firebase not ready. Please try again."); return; }

        auth.CreateUserWithEmailAndPasswordAsync(email, pass)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    ShowError(GetAuthError(task.Exception));
                    return;
                }

                FirebaseUser newUser = task.Result.User;

                // Update display name
                UserProfile profile = new UserProfile { DisplayName = name };
                newUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(_ =>
                {
                    // Save admin document in Firestore
                    CreateAdminDocument(newUser.UserId, name, email);
                });
            });
    }

    void CreateAdminDocument(string uid, string name, string email)
    {
        var data = new System.Collections.Generic.Dictionary<string, object>
        {
            { "name",       name },
            { "email",      email },
            { "role",       "admin" },
            { "createdAt",  FieldValue.ServerTimestamp }
        };

        db.Collection("admins").Document(uid).SetAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                SetLoading(false);
                if (task.IsFaulted)
                {
                    ShowError("Account created but profile save failed. Contact support.");
                    return;
                }

                // Show success then navigate to admin login
                ShowSuccess($"Admin account created for {name}!\nYou can now sign in.");
                StartCoroutine(NavigateToLoginAfterDelay(2.5f));
            });
    }

    IEnumerator NavigateToLoginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (successOverlay) successOverlay.SetActive(false);
        // Navigate back to admin login
        AdminUIManager.Instance?.ShowPanel(AdminUIManager.Instance.adminLoginPanel, false);
    }

    void OnBackToSignIn()
    {
        AdminUIManager.Instance?.ShowPanel(AdminUIManager.Instance.adminLoginPanel, false);
    }

    // ── Feedback ─────────────────────────────────────────────
    void ShowError(string msg)
    {
        SetLoading(false);
        if (errorText)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }
    }

    void ShowSuccess(string msg)
    {
        if (successOverlay) successOverlay.SetActive(true);
        if (successText) successText.text = msg;
    }

    void SetLoading(bool loading)
    {
        registerBtn.interactable = !loading;
        if (loadingSpinner) loadingSpinner.SetActive(loading);
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void ClearForm()
    {
        if (fullNameField) fullNameField.text = "";
        if (emailField) emailField.text = "";
        if (registrationKeyField) registrationKeyField.text = "";
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

    string GetAuthError(System.AggregateException ex)
    {
        var fbEx = ex?.GetBaseException() as Firebase.FirebaseException;
        if (fbEx == null) return "Registration failed. Please try again.";
        return (AuthError)fbEx.ErrorCode switch
        {
            AuthError.EmailAlreadyInUse => "This email is already registered.",
            AuthError.InvalidEmail => "Invalid email address.",
            AuthError.WeakPassword => "Password is too weak.",
            AuthError.NetworkRequestFailed => "No internet connection.",
            _ => "Registration failed. Please try again."
        };
    }
}
