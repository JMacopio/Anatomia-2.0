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

    [Header("Uploading State")]
    public GameObject uploadingOverlay;
    public TMP_Text uploadingText;

    [Header("References")]
    public Model3DViewerUI viewer;      // the existing 3D viewer script
    public GameObject studentToolbar; // hide student toolbar while capturing
    public GameObject boneInfoPanel;  // hide bone info panel while capturing

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
        uploadingOverlay?.SetActive(false);
    }

    // ── Called by AddQuestionModalUI when admin taps "Capture" ───
    public void EnterCaptureMode(System.Action<bool, string, string> callback)
    {
        onCaptureComplete = callback;
        isInCaptureMode = true;

        // Show the Model3DPanel
        UIManager.Instance.ShowPanel(UIManager.Instance.model3DPanel);

        // Show capture overlay, hide student-only UI
        captureModeOverlay?.SetActive(true);
        studentToolbar?.SetActive(false);   // hide bottom nav during capture
        boneInfoPanel?.SetActive(false);

        if (captureHintText)
            captureHintText.text =
                "Rotate, zoom, and tap structures to frame the shot.\n" +
                "Tap \"Use This View\" when ready.";
    }

    void OnUseThisView()
    {
        //if (ModelImageCapture.Instance == null)
        //{
        //    FinishCapture(false, null, "Capture system not found.");
        //    return;
        //}

        uploadingOverlay?.SetActive(true);
        if (uploadingText) uploadingText.text = "Capturing...";

        StartCoroutine(CaptureAndUploadRoutine());
    }

    IEnumerator CaptureAndUploadRoutine()
    {
        // Give one frame for any UI overlay to visually clear
        // from the render before capturing
        captureModeOverlay?.SetActive(false);
        yield return new WaitForEndOfFrame();

        if (uploadingText) uploadingText.text = "Uploading...";

        bool done = false;
        bool success = false;
        string url = null;
        string error = null;

        //ModelImageCapture.Instance.CaptureAndUpload(
        //    questionId: System.Guid.NewGuid().ToString(),
        //    onSuccess: (resultUrl) =>
        //    {
        //        success = true;
        //        url = resultUrl;
        //        done = true;
        //    },
        //    onError: (errMsg) =>
        //    {
        //        success = false;
        //        error = errMsg;
        //        done = true;
        //    });

        // Wait for upload to complete
        while (!done) yield return null;

        uploadingOverlay?.SetActive(false);
        FinishCapture(success, url, error);
    }

    void OnCancelCapture()
    {
        FinishCapture(false, null, null); // null error = user cancelled, no error shown
    }

    void FinishCapture(bool success, string url, string error)
    {
        isInCaptureMode = false;

        captureModeOverlay?.SetActive(false);
        uploadingOverlay?.SetActive(false);
        studentToolbar?.SetActive(true);

        // Go back to admin panel
        UIManager.Instance.GoBack();

        // Notify the modal
        onCaptureComplete?.Invoke(success, url, error);
        onCaptureComplete = null;
    }

    public bool IsInCaptureMode() => isInCaptureMode;
}