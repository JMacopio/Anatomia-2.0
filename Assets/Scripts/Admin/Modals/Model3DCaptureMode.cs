using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Model3DCaptureMode : MonoBehaviour
{
    public static Model3DCaptureMode Instance { get; private set; }

    [Header("Capture Mode UI")]
    public GameObject captureModeOverlay;
    public Button useThisViewBtn;
    public Button cancelCaptureBtn;
    public TMP_Text captureHintText;

    [Header("References")]
    public GameObject studentToolbar;  // hide student toolbar while capturing
    public GameObject boneInfoPanel;   // hide bone info panel while capturing

    // Callback signature: (success, base64Image, errorMessage)
    private System.Action<bool, string, string> onCaptureComplete;
    private bool isInCaptureMode = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        useThisViewBtn.onClick.AddListener(OnUseThisView);
        cancelCaptureBtn.onClick.AddListener(OnCancelCapture);
        captureModeOverlay?.SetActive(false);
    }

    // ── Called by AddQuestionModalUI when admin taps "Capture" ───
    public void EnterCaptureMode(System.Action<bool, string, string> callback)
    {
        onCaptureComplete = callback;
        isInCaptureMode = true;

        UIManager.Instance.ShowPanel(UIManager.Instance.model3DPanel);

        captureModeOverlay?.SetActive(true);
        studentToolbar?.SetActive(false);
        boneInfoPanel?.SetActive(false);

        if (captureHintText)
            captureHintText.text =
                "Rotate, zoom, and tap structures to frame the shot.\n" +
                "Tap \"Use This View\" when ready.";
    }

    // ── Capture is instant — no upload wait, just encode ─────────
    void OnUseThisView()
    {
        if (ModelImageCapture.Instance == null)
        {
            FinishCapture(false, null, "Capture system not found.");
            return;
        }

        StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        // Hide the overlay for one frame so it doesn't appear in the shot
        captureModeOverlay?.SetActive(false);
        yield return new WaitForEndOfFrame();

        string base64 = ModelImageCapture.Instance.CaptureAsBase64();

        if (string.IsNullOrEmpty(base64))
        {
            FinishCapture(false, null, "Failed to capture image.");
            yield break;
        }

        FinishCapture(true, base64, null);
    }

    void OnCancelCapture()
    {
        FinishCapture(false, null, null); // null error = user cancelled quietly
    }

    void FinishCapture(bool success, string base64Image, string error)
    {
        isInCaptureMode = false;

        captureModeOverlay?.SetActive(false);
        studentToolbar?.SetActive(true);

        UIManager.Instance.GoBack();

        onCaptureComplete?.Invoke(success, base64Image, error);
        onCaptureComplete = null;
    }

    public bool IsInCaptureMode() => isInCaptureMode;
}