using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
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
    public Camera modelCamera;
    public Transform modelContainer;
    public RawImage modelRawImage;

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
    public Image rotateBtnBG;

    [Header("Touch Settings")]
    public float rotationSpeed = 0.25f;
    public float zoomStep = 0.25f;
    public float minZoom = 0.4f;
    public float maxZoom = 3.5f;
    public float autoRotateSpeed = 30f;

    private float currentZoom = 1f;
    private bool autoRotating = false;
    private bool isBoneInfoOpen = false;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private AnatomySystemData currentSystem;

    private Vector2 lastSinglePos;
    private float lastPinchDist;
    private bool wasPinching = false;
    private float touchStartTime;
    private Vector2 touchStartPos;
    private const float TAP_DURATION = 0.25f;
    private const float TAP_MOVE = 15f;

    private static readonly Dictionary<string, Color> catColors =
        new Dictionary<string, Color>
    {
        { "Skull",            new Color(0.49f, 0.23f, 0.93f) },
        { "Vertebral Column", new Color(0.23f, 0.51f, 0.96f) },
        { "Thorax",           new Color(0.94f, 0.27f, 0.27f) },
        { "Upper Limb",       new Color(0.13f, 0.69f, 0.30f) },
        { "Pelvis",           new Color(0.95f, 0.61f, 0.07f) },
        { "Lower Limb",       new Color(0.06f, 0.71f, 0.80f) },
        { "Skeletal System",  new Color(0.49f, 0.23f, 0.93f) },
    };

    void Start()
    {
        backBtn.onClick.AddListener(OnBack);
        closeInfoBtn.onClick.AddListener(CloseBoneInfo);
        resetBtn.onClick.AddListener(ResetView);
        zoomInBtn.onClick.AddListener(ZoomIn);
        zoomOutBtn.onClick.AddListener(ZoomOut);
        rotateBtn.onClick.AddListener(ToggleAutoRotate);
        infoBtn?.onClick.AddListener(ShowSystemInfo);
        expandBtn?.onClick.AddListener(ToggleFullscreen);

        if (boneInfoPanel) boneInfoPanel.SetActive(false);
        StartCoroutine(HideHintAfter(4f));
    }

    public void LoadSystem(AnatomySystemData system)
    {
        currentSystem = system;
        systemTitleText.text = system.systemName;
        if (boneInfoPanel) boneInfoPanel.SetActive(false);
        isBoneInfoOpen = false;
        if (modelContainer != null && gameObject.activeInHierarchy) //added && gameObject.activeInHierarchy
            StartCoroutine(AutoFrameModel());
        tapHintBubble?.SetActive(true);
        if (gameObject.activeInHierarchy) //added this if (gameObject.activeInHierarchy)
            StartCoroutine(HideHintAfter(4f));
    }

    // AUTO-FRAME: moves camera to fit model regardless of FBX position
    //IEnumerator AutoFrameModel()
    public IEnumerator AutoFramePublic() => AutoFrameModel();
    IEnumerator AutoFrameModel()
    {
        yield return null; // wait one frame

        if (modelCamera == null || modelContainer == null) yield break;

        var renderers = modelContainer.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[3DViewer] No renderers found in modelContainer.");
            yield break;
        }

        // Calculate total bounds
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        Vector3 center = bounds.center;
        float size = bounds.size.magnitude;
        float fovRad = modelCamera.fieldOfView * Mathf.Deg2Rad;
        float distance = (size * 0.6f) / Mathf.Tan(fovRad * 0.5f);
        distance = Mathf.Max(distance, 0.5f);

        // Position camera in front of model
        modelCamera.transform.position = center + new Vector3(0f, 0f, -distance);
        modelCamera.transform.LookAt(center);

        // Extend far clip to cover full model
        modelCamera.farClipPlane = Mathf.Max(distance + size + 50f, 200f);

        rotationX = 0f;
        rotationY = 0f;
        currentZoom = 1f;
        modelContainer.localScale = Vector3.one;
        modelContainer.localRotation = Quaternion.identity;

        Debug.Log($"[3DViewer] AutoFrame: center={center} size={size:F1} dist={distance:F1}");
    }

    void Update()
    {
        if (modelContainer == null) return;

        if (autoRotating)
        {
            rotationY += autoRotateSpeed * Time.deltaTime;
            ApplyRotation();
        }

#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    void HandleTouchInput()
    {
        int count = Input.touchCount;

        if (count == 2)
        {
            autoRotating = false;
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            { lastPinchDist = dist; wasPinching = true; return; }

            SetZoom(currentZoom + (dist - lastPinchDist) * 0.002f);
            lastPinchDist = dist;
            return;
        }

        if (count == 1)
        {
            Touch t = Input.GetTouch(0);
            if (IsTouchOverUI(t.position)) return;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    wasPinching = false;
                    lastSinglePos = t.position;
                    touchStartTime = Time.time;
                    touchStartPos = t.position;
                    break;
                case TouchPhase.Moved:
                    if (!wasPinching)
                    {
                        Vector2 d = t.position - lastSinglePos;
                        rotationY += d.x * rotationSpeed;
                        rotationX -= d.y * rotationSpeed;
                        rotationX = Mathf.Clamp(rotationX, -70f, 70f);
                        ApplyRotation();
                        lastSinglePos = t.position;
                        tapHintBubble?.SetActive(false);
                    }
                    break;
                case TouchPhase.Ended:
                    if (!wasPinching
                        && Time.time - touchStartTime < TAP_DURATION
                        && Vector2.Distance(t.position, touchStartPos) < TAP_MOVE)
                        TrySelectBone(t.position);
                    wasPinching = false;
                    break;
            }
        }

        if (count == 0) wasPinching = false;
    }

    void HandleMouseInput()
    {
        // Left mouse button drag = rotate (same as single finger)
        if (Input.GetMouseButton(0) && !IsMouseOverButton())
        {
            rotationY += Input.GetAxis("Mouse X") * 150f * Time.deltaTime;
            rotationX -= Input.GetAxis("Mouse Y") * 150f * Time.deltaTime;
            rotationX = Mathf.Clamp(rotationX, -70f, 70f);
            ApplyRotation();
        }

        // Scroll = zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            SetZoom(currentZoom + scroll * 0.5f);

        // Left click (no drag) = select bone
        //if (Input.GetMouseButtonDown(0))
        //{
        //    Debug.Log("[3DViewer] Mouse clicked at: " + Input.mousePosition);
        //    bool overUI = IsMouseOverUI();
        //    Debug.Log("[3DViewer] IsMouseOverUI: " + overUI);
        //    if (!overUI)
        //        TrySelectBone(Input.mousePosition);
        //}
        // Left click = select bone
        // We skip the IsMouseOverUI check here because the RawImage
        // itself counts as UI. TrySelectBone handles out-of-bounds clicks.
        // Only block clicks on actual buttons (toolbar, back, close)
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[3DViewer] Mouse clicked at: " + Input.mousePosition);

            // Only block if clicking on a Button — not on RawImage or panels
            bool overButton = IsMouseOverButton();
            Debug.Log("[3DViewer] IsMouseOverButton: " + overButton);

            if (!overButton)
                TrySelectBone(Input.mousePosition);
        }
    }

    void ApplyRotation()
    {
        if (modelContainer)
            modelContainer.localRotation =
                Quaternion.Euler(rotationX, rotationY, 0f);
    }

    // BONE SELECTION — converts touch through RawImage to 3D ray
    void TrySelectBone(Vector2 screenPos)
    {
        if (modelCamera == null) return;

        Ray ray;

        if (modelRawImage != null)
        {
            // Convert screen position to UV inside RawImage
            RectTransform rt = modelRawImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rt, screenPos, null, out Vector2 local))
            {
                Debug.Log("[3DViewer] Touch outside RawImage.");
                return;
            }

            Rect rect = rt.rect;
            if (local.x < rect.xMin || local.x > rect.xMax ||
                local.y < rect.yMin || local.y > rect.yMax)
            {
                if (isBoneInfoOpen) CloseBoneInfo();
                return;
            }

            float u = (local.x - rect.xMin) / rect.width;
            float v = (local.y - rect.yMin) / rect.height;
            ray = modelCamera.ViewportPointToRay(new Vector3(u, v, 0f));
            Debug.Log($"[3DViewer] UV Ray: ({u:F2}, {v:F2})");
        }
        else
        {
            // Fallback if RawImage not assigned
            ray = modelCamera.ScreenPointToRay(screenPos);
            Debug.Log("[3DViewer] Direct screen ray (assign RawImage for accuracy)");
        }

        // Layer mask
        int mask = LayerMask.GetMask("SkeletonModel");
        if (mask == 0)
        {
            Debug.LogWarning("[3DViewer] SkeletonModel layer missing! Using all layers.");
            mask = ~0;
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 5000f, mask))
        {
            Debug.Log($"[3DViewer] HIT: {hit.collider.name}");
            var info = hit.collider.GetComponent<StructureInfo>()
                    ?? hit.collider.GetComponentInParent<StructureInfo>();
            if (info != null)
                ShowBoneInfo(info);
            else
                Debug.Log($"[3DViewer] No StructureInfo on {hit.collider.name}. Run Auto Setup.");
        }
        else
        {
            Debug.Log("[3DViewer] No hit.");
            if (isBoneInfoOpen) CloseBoneInfo();
        }
    }

    //added
    void ShowBoneInfo(string name, string description, string category)
    {
        //if (boneInfoPanel == null) return;
        //boneNameText.text = name;
        //boneDescText.text = description;
        //if (categoryText) categoryText.text = category.ToUpper();
        //if (categoryTagBG && catColors.ContainsKey(category))
        //    categoryTagBG.color = catColors[category];

        //boneInfoPanel.SetActive(true);
        //isBoneInfoOpen = true;
        //var rect = boneInfoPanel.GetComponent<RectTransform>();
        //if (rect) rect.anchoredPosition = Vector2.zero;
        //tapHintBubble?.SetActive(false);
        if (boneInfoPanel == null) return;

        boneNameText.text = name;
        boneDescText.text = description;
        if (categoryText) categoryText.text = category.ToUpper();
        if (categoryTagBG && catColors.ContainsKey(category))
            categoryTagBG.color = catColors[category];

        isBoneInfoOpen = true;
        tapHintBubble?.SetActive(false);

        //Stop any running animation, then slide up
        StopCoroutine(nameof(SlidePanel));
        StartCoroutine(SlidePanel(true));

    }

    void ShowBoneInfo(StructureInfo info)
    {
        //if (boneInfoPanel == null) return;
        //boneNameText.text = info.structureName;
        //boneDescText.text = info.description;
        //if (categoryText) categoryText.text = info.category.ToUpper();
        //if (categoryTagBG && catColors.ContainsKey(info.category))
        //    categoryTagBG.color = catColors[info.category];

        //boneInfoPanel.SetActive(true);
        //isBoneInfoOpen = true;
        //var rect = boneInfoPanel.GetComponent<RectTransform>();
        //if (rect) rect.anchoredPosition = Vector2.zero;
        //tapHintBubble?.SetActive(false);

        ShowBoneInfo(info.structureName, info.description, info.category);
    }


    void CloseBoneInfo()
    {
        //if (boneInfoPanel) boneInfoPanel.SetActive(false);
        //isBoneInfoOpen = false;

        //Stop any running animation, then slide down
        StopCoroutine(nameof(SlidePanel));
        StartCoroutine(SlidePanel(false));
    }

    // Slide panel up from bottom / slide down off screen
    IEnumerator SlidePanel(bool slideUp)
    {
        if (boneInfoPanel == null) yield break;

        var rect = boneInfoPanel.GetComponent<RectTransform>();
        float height = rect.rect.height > 0 ? rect.rect.height : 220f;
        float fromY = slideUp ? -height : 0f;
        float toY = slideUp ? 0f : -height;
        float duration = 0.25f;
        float elapsed = 0f;

        if (slideUp) boneInfoPanel.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(fromY, toY, t));
            yield return null;
        }

        rect.anchoredPosition = new Vector2(0f, toY);
        if (!slideUp) boneInfoPanel.SetActive(false);
    }

    void ShowSystemInfo()
    {
        //if (currentSystem == null) return;
        //ShowBoneInfo(new StructureInfo
        //{
        //    structureName = currentSystem.systemName,
        //    description = $"{currentSystem.systemName} has " +
        //                    $"{currentSystem.structureCount} structures. " +
        //                    "Tap any bone to learn more.",
        //    category = "Skeletal System"
        //});
        if (currentSystem == null) return;
        ShowBoneInfo(
            currentSystem.systemName,
            $"{currentSystem.systemName} has {currentSystem.structureCount} structures. Tap any bone to learn more.",
            "Skeletal System"
        );
    }

    void SetZoom(float z)
    {
        currentZoom = Mathf.Clamp(z, minZoom, maxZoom);
        if (modelContainer)
            modelContainer.localScale = Vector3.one * currentZoom;
    }

    void ZoomIn() => SetZoom(currentZoom + zoomStep);
    void ZoomOut() => SetZoom(currentZoom - zoomStep);

    void ResetView()
    {
        rotationX = rotationY = 0f;
        currentZoom = 1f;
        autoRotating = false;
        UpdateRotateBtnVisual();
        if (modelContainer)
        {
            modelContainer.localRotation = Quaternion.identity;
            modelContainer.localScale = Vector3.one;
        }
        if (modelContainer != null && gameObject.activeInHierarchy) //added && gameObject.activeInHierarchy
            StartCoroutine(AutoFrameModel());
        CloseBoneInfo();
    }

    void ToggleAutoRotate()
    {
        autoRotating = !autoRotating;
        UpdateRotateBtnVisual();
    }

    void UpdateRotateBtnVisual()
    {
        if (rotateBtnBG)
            rotateBtnBG.color = autoRotating
                ? new Color(0.49f, 0.23f, 0.93f, 0.35f)
                : new Color(1f, 1f, 1f, 0f);
    }

    void ToggleFullscreen() => tapHintBubble?.SetActive(false);

    bool IsTouchOverUI(Vector2 pos)
    {
        var pe = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current)
        { position = pos };
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pe, results);
        foreach (var r in results)
            if (r.gameObject.GetComponent<RawImage>() == null) return true;
        return false;
    }

    //bool IsMouseOverUI() =>
    //    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    //bool IsMouseOverUI()
    //{
    //    // Same as IsTouchOverUI but for mouse — excludes RawImage
    //    // so clicks on the 3D model view pass through correctly
    //    var pe = new UnityEngine.EventSystems.PointerEventData(
    //        UnityEngine.EventSystems.EventSystem.current)
    //    { position = Input.mousePosition };
    //    var results = new List<UnityEngine.EventSystems.RaycastResult>();
    //    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pe, results);
    //    foreach (var r in results)
    //        if (r.gameObject.GetComponent<RawImage>() == null) return true;
    //    return false;
    //}
    // Checks only for Buttons — allows clicks through panels and RawImage
    bool IsMouseOverButton()
    {
        var pe = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current)
        { position = Input.mousePosition };
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pe, results);
        foreach (var r in results)
        {
            // Block if clicking an actual interactable button
            var btn = r.gameObject.GetComponent<UnityEngine.UI.Button>();
            if (btn != null && btn.interactable) return true;
        }
        return false;
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