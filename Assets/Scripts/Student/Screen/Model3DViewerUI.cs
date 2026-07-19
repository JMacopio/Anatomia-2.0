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

    [Header("3D Display")]
    public Camera modelCamera;      // Second Camera pointing at skeleton
    public Transform modelContainer;   // parent of the skeleton GO
    public RawImage modelRawImage;    // displays the Render Texture

    [Header("Bone Info Panel")]
    public GameObject boneInfoPanel;
    public TMP_Text boneNameText;
    public TMP_Text boneDescText;
    public TMP_Text categoryText;
    public Image categoryTagBG;
    public Button closeInfoBtn;

    [Header("Toolbar")]
    public Button resetBtn;
    public Button zoomInBtn;
    public Button zoomOutBtn;
    public Button rotateBtn;
    public Button infoBtn;
    public Image rotateBtnBG;         // tinted when auto-rotate is ON

    [Header("Touch Settings")]
    public float rotationSpeed = 0.25f;
    public float zoomSpeed = 0.005f;
    public float minScale = 0.4f;
    public float maxScale = 3.5f;
    public float autoRotateSpeed = 30f;

    // ── State ────────────────────────────────────────────────
    private float currentScale = 1f;
    private bool autoRotating = false;
    private bool isFullscreen = false;
    private bool isBoneInfoOpen = false;
    private AnatomySystemData currentSystem;

    // Gesture tracking
    private Vector2 lastSingleTouchPos;
    private float lastPinchDist;
    private bool wasPinching = false;
    private float touchStartTime;
    private Vector2 touchStartPos;
    private const float TAP_MAX_DURATION = 0.25f;
    private const float TAP_MAX_MOVE = 10f;

    // Category colors
    private static readonly System.Collections.Generic.Dictionary<string, Color>
        categoryColors = new System.Collections.Generic.Dictionary<string, Color>
    {
        { "Skull",            new Color(0.49f, 0.23f, 0.93f) }, // purple
        { "Vertebral Column", new Color(0.23f, 0.51f, 0.96f) }, // blue
        { "Thorax",           new Color(0.94f, 0.27f, 0.27f) }, // red
        { "Upper Limb",       new Color(0.13f, 0.69f, 0.30f) }, // green
        { "Pelvis",           new Color(0.95f, 0.61f, 0.07f) }, // amber
        { "Lower Limb",       new Color(0.06f, 0.71f, 0.80f) }, // teal
        { "Skeletal System",  new Color(0.49f, 0.23f, 0.93f) }, // purple
    };

    // ─────────────────────────────────────────────────────────
    void Start()
    {
        // Buttons
        backBtn.onClick.AddListener(OnBack);
        expandBtn?.onClick.AddListener(ToggleFullscreen);
        closeInfoBtn.onClick.AddListener(CloseBoneInfo);
        resetBtn.onClick.AddListener(ResetView);
        zoomInBtn.onClick.AddListener(ZoomIn);
        zoomOutBtn.onClick.AddListener(ZoomOut);
        rotateBtn.onClick.AddListener(ToggleAutoRotate);
        infoBtn?.onClick.AddListener(ShowSystemInfo);

        // Initial state
        boneInfoPanel.SetActive(false);
        StartCoroutine(HideHintAfter(4f));
    }

    // ── Load a system ─────────────────────────────────────────
    public void LoadSystem(AnatomySystemData system)
    {
        currentSystem = system;
        systemTitleText.text = system.systemName;
        boneInfoPanel.SetActive(false);
        ResetView();
        tapHintBubble?.SetActive(true);
        StartCoroutine(HideHintAfter(4f));
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE — Handle all touch input
    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (modelContainer == null) return;

        // Auto-rotate
        if (autoRotating)
            modelContainer.Rotate(Vector3.up,
                autoRotateSpeed * Time.deltaTime, Space.World);

#if UNITY_EDITOR
        HandleEditorMouse();
#else
        HandleTouchInput();
#endif
    }

    // ── Touch Input ──────────────────────────────────────────
    void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        // ── TWO FINGERS — Pinch zoom ─────────────────────────
        if (touchCount == 2)
        {
            autoRotating = false;
            wasPinching = true;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDist = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                lastPinchDist = currentDist;
                return;
            }

            float delta = currentDist - lastPinchDist;
            SetScale(currentScale + delta * zoomSpeed);
            lastPinchDist = currentDist;
            return;
        }

        // ── ONE FINGER — Rotate or Tap ───────────────────────
        if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Check if touch is over UI
            if (IsTouchOverUI(touch.position)) return;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    wasPinching = false;
                    lastSingleTouchPos = touch.position;
                    touchStartTime = Time.time;
                    touchStartPos = touch.position;
                    break;

                case TouchPhase.Moved:
                    if (!wasPinching)
                    {
                        Vector2 delta = touch.position - lastSingleTouchPos;
                        RotateModel(delta);
                        lastSingleTouchPos = touch.position;
                        tapHintBubble?.SetActive(false);
                    }
                    break;

                case TouchPhase.Ended:
                    // Detect tap (short time + small movement)
                    float duration = Time.time - touchStartTime;
                    float moved = Vector2.Distance(touch.position, touchStartPos);

                    if (!wasPinching && duration < TAP_MAX_DURATION
                        && moved < TAP_MAX_MOVE)
                    {
                        TrySelectBone(touch.position);
                    }
                    wasPinching = false;
                    break;
            }
        }

        if (touchCount == 0)
            wasPinching = false;
    }

    // ── Editor Mouse (for testing) ───────────────────────────
    void HandleEditorMouse()
    {
        // Right-click drag = rotate
        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            modelContainer.Rotate(Vector3.up, -mx * 150f * Time.deltaTime, Space.World);
            modelContainer.Rotate(Vector3.right, my * 150f * Time.deltaTime, Space.World);
        }

        // Scroll = zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            SetScale(currentScale + scroll * 0.5f);

        // Left-click = select bone
        if (Input.GetMouseButtonDown(0)
            && !UnityEngine.EventSystems.EventSystem.current
                .IsPointerOverGameObject())
        {
            TrySelectBone(Input.mousePosition);
        }
    }

    // ── Rotate the model ────────────────────────────────────
    void RotateModel(Vector2 delta)
    {
        modelContainer.Rotate(Vector3.up,
            -delta.x * rotationSpeed, Space.World);
        modelContainer.Rotate(Vector3.right,
             delta.y * rotationSpeed, Space.World);
    }

    // ── Try to select a bone via raycast ────────────────────
    void TrySelectBone(Vector2 screenPos)
    {
        if (modelCamera == null) return;

        Ray ray = modelCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            var info = hit.collider.GetComponent<StructureInfo>();
            if (info != null)
            {
                ShowBoneInfo(info);
                return;
            }

            // Hit mesh but no StructureInfo — try parent
            var parentInfo = hit.collider
                .GetComponentInParent<StructureInfo>();
            if (parentInfo != null)
                ShowBoneInfo(parentInfo);
        }
        else
        {
            // Tapped empty space — close panel
            if (isBoneInfoOpen) CloseBoneInfo();
        }
    }

    // ── Show Bone Info Panel ─────────────────────────────────
    void ShowBoneInfo(StructureInfo info)
    {
        boneNameText.text = info.structureName;
        boneDescText.text = info.description;

        // Category tag color
        if (categoryText) categoryText.text = info.category.ToUpper();
        if (categoryTagBG && categoryColors.ContainsKey(info.category))
            categoryTagBG.color = categoryColors[info.category];

        // Slide up animation
        boneInfoPanel.SetActive(true);
        isBoneInfoOpen = true;
        StopCoroutine(nameof(SlidePanel));
        StartCoroutine(SlidePanel(true));

        tapHintBubble?.SetActive(false);
    }

    void CloseBoneInfo()
    {
        StartCoroutine(SlidePanel(false));
        isBoneInfoOpen = false;
    }

    void ShowSystemInfo()
    {
        if (currentSystem == null) return;
        var fakeInfo = new StructureInfo
        {
            structureName = currentSystem.systemName,
            description = $"The {currentSystem.systemName} contains " +
                            $"{currentSystem.structureCount} anatomical structures. " +
                            "Tap any bone to learn about it.",
            category = "Skeletal System"
        };
        ShowBoneInfo(fakeInfo);
    }

    // ── Slide animation for info panel ──────────────────────
    IEnumerator SlidePanel(bool slideUp)
    {
        var rect = boneInfoPanel.GetComponent<RectTransform>();
        float panelH = rect.rect.height;
        float start = slideUp ? -panelH : 0f;
        float end = slideUp ? 0f : -panelH;
        float elapsed = 0f;
        float dur = 0.25f;

        if (slideUp) boneInfoPanel.SetActive(true);

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(start, end, t));
            yield return null;
        }

        rect.anchoredPosition = new Vector2(0f, end);
        if (!slideUp) boneInfoPanel.SetActive(false);
    }

    // ── Scale (Zoom) ─────────────────────────────────────────
    void SetScale(float scale)
    {
        currentScale = Mathf.Clamp(scale, minScale, maxScale);
        modelContainer.localScale = Vector3.one * currentScale;
    }

    void ZoomIn() => SetScale(currentScale + 0.2f);
    void ZoomOut() => SetScale(currentScale - 0.2f);

    // ── Reset ────────────────────────────────────────────────
    void ResetView()
    {
        currentScale = 1f;
        if (modelContainer)
        {
            modelContainer.localScale = Vector3.one;
            modelContainer.localRotation = Quaternion.identity;
            modelContainer.localPosition = Vector3.zero;
        }
        CloseBoneInfo();
        autoRotating = false;
        UpdateRotateBtnColor();
    }

    // ── Auto Rotate ──────────────────────────────────────────
    void ToggleAutoRotate()
    {
        autoRotating = !autoRotating;
        UpdateRotateBtnColor();
    }

    void UpdateRotateBtnColor()
    {
        if (rotateBtnBG)
            rotateBtnBG.color = autoRotating
                ? new Color(0.49f, 0.23f, 0.93f, 0.3f) // purple tint = ON
                : new Color(1f, 1f, 1f, 0f);            // transparent = OFF
    }

    // ── Fullscreen ───────────────────────────────────────────
    void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        tapHintBubble?.SetActive(!isFullscreen);
    }

    // ── UI touch check ───────────────────────────────────────
    bool IsTouchOverUI(Vector2 screenPos)
    {
        var pointer = new UnityEngine.EventSystems
            .PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        { position = screenPos };
        var results = new System.Collections.Generic.List<
            UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current
            .RaycastAll(pointer, results);
        return results.Count > 0;
    }

    void OnBack()
    {
        autoRotating = false;
        CloseBoneInfo();
        UIManager.Instance.GoBack();
    }

    IEnumerator HideHintAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        tapHintBubble?.SetActive(false);
    }
}