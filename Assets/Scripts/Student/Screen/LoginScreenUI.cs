using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginScreenUI : MonoBehaviour
{

    [Header("Input Fields")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button togglePasswordBtn;
    public Image eyeIcon;

    [Header("Buttons")]
    public Button signInBtn;
    public Button googleSignInBtn;
    public Button forgotPasswordBtn; // NEW — "Forgot Password?"
    public Button adminLoginBtn;
    public Button signUpBtn;        // NEW — "Don't have an account? Sign Up"

    [Header("UI Feedback")]
    public TMP_Text errorText;
    public GameObject loadingSpinner;

    [Header("Visual")]
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    //private bool passwordVisible = false;

    [Header("Google Sign-In Config")]
    [SerializeField]
    private string webClientId =
        "597465184116-a367qp7bn4887g5nddrlg0ea2bav1qa6.apps.googleusercontent.com";

    // ── Firebase ─────────────────────────────────────────────
    private FirebaseAuth auth;
    private bool passwordVisible = false;
    private bool isInitialized = false;

    void Start()
    {
        // Setup password field
        passwordField.contentType = TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();

        // Bind buttons
        signInBtn.onClick.AddListener(OnSignIn);
        googleSignInBtn.onClick.AddListener(OnGoogleSignIn);
        adminLoginBtn.onClick.AddListener(OnAdminLogin);
        togglePasswordBtn.onClick.AddListener(TogglePasswordVisibility);
        forgotPasswordBtn?.onClick.AddListener(OnForgotPassword); //added
        signUpBtn?.onClick.AddListener(() =>
            UIManager.Instance?.OpenStudentSignUp());

        // Clear error
        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);

        // Init Firebase + Google Sign-In
        InitializeFirebaseAndGoogle();
    }

    void TogglePasswordVisibility()
    {
        passwordVisible = !passwordVisible;
        passwordField.contentType = passwordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        passwordField.ForceLabelUpdate();
        if (eyeIcon)
            eyeIcon.sprite = passwordVisible ? eyeOpenSprite : eyeClosedSprite;
    }

    void OnSignIn()
    {
        string email = emailField.text.Trim();
        string pass = passwordField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            ShowError("Please fill in all fields.");
            return;
        }
        if (!IsValidEmail(email))
        {
            ShowError("Please enter a valid email.");
            return;
        }

        //StartCoroutine(SignInRoutine(email, pass));

        //added
        SetLoading(true);
        PlayerSessionManager.Instance?.Login(email, pass);

        // Subscribe to result (one-time)
        if (PlayerSessionManager.Instance != null)
        {
            PlayerSessionManager.Instance.OnLoginFailed += OnLoginFailed;
            PlayerSessionManager.Instance.OnLoginSuccess += OnLoginSuccess;
        }
    }

    //added
    // Callback for login success
    void OnLoginSuccess()
    {
        SetLoading(false);
        UnsubscribeEvents();
    }

    //added
    void OnLoginFailed(string error)
    {
        SetLoading(false);
        ShowError(error);
        UnsubscribeEvents();
    }

    //added
    // Unsubscribe from events to avoid memory leaks
    void UnsubscribeEvents()
    {
        if (PlayerSessionManager.Instance == null) return;
        PlayerSessionManager.Instance.OnLoginFailed -= OnLoginFailed;
        PlayerSessionManager.Instance.OnLoginSuccess -= OnLoginSuccess;
    }

    IEnumerator SignInRoutine(string email, string pass)
    {
        SetLoading(true);
        yield return new WaitForSeconds(1.2f); // Simulate API call
        SetLoading(false);

        // In production: verify against backend
        PlayerSessionManager.Instance.Login(email, pass);
    }

    void OnGoogleSignIn()
    {
        // Integrate Google Sign-In SDK here
        //Debug.Log("Google Sign-In pressed");
        //StartCoroutine(SignInRoutine("google@student.com", "oauth"));

        if (!isInitialized)
        { ShowError("App not ready. Please wait."); return; }

        SetLoading(true);

        // Start Google Sign-In flow
        GoogleSignIn.DefaultInstance
            .SignIn()
            .ContinueWithOnMainThread(OnGoogleSignInFinished);
    }

    void OnAdminLogin()
    {
        // Activate admin canvas / open admin login
        // If using separate canvas: adminCanvas.SetActive(true);
        //adminCanvas.SetActive(true);
        //studentCanvas.SetActive(false);
        UIManager.Instance.ShowAdminUI(); //added

        AdminUIManager.Instance?.ShowPanel(AdminUIManager.Instance.adminLoginPanel, false);
        Debug.Log("[Login] Navigating to Admin Login.");
    }

    void ShowError(string msg)
    {
        if (errorText)
        {
            errorText.text = msg;
            errorText.gameObject.SetActive(true);
        }
    }

    void SetLoading(bool isLoading)
    {
        signInBtn.interactable = !isLoading;
        googleSignInBtn.interactable = !isLoading;
        if (loadingSpinner) loadingSpinner.SetActive(isLoading);
        if (errorText) errorText.gameObject.SetActive(false);
    }

    bool IsValidEmail(string email) =>
        System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    // ── Initialize Firebase + Google Sign-In ─────────────────
    void InitializeFirebaseAndGoogle()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    isInitialized = true;

                    // Configure Google Sign-In
                    GoogleSignIn.Configuration = new GoogleSignInConfiguration
                    {
                        RequestIdToken = true,
                        RequestEmail = true,
                        WebClientId = webClientId
                    };

                    Debug.Log("[Login] Firebase + Google Sign-In ready.");

                    // Auto-login if already signed in
                    if (auth.CurrentUser != null)
                    {
                        Debug.Log("[Login] Auto-login: " + auth.CurrentUser.Email);
                        PlayerSessionManager.Instance?.Login(
                            auth.CurrentUser.Email, "");
                    }
                }
                else
                {
                    ShowError("App initialization failed. Please restart.");
                    Debug.LogError("[Login] Firebase error: " + task.Result);
                }
            });
    }

    //added
    void OnGoogleSignInFinished(
        System.Threading.Tasks.Task<GoogleSignInUser> task)
    {
        if (task.IsCanceled)
        {
            SetLoading(false);
            Debug.Log("[GoogleSignIn] Cancelled by user.");
            return;
        }

        if (task.IsFaulted)
        {
            SetLoading(false);
            ShowError("Google Sign-In failed. Please try again.");
            Debug.LogError("[GoogleSignIn] Error: " + task.Exception);
            return;
        }

        // Got Google token — now sign in with Firebase
        string idToken = task.Result.IdToken;
        FirebaseGoogleSignIn(idToken);
    }

    void FirebaseGoogleSignIn(string idToken)
    {
        // Exchange Google token for Firebase credential
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    SetLoading(false);
                    ShowError("Google authentication failed.");
                    Debug.LogError("[FirebaseGoogle] " + task.Exception);
                    return;
                }

                // Success — get the Firebase user
                FirebaseUser user = task.Result;
                Debug.Log($"[GoogleSignIn] Signed in: {user.DisplayName} ({user.Email})");

                SetLoading(false);

                // Pass to PlayerSessionManager to load/create Firestore profile
                PlayerSessionManager.Instance?.HandleGoogleSignIn(
                    user.UserId,
                    user.DisplayName ?? "Student",
                    user.Email,
                    user.PhotoUrl?.ToString() ?? ""
                );
            });
    }

    // ── Forgot Password ──────────────────────────────────────
    void OnForgotPassword()
    {
        string email = emailField.text.Trim();

        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowError("Enter your email above then tap Forgot Password.");
            return;
        }

        SetLoading(true);

        auth.SendPasswordResetEmailAsync(email)
            .ContinueWithOnMainThread(task =>
            {
                SetLoading(false);
                if (task.IsFaulted)
                {
                    ShowError("Could not send reset email. Check your email address.");
                    return;
                }
                ShowError($"Password reset email sent to {email}");
                if (errorText) errorText.color = new Color(0.13f, 0.69f, 0.30f); // green
            });
    }
}
