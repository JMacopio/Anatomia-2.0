using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Model3DViewerUI : MonoBehaviour
{
    [Header("Header")]
    public Button backBtn;
    public TMP_Text systemTitleText;
    public Button expandBtn;

    [Header("Hint")]
    public GameObject tapHintBubble;

    [Header("3D Model")]
    public Transform modelContainer;
    public GameObject[] systemModelPrefabs; // Index matches AnatomySystemData order

    [Header("Structure Info Panel")]
    public GameObject structureInfoPanel;
    public TMP_Text structureNameText;
    public TMP_Text structureDescText;
    public Button closeInfoBtn;

    [Header("Toolbar")]
    public Button resetBtn;
    public Button zoomOutBtn;
    public Button zoomInBtn;
    public Button rotateBtn;
    public Button infoBtn;

    [Header("Camera / Model Settings")]
    public float rotationSpeed = 60f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 0.5f;
    public float maxZoom = 3f;

    private GameObject currentModel;
    private float currentZoom = 1f;
    private bool isFullscreen = false;
    private bool autoRotating = false;
    private AnatomySystemData currentSystem;

    void Start()
    {
        backBtn.onClick.AddListener(() =>
        {
            CleanupModel();
            UIManager.Instance.GoBack();
        });

        expandBtn.onClick.AddListener(ToggleFullscreen);
        closeInfoBtn.onClick.AddListener(() => structureInfoPanel.SetActive(false));
        resetBtn.onClick.AddListener(ResetView);
        zoomInBtn.onClick.AddListener(ZoomIn);
        zoomOutBtn.onClick.AddListener(ZoomOut);
        rotateBtn.onClick.AddListener(ToggleAutoRotate);
        infoBtn.onClick.AddListener(ShowSystemInfo);

        structureInfoPanel.SetActive(false);
        tapHintBubble.SetActive(true);
        StartCoroutine(HideTapHintAfterDelay(4f));
    }

    public void LoadSystem(AnatomySystemData system)
    {
        currentSystem = system;
        systemTitleText.text = system.systemName;
        CleanupModel();

        // Load the 3D prefab for this system
        // In production: use Addressables or Resources.Load based on system.iconKey
        // Example: currentModel = Instantiate(Resources.Load<GameObject>($"Models/{system.iconKey}"), modelContainer);
        // For now, placeholder:
        Debug.Log($"[Model3DViewer] Loading model for: {system.systemName}");

        ResetView();
    }

    void CleanupModel()
    {
        if (currentModel != null)
            Destroy(currentModel);
    }

    void Update()
    {
        if (currentModel == null) return;

        // Touch input: single finger drag = rotate
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                currentModel.transform.Rotate(Vector3.up,
                    -touch.deltaPosition.x * 0.3f, Space.World);
                currentModel.transform.Rotate(Vector3.right,
                    touch.deltaPosition.y * 0.3f, Space.World);
                tapHintBubble.SetActive(false);
            }
            // Tap to select structure
            if (touch.phase == TouchPhase.Ended && touch.deltaPosition.magnitude < 5f)
                TrySelectStructure(touch.position);
        }

        // Two-finger pinch = zoom
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float prevDist = (t0.position - t0.deltaPosition
                - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = currDist - prevDist;
            SetZoom(currentZoom + delta * zoomSpeed * Time.deltaTime);
        }

        // Auto-rotate
        if (autoRotating && currentModel != null)
            currentModel.transform.Rotate(Vector3.up,
                rotationSpeed * Time.deltaTime, Space.World);

        // Editor mouse drag fallback
        if (Application.isEditor && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            if (currentModel != null)
            {
                currentModel.transform.Rotate(Vector3.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World);
                currentModel.transform.Rotate(Vector3.right, mouseY * rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

    void TrySelectStructure(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            StructureInfo info = hit.collider.GetComponent<StructureInfo>();
            if (info != null)
                ShowStructureInfo(info.structureName, info.description);
        }
    }

    void ShowStructureInfo(string name, string description)
    {
        structureNameText.text = name;
        structureDescText.text = description;
        structureInfoPanel.SetActive(true);
    }

    void ShowSystemInfo()
    {
        if (currentSystem != null)
            ShowStructureInfo(currentSystem.systemName,
                $"This system contains {currentSystem.structureCount} anatomical structures. " +
                $"Tap on individual parts to learn more about each structure.");
    }

    void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        if (modelContainer)
            modelContainer.localScale = Vector3.one * currentZoom;
    }

    void ZoomIn() => SetZoom(currentZoom + 0.2f);
    void ZoomOut() => SetZoom(currentZoom - 0.2f);

    void ResetView()
    {
        currentZoom = 1f;
        if (modelContainer)
        {
            modelContainer.localScale = Vector3.one;
            modelContainer.localRotation = Quaternion.identity;
        }
    }

    void ToggleAutoRotate()
    {
        autoRotating = !autoRotating;
        // Optionally tint the rotate button to show active state
    }

    void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        // Hide/show UI overlays for fullscreen mode
        tapHintBubble.SetActive(!isFullscreen);
    }

    IEnumerator HideTapHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        tapHintBubble.SetActive(false);
    }
}
