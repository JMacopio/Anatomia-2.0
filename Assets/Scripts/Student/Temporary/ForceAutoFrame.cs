using UnityEngine;

public class ForceAutoFrame : MonoBehaviour
{
    void Start()
    {
        var viewer = FindObjectOfType<Model3DViewerUI>();
        if (viewer != null)
            StartCoroutine(viewer.AutoFramePublic());
    }
}
