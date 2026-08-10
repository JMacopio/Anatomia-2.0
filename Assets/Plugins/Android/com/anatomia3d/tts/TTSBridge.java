// ════════════════════════════════════════════════════════════
// TTSBridge.java — Android native plugin
// Place this file at:
// Assets/Plugins/Android/com/anatomia3d/tts/TTSBridge.java
//
// This wraps Android's built-in TextToSpeech engine so Unity
// can call speak/stop/shutdown via AndroidJavaObject.
// No external asset store plugin required — pure Android SDK.
// ════════════════════════════════════════════════════════════

package com.anatomia3d.tts;

import android.app.Activity;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import java.util.Locale;

public class TTSBridge {

    private TextToSpeech tts;
    private boolean isReady = false;
    private boolean speaking = false;

    public TTSBridge(final Activity activity) {
        tts = new TextToSpeech(activity, new TextToSpeech.OnInitListener() {
            @Override
            public void onInit(int status) {
                if (status == TextToSpeech.SUCCESS) {
                    int result = tts.setLanguage(Locale.US);
                    if (result == TextToSpeech.LANG_MISSING_DATA ||
                        result == TextToSpeech.LANG_NOT_SUPPORTED) {
                        // Fallback to default locale
                        tts.setLanguage(Locale.getDefault());
                    }
                    // Slightly slower rate — easier to follow for students
                    tts.setSpeechRate(0.95f);
                    tts.setPitch(1.0f);
                    isReady = true;
                }
            }
        });

        // Track speaking state via utterance progress
        tts.setOnUtteranceProgressListener(new UtteranceProgressListener() {
            @Override
            public void onStart(String utteranceId) {
                speaking = true;
            }

            @Override
            public void onDone(String utteranceId) {
                speaking = false;
            }

            @Override
            public void onError(String utteranceId) {
                speaking = false;
            }
        });
    }

    public void speak(String text) {
        if (!isReady || tts == null) return;
        tts.stop(); // stop any previous speech first
        tts.speak(text, TextToSpeech.QUEUE_FLUSH, null, "anatomia3d_utterance");
    }

    public void stop() {
        if (tts != null) {
            tts.stop();
            speaking = false;
        }
    }

    public boolean isSpeaking() {
        return speaking;
    }

    public void shutdown() {
        if (tts != null) {
            tts.stop();
            tts.shutdown();
        }
    }
}