using UnityEngine;

public class TextToSpeechManager : MonoBehaviour
{
    public static TextToSpeechManager Instance { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject ttsObject;
    private AndroidJavaObject currentActivity;
    private bool isInitialized = false;
#endif

    public bool isSpeaking = false;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        InitializeTTS();
    }

    void InitializeTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
 
            // Create the TTS bridge class (see TTSPlugin.java below)
            ttsObject = new AndroidJavaObject(
                "com.anatomia3d.tts.TTSBridge", currentActivity);
 
            isInitialized = true;
            Debug.Log("[TTS] Initialized successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TTS] Init failed: " + e.Message);
        }
#else
        Debug.Log("[TTS] Editor mode — TTS calls will be logged only.");
#endif
    }

    // ── Speak the given text ───────────────────────────────────
    public void Speak(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized) return;
        ttsObject.Call("speak", text);
        isSpeaking = true;
#else
        Debug.Log($"[TTS] (Editor Preview) Would speak: \"{text}\"");
        isSpeaking = true;
        // Simulate speaking duration in editor for UI testing
        CancelInvoke(nameof(SimulateSpeechEnd));
        Invoke(nameof(SimulateSpeechEnd), Mathf.Max(2f, text.Length * 0.06f));
#endif
    }

    void SimulateSpeechEnd() => isSpeaking = false;

    // ── Stop current speech ─────────────────────────────────────
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized) return;
        ttsObject.Call("stop");
#else
        CancelInvoke(nameof(SimulateSpeechEnd));
#endif
        isSpeaking = false;
    }

    // ── Check if currently speaking (poll from Update if needed) ─
    public bool IsSpeaking()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized) return false;
        isSpeaking = ttsObject.Call<bool>("isSpeaking");
#endif
        return isSpeaking;
    }

    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (isInitialized) ttsObject?.Call("shutdown");
#endif
    }
}
