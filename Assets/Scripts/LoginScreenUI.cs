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
    public Button adminLoginBtn;

    [Header("UI Feedback")]
    public TMP_Text errorText;
    public GameObject loadingSpinner;

    [Header("Visual")]
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    private bool passwordVisible = false;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        // Clear error
        if (errorText) errorText.gameObject.SetActive(false);
        if (loadingSpinner) loadingSpinner.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void OnSignIn()
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

        StartCoroutine(SignInRoutine(email, pass));
    }

    IEnumerator SignInRoutine(string email, string pass)
    {
        SetLoading(true);
        yield return new WaitForSeconds(1.2f); // Simulate API call
        SetLoading(false);

        // In production: verify against backend
        PlayerSessionManager.Instance.Login(email, pass);
    }

    public void OnGoogleSignIn()
    {
        // Integrate Google Sign-In SDK here
        Debug.Log("Google Sign-In pressed");
        StartCoroutine(SignInRoutine("google@student.com", "oauth"));
    }

    public void OnAdminLogin()
    {
        // Navigate to admin login panel (not in student scope)
        Debug.Log("Admin login pressed");
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
}
