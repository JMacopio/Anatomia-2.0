using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class AdminSignUpUI : MonoBehaviour
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
    public Button createAccountBtn;
    public Button backToSignInBtn;

    [Header("Feedback")]
    public TMP_Text errorText;
    public TMP_Text successText;
    public GameObject loadingSpinner;
    public GameObject successOverlay;

    private bool pass1Visible = false;
    private bool pass2Visible = false;
    private bool isFirebaseReady = false;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // Strength colors
    private Color weakColor = new Color(0.94f, 0.27f, 0.27f);
    private Color fairColor = new Color(0.95f, 0.61f, 0.07f);
    private Color goodColor = new Color(0.13f, 0.69f, 0.30f);
    private Color strongColor = new Color(0.55f, 0.30f, 0.90f);

    void Start()
    {
        // Password fields
        passwordField.contentType = TMP_InputField.ContentType.Password;
        confirmPasswordField.contentType = TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();
        confirmPasswordField.ForceLabelUpdate();

        // Buttons
        createAccountBtn.onClick.AddListener(OnCreateAccount);
        backToSignInBtn?.onClick.AddListener(OnBackToSignIn);
        togglePasswordBtn?.onClick.AddListener(
            () => TogglePassword(ref pass1Visible, passwordField, eyeIcon1));
        toggleConfirmPasswordBtn?.onClick.AddListener(
            () => TogglePassword(ref pass2Visible, confirmPasswordField, eyeIcon2));

        // Live strength
        passwordField.onValueChanged.AddListener(OnPasswordChanged);

        // Hide feedback
        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);
        if (successOverlay) successOverlay.SetActive(false);

        InitFirebase();
    }

    void OnEnable() => ClearForm();

    // ── Firebase Init ────────────────────────────────────────
    void InitFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    db = FirebaseFirestore.DefaultInstance;
                    isFirebaseReady = true;
                    Debug.Log("[AdminSignUp] Firebase ready.");
                }
                else
                {
                    Debug.LogError("[AdminSignUp] Firebase init failed.");
                }
            });
    }

    // ── Toggle Password ──────────────────────────────────────
    void TogglePassword(ref bool visible, TMP_InputField field, Image icon)
    {
        visible = !visible;
        field.contentType = visible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
        if (icon) icon.sprite = visible ? eyeOpenSprite : eyeClosedSprite;
    }

    // ── Password Strength ────────────────────────────────────
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
                    SetBarColor(weakColor);
                    break;
                case 2:
                    passwordStrengthText.text = "Fair";
                    passwordStrengthText.color = fairColor;
                    SetBarColor(fairColor);
                    break;
                case 3:
                    passwordStrengthText.text = "Good";
                    passwordStrengthText.color = goodColor;
                    SetBarColor(goodColor);
                    break;
                case 4:
                    passwordStrengthText.text = "Strong 💪";
                    passwordStrengthText.color = strongColor;
                    SetBarColor(strongColor);
                    break;
            }
        }
    }

    void SetBarColor(Color color)
    {
        if (passwordStrengthBar?.fillRect != null)
            passwordStrengthBar.fillRect.GetComponent<Image>().color = color;
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

    // ── Create Admin Account ─────────────────────────────────
    void OnCreateAccount()
    {
        string name = fullNameField.text.Trim();
        string email = emailField.text.Trim();
        string pass = passwordField.text;
        string confirm = confirmPasswordField.text;

        // Validate
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        { ShowError("Please enter your full name."); return; }

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        { ShowError("Please enter a valid email address."); return; }

        if (string.IsNullOrEmpty(pass) || pass.Length < 6)
        { ShowError("Password must be at least 6 characters."); return; }

        if (CalculateStrength(pass) < 2)
        { ShowError("Password too weak. Add numbers or symbols."); return; }

        if (pass != confirm)
        { ShowError("Passwords do not match."); return; }

        if (!isFirebaseReady)
        { ShowError("System not ready. Please try again."); return; }

        SetLoading(true);
        RegisterAdminInFirebase(name, email, pass);
    }

    // ── Register in Firebase Auth ────────────────────────────
    void RegisterAdminInFirebase(string name, string email, string pass)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, pass)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    ShowError(GetAuthError(task.Exception));
                    return;
                }

                FirebaseUser newUser = task.Result.User;

                // Set display name in Firebase Auth
                UserProfile profile = new UserProfile { DisplayName = name };
                newUser.UpdateUserProfileAsync(profile)
                    .ContinueWithOnMainThread(_ =>
                    {
                        // Save to ADMINS collection
                        SaveToAdminsCollection(newUser.UserId, name, email);
                    });
            });
    }

    // ── Save to admins/ collection ───────────────────────────
    void SaveToAdminsCollection(string uid, string name, string email)
    {
        var adminDoc = new Dictionary<string, object>
        {
            { "name",      name },
            { "email",     email },
            { "role",      "admin" },
            { "createdAt", FieldValue.ServerTimestamp }
        };

        // Saves to admins/{uid} — same collection AdminSessionManager reads from
        db.Collection("admins").Document(uid)
            .SetAsync(adminDoc)
            .ContinueWithOnMainThread(task =>
            {
                SetLoading(false);

                if (task.IsFaulted)
                {
                    ShowError("Account created but failed to save profile. Contact support.");
                    Debug.LogError("[AdminSignUp] Firestore error: " + task.Exception);
                    return;
                }

                Debug.Log($"[AdminSignUp] Admin saved to admins/{uid} — {name} ({email})");
                ShowSuccess($"✅ Admin account created!\n{name} can now sign in.");
                StartCoroutine(NavigateToLoginAfterDelay(2.5f));
            });
    }

    IEnumerator NavigateToLoginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (successOverlay) successOverlay.SetActive(false);
        // Go back to Admin Login
        AdminUIManager.Instance?.ShowPanel(
            AdminUIManager.Instance.adminLoginPanel, false);
    }

    void OnBackToSignIn()
    {
        AdminUIManager.Instance?.ShowPanel(
            AdminUIManager.Instance.adminLoginPanel, false);
    }

    // ── Feedback ─────────────────────────────────────────────
    void ShowError(string msg)
    {
        SetLoading(false);
        if (errorText)
        {
            errorText.text = msg;
            errorText.color = new Color(0.94f, 0.27f, 0.27f);
            errorText.gameObject.SetActive(true);
        }
    }

    void ShowSuccess(string msg)
    {
        if (successOverlay) successOverlay.SetActive(true);
        if (successText) successText.text = msg;
        if (errorText) errorText.gameObject.SetActive(false);
    }

    void SetLoading(bool loading)
    {
        createAccountBtn.interactable = !loading;
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

    string GetAuthError(System.AggregateException ex)
    {
        var fbEx = ex?.GetBaseException() as Firebase.FirebaseException;
        if (fbEx == null) return "Registration failed. Please try again.";
        return (AuthError)fbEx.ErrorCode switch
        {
            AuthError.EmailAlreadyInUse => "This email is already registered.",
            AuthError.InvalidEmail => "Invalid email address.",
            AuthError.WeakPassword => "Password is too weak (min 6 chars).",
            AuthError.NetworkRequestFailed => "No internet connection.",
            _ => "Failed to create account. Try again."
        };
    }
}