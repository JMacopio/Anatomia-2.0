using System;
using UnityEngine;

public class ModelImageCapture : MonoBehaviour
{
    public static ModelImageCapture Instance { get; private set; }

    [Header("Capture Settings")]
    public Camera modelCamera;

    // Kept small so the resulting Base64 string stays well under
    // Firestore's 1MB per-document limit even with other fields.
    public int captureWidth = 512;
    public int captureHeight = 512;

    [Range(30, 90)]
    public int jpgQuality = 70; // lower = smaller file, more compression artifacts

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    // CAPTURE — renders the current camera view to a texture
    // ─────────────────────────────────────────────────────────
    public Texture2D CaptureCurrentView()
    {
        if (modelCamera == null)
        {
            Debug.LogError("[ImageCapture] No modelCamera assigned.");
            return null;
        }

        RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);
        RenderTexture previousTarget = modelCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        modelCamera.targetTexture = rt;
        modelCamera.Render();

        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(
            captureWidth, captureHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        screenshot.Apply();

        // Restore camera's original render target
        modelCamera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        rt.Release();
        Destroy(rt);

        return screenshot;
    }

    // ─────────────────────────────────────────────────────────
    // CAPTURE + ENCODE — captures then returns a Base64 JPG string
    // No network call, no storage bucket — instant and free.
    // ─────────────────────────────────────────────────────────
    public string CaptureAsBase64()
    {
        Texture2D screenshot = CaptureCurrentView();
        if (screenshot == null) return null;

        byte[] jpgData = screenshot.EncodeToJPG(jpgQuality);
        Destroy(screenshot);

        string base64 = Convert.ToBase64String(jpgData);

        float sizeKB = jpgData.Length / 1024f;
        Debug.Log($"[ImageCapture] Captured image: {sizeKB:F1} KB " +
                   $"({base64.Length} base64 chars)");

        if (sizeKB > 700f)
            Debug.LogWarning("[ImageCapture] Image is large — consider " +
                "lowering captureWidth/Height or jpgQuality to stay safely " +
                "under Firestore's 1MB document limit.");

        return base64;
    }

    // ─────────────────────────────────────────────────────────
    // Convenience: capture straight to a preview Sprite (no encode)
    // Used for showing the admin a live preview before saving.
    // ─────────────────────────────────────────────────────────
    public Sprite CaptureAsPreviewSprite()
    {
        Texture2D tex = CaptureCurrentView();
        if (tex == null) return null;

        return Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));
    }

    // ─────────────────────────────────────────────────────────
    // Convert a Base64 string BACK into a displayable Texture2D
    // Used on the student side to show the stored question image.
    // ─────────────────────────────────────────────────────────
    public static Texture2D Base64ToTexture(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return null;

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2); // size auto-replaced by LoadImage
            tex.LoadImage(bytes);
            return tex;
        }
        catch (Exception e)
        {
            Debug.LogError("[ImageCapture] Failed to decode Base64 image: " + e.Message);
            return null;
        }
    }
}