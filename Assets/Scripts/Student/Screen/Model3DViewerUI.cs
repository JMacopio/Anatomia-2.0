using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Model3DViewerUI : MonoBehaviour
{
    [Header("3D Models — add one per anatomy system")]
    public GameObject skeletalModel;    
    public GameObject muscularModel;
    public GameObject cardiovascularModel;

    [Header("Highlight Settings")]
    public Material highlightMaterial;      // drag your glow material here
    public Color highlightColor = new Color(0.4f, 0.8f, 1.0f, 1f); // cyan-blue glow
    public float highlightIntensity = 2.5f; // emission intensity
    public bool pulseHighlight = true;  // animate the glow

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

    //added for auto-zoom when click or press
    [Header("Auto Zoom Settings")]
    public float zoomInFOV = 30f;   // FOV when zoomed in (smaller = closer)
    public float zoomOutFOV = 60f;   // FOV when zoomed out (default)
    public float zoomDuration = 0.5f;  // seconds to complete zoom
    public float zoomInDistance = 0.4f;  // how much to move camera toward bone (0-1)

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

    private Coroutine slidePanelCoroutine;

    private Renderer highlightedRenderer;
    private Material[] originalMaterials;
    private Material runtimeHighlightMat; // created at runtime
    private Coroutine pulseCoroutine;

    // Private zoom state
    private Vector3 originalCameraPos;
    private float originalFOV;
    private Vector3 zoomedCameraPos;
    private bool isZoomed = false;
    private Coroutine zoomCoroutine;

    void Start()
    {
        InitHighlightMaterial();
        originalCameraPos = modelCamera.transform.position;
        originalFOV       = modelCamera.fieldOfView;
        zoomOutFOV        = originalFOV;

        backBtn.onClick.AddListener(OnBack);
        closeInfoBtn.onClick.AddListener(CloseBoneInfo);
        resetBtn.onClick.AddListener(ResetView);
        zoomInBtn.onClick.AddListener(ZoomIn);
        zoomOutBtn.onClick.AddListener(ZoomOuttab);
        rotateBtn.onClick.AddListener(ToggleAutoRotate);
        infoBtn?.onClick.AddListener(ShowSystemInfo);
        expandBtn?.onClick.AddListener(ToggleFullscreen);

        if (boneInfoPanel) boneInfoPanel.SetActive(false);
        StartCoroutine(HideHintAfter(4f));
    }

    public void LoadSystem(AnatomySystemData system)
    {
        Debug.Log($"[Model3DViewer] Comparing: '{system.systemName}' == 'Muscular System'? {system.systemName == "Muscular System"}");
        currentSystem = system;
        systemTitleText.text = system.systemName;

        // ── Swap highlight color per system ─────────────────── added
        highlightColor = HighlightMaterialFactory
            .GetSystemColor(system.systemName);
        InitHighlightMaterial(); // recreate material with new color

        //added this part and switch statement to show correct model based on system name
        // Hide all models first
        if (skeletalModel) skeletalModel.SetActive(false);
        if (muscularModel) muscularModel.SetActive(false);
        if (cardiovascularModel) cardiovascularModel.SetActive(false);

        // Show correct model based on system name
        switch (system.systemName)
        {
            case "Skeletal System":
                Debug.Log("[Model3DViewer] → Skeletal case");
                if (skeletalModel) skeletalModel.SetActive(true);
                break;
            case "Muscular System":
                Debug.Log("[Model3DViewer] → Muscular case");
                if (muscularModel) muscularModel.SetActive(true);
                break;
            case "Cardiovascular System":
                Debug.Log("[Model3DViewer] → Cardiovascular case");
                if (cardiovascularModel) cardiovascularModel.SetActive(true);
                break;
            default:
                // Fallback — show skeletal
                Debug.Log($"[Model3DViewer] → Default case! systemName = '{system.systemName}'");
                if (skeletalModel) skeletalModel.SetActive(true);
                break;
        }

        // ── Reset state ───────────────────────────────────────
        RestoreHighlight(); //added

        if (boneInfoPanel) boneInfoPanel.SetActive(false);
        isBoneInfoOpen = false;
        
        if (modelContainer != null && gameObject.activeInHierarchy) //added && gameObject.activeInHierarchy
            StartCoroutine(AutoFrameModel());

        // Save post-frame camera state as the "home" position
        originalCameraPos = modelCamera.transform.position;
        originalFOV = modelCamera.fieldOfView;
        zoomOutFOV = originalFOV;
        isZoomed = false;

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
                //ShowBoneInfo(info);
                ShowBoneInfo(info, hit);           // for sphereHit
                //ShowBoneInfo(info, rayHit);        // for rayHit
                //ShowBoneInfo(info, bigSphereHit);  // for bigSphereHit
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
        //StopCoroutine(nameof(SlidePanel));
        //StartCoroutine(SlidePanel(true));
        // FIXED — stop using string, use reference instead
        boneInfoPanel.SetActive(true);
        if (slidePanelCoroutine != null) StopCoroutine(slidePanelCoroutine);
        slidePanelCoroutine = StartCoroutine(SlidePanel(true));

    }

    void ShowBoneInfo(StructureInfo info, RaycastHit hit)
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

        // ── Highlight the tapped bone ─────────────────────────
        HighlightBone(hit);

        // Zoom in toward the hit bone
       // ZoomIn(hit.point);
    }


    void CloseBoneInfo()
    {
        //if (boneInfoPanel) boneInfoPanel.SetActive(false);
        //isBoneInfoOpen = false;

        //Stop any running animation, then slide down
        //StopCoroutine(nameof(SlidePanel));
        //StartCoroutine(SlidePanel(false));

        if (!isBoneInfoOpen) return;  // prevent double-close
        isBoneInfoOpen = false;       // set false immediately

        // ── Restore bone to original appearance ──────────────
        RestoreHighlight();

        //ZoomOut();  // reset zoom when closing info

        // FIXED — stop using string, use reference instead
        if (slidePanelCoroutine != null) StopCoroutine(slidePanelCoroutine);
        slidePanelCoroutine = StartCoroutine(SlidePanel(false));
    }

    // Slide panel up from bottom / slide down off screen
    IEnumerator SlidePanel(bool slideUp)
    {
        if (boneInfoPanel == null) yield break;

        var rect = boneInfoPanel.GetComponent<RectTransform>();
        float height = rect.rect.height > 0 ? rect.rect.height : 220f;
        //float fromY = slideUp ? -height : 0f;
        //float toY = slideUp ? 0f : -height;

        // NEW — uses panel's designed Y position (233)
        float targetY = 233f;  // match the Inspector value
        float fromY = slideUp ? -(height + targetY) : targetY;
        float toY = slideUp ? targetY : -(height + targetY);

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
    void ZoomOuttab() => SetZoom(currentZoom - zoomStep);

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

        // ADD: reset zoom
        if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
        if (modelCamera != null)
        {
            modelCamera.transform.position = originalCameraPos;
            modelCamera.fieldOfView = zoomOutFOV;
            if (modelContainer != null)
                modelCamera.transform.LookAt(modelContainer.position);
        }
        isZoomed = false;
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

    // Creates the highlight material at runtime if none is assigned
    void InitHighlightMaterial()
    {
        if (highlightMaterial != null)
        {
            // Use assigned material as base
            runtimeHighlightMat = new Material(highlightMaterial);
        }
        else
        {
            // Create a simple URP Lit material with emission
            runtimeHighlightMat = new Material(
                Shader.Find("Universal Render Pipeline/Lit"));

            if (runtimeHighlightMat == null)
            {
                // Fallback to standard if URP not found
                runtimeHighlightMat = new Material(
                    Shader.Find("Standard"));
            }
        }

        // Enable emission
        runtimeHighlightMat.EnableKeyword("_EMISSION");
        runtimeHighlightMat.SetColor("_BaseColor", highlightColor);
        runtimeHighlightMat.SetColor("_Color", highlightColor);
        runtimeHighlightMat.SetColor("_EmissionColor",
            highlightColor * highlightIntensity);
        runtimeHighlightMat.SetFloat("_Smoothness", 0.8f);
        runtimeHighlightMat.SetFloat("_Metallic", 0.2f);
    }

    // Highlights the bone that was hit by the raycast
    void HighlightBone(RaycastHit hit)
    {
        // Restore any previously highlighted bone first
        RestoreHighlight();

        // Get the renderer from the hit collider or its parent
        Renderer rend = hit.collider.GetComponent<Renderer>()
                     ?? hit.collider.GetComponentInParent<Renderer>();

        if (rend == null) return;

        // Save original materials so we can restore them later
        highlightedRenderer = rend;
        originalMaterials = rend.sharedMaterials;

        // Apply highlight material to all material slots
        Material[] highlighted = new Material[rend.sharedMaterials.Length];
        for (int i = 0; i < highlighted.Length; i++)
            highlighted[i] = runtimeHighlightMat;
        rend.materials = highlighted;

        // Start pulse animation if enabled
        if (pulseHighlight)
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseHighlight());
        }
    }


    // Restores the bone to its original materials
    void RestoreHighlight()
    {
        // Stop pulse animation
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Restore original materials
        if (highlightedRenderer != null && originalMaterials != null)
        {
            highlightedRenderer.sharedMaterials = originalMaterials;
            highlightedRenderer = null;
            originalMaterials = null;
        }
    }


    // Animates the highlight emission to pulse/glow in and out
    IEnumerator PulseHighlight()
    {
        float time = 0f;
        float speed = 2f; // pulse speed

        while (true)
        {
            time += Time.deltaTime * speed;

            // Oscillate intensity between base and boosted
            float t = (Mathf.Sin(time) + 1f) * 0.5f;
            float intensity = Mathf.Lerp(1.5f, highlightIntensity, t);

            if (runtimeHighlightMat != null)
            {
                runtimeHighlightMat.SetColor("_EmissionColor",
                    highlightColor * intensity);
            }

            yield return null;
        }
    }

    /// <summary>
    /// Returns true if a 3D model is assigned for the given system
    /// </summary>
    public bool HasModelForSystem(string systemName)
    {
        return systemName switch
        {
            "Skeletal System" => skeletalModel != null,
            "Muscular System" => muscularModel != null,
            "Cardiovascular System" => cardiovascularModel != null,
            _ => false
        };
    }

    // Smoothly zooms camera in toward the tapped bone's world position
    //void ZoomIn(Vector3 boneWorldPos)
    //{
    //    if (modelCamera == null) return;

    //    // Calculate target camera position — move closer to the bone
    //    Vector3 dirToBone = (boneWorldPos - modelCamera.transform.position).normalized;
    //    float currentDist = Vector3.Distance(modelCamera.transform.position, boneWorldPos);
    //    float targetDist = currentDist * (1f - zoomInDistance);

    //    zoomedCameraPos = modelCamera.transform.position
    //                    + dirToBone * (currentDist - targetDist);

    //    // Start zoom animation
    //    if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
    //    zoomCoroutine = StartCoroutine(
    //        AnimateZoom(modelCamera.transform.position, zoomedCameraPos,
    //                    modelCamera.fieldOfView, zoomInFOV));

    //    isZoomed = true;
    //}

    // Smoothly zooms camera back to original position and FOV
    //void ZoomOut()
    //{
    //    if (modelCamera == null || !isZoomed) return;

    //    if (zoomCoroutine != null) StopCoroutine(zoomCoroutine);
    //    zoomCoroutine = StartCoroutine(
    //        AnimateZoom(modelCamera.transform.position, originalCameraPos,
    //                    modelCamera.fieldOfView, zoomOutFOV));

    //    isZoomed = false;
    //}

    // Coroutine that smoothly animates camera position and FOV

    //IEnumerator AnimateZoom(Vector3 fromPos, Vector3 toPos,
    //                     float fromFOV, float toFOV)
    //{
    //    float elapsed = 0f;

    //    while (elapsed < zoomDuration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomDuration);

    //        // Animate position
    //        modelCamera.transform.position =
    //            Vector3.Lerp(fromPos, toPos, t);

    //        // Animate FOV (field of view = zoom)
    //        modelCamera.fieldOfView =
    //            Mathf.Lerp(fromFOV, toFOV, t);

    //        // Keep camera looking at model center
    //        if (modelContainer != null)
    //            modelCamera.transform.LookAt(modelContainer.position);

    //        yield return null;
    //    }

    //    // Snap to final values
    //    modelCamera.transform.position = toPos;
    //    modelCamera.fieldOfView = toFOV;

    //    if (modelContainer != null)
    //        modelCamera.transform.LookAt(modelContainer.position);
    //}
}