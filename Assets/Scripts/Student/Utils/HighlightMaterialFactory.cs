using UnityEngine;

public class HighlightMaterialFactory : MonoBehaviour
{
    /// <summary>
    /// Creates a URP emissive highlight material at runtime
    /// </summary>
    public static Material Create(Color baseColor, float emissionIntensity)
    {
        // Try URP shader first
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        // Fallback to Standard if URP not found
        if (shader == null)
            shader = Shader.Find("Standard");

        // Final fallback
        if (shader == null)
            shader = Shader.Find("Mobile/Diffuse");

        var mat = new Material(shader);

        // Base color
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color", baseColor); // for Standard shader

        // Enable and set emission
        mat.EnableKeyword("_EMISSION");
        Color emissionColor = baseColor * emissionIntensity;
        mat.SetColor("_EmissionColor", emissionColor);

        // Surface settings
        mat.SetFloat("_Smoothness", 0.8f);
        mat.SetFloat("_Glossiness", 0.8f); // Standard shader
        mat.SetFloat("_Metallic", 0.1f);

        return mat;
    }

    /// <summary>
    /// Returns a color suited to the anatomy system being viewed
    /// </summary>
    public static Color GetSystemColor(string systemName)
    {
        return systemName switch
        {
            "Skeletal System" => new Color(0.4f, 0.8f, 1.0f), // cyan-blue
            "Muscular System" => new Color(1.0f, 0.4f, 0.2f), // red-orange
            "Cardiovascular System" => new Color(1.0f, 0.2f, 0.3f), // red
            "Respiratory System" => new Color(0.4f, 0.9f, 0.6f), // green
            "Nervous System" => new Color(0.9f, 0.9f, 0.2f), // yellow
            "Digestive System" => new Color(0.8f, 0.5f, 0.2f), // amber
            _ => new Color(0.4f, 0.8f, 1.0f), // default cyan
        };
    }
}
