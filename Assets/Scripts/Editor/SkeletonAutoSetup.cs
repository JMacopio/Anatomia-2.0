using UnityEditor;
using UnityEngine;

public class SkeletonAutoSetup : EditorWindow
{
    private GameObject skeletonRoot;
    private bool overwriteExisting = false;
    private bool addMeshCollider = true;
    private bool addStructureInfo = true;
    private Vector2 scrollPos;
    private int processedCount = 0;

    [MenuItem("Anatomia 3D/Auto Setup Skeleton")]
    public static void ShowWindow()
    {
        GetWindow<SkeletonAutoSetup>("Skeleton Auto Setup");
    }

    // Also available via right-click on GameObject in Hierarchy
    [MenuItem("GameObject/Anatomia 3D/Auto Setup Skeleton", false, 10)]
    static void SetupFromHierarchy()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select the root skeleton GameObject first.", "OK");
            return;
        }
        var window = GetWindow<SkeletonAutoSetup>("Skeleton Auto Setup");
        window.skeletonRoot = Selection.activeGameObject;
    }

    void OnGUI()
    {
        GUILayout.Label("Anatomia 3D — Skeleton Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        skeletonRoot = (GameObject)EditorGUILayout.ObjectField(
            "Skeleton Root", skeletonRoot, typeof(GameObject), true);

        EditorGUILayout.Space();
        GUILayout.Label("Options", EditorStyles.boldLabel);
        addMeshCollider = EditorGUILayout.Toggle("Add Mesh Colliders", addMeshCollider);
        addStructureInfo = EditorGUILayout.Toggle("Add StructureInfo", addStructureInfo);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will scan every child GameObject with a MeshFilter/SkinnedMeshRenderer " +
            "and add a MeshCollider + StructureInfo component using the GameObject's name.",
            MessageType.Info);

        EditorGUILayout.Space();

        GUI.enabled = skeletonRoot != null;
        if (GUILayout.Button("Run Auto Setup", GUILayout.Height(40)))
            RunSetup();
        GUI.enabled = true;

        if (processedCount > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Done! Processed {processedCount} bone(s).", MessageType.None);
        }
    }

    void RunSetup()
    {
        if (skeletonRoot == null) return;

        processedCount = 0;
        Undo.RegisterFullObjectHierarchyUndo(skeletonRoot, "Auto Setup Skeleton");

        // Get ALL children recursively
        Transform[] allChildren = skeletonRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child == skeletonRoot.transform) continue;

            bool hasMesh = child.GetComponent<MeshFilter>() != null
                        || child.GetComponent<SkinnedMeshRenderer>() != null;

            if (!hasMesh) continue;

            // ?? Add Mesh Collider ????????????????????????????
            if (addMeshCollider)
            {
                MeshCollider existing = child.GetComponent<MeshCollider>();
                if (existing == null || overwriteExisting)
                {
                    if (existing != null && overwriteExisting)
                        DestroyImmediate(existing);

                    MeshCollider col = child.gameObject.AddComponent<MeshCollider>();

                    // For SkinnedMeshRenderer, bake the mesh
                    SkinnedMeshRenderer smr = child.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null)
                    {
                        Mesh bakedMesh = new Mesh();
                        smr.BakeMesh(bakedMesh);
                        col.sharedMesh = bakedMesh;
                    }
                }
            }

            // ?? Add StructureInfo ????????????????????????????
            if (addStructureInfo)
            {
                StructureInfo existing = child.GetComponent<StructureInfo>();
                if (existing == null || overwriteExisting)
                {
                    if (existing != null && overwriteExisting)
                        DestroyImmediate(existing);

                    StructureInfo info = child.gameObject.AddComponent<StructureInfo>();

                    // Use the GameObject name as the structure name
                    // Clean up common suffixes like .s .t .g from FBX names
                    info.structureName = CleanBoneName(child.name);
                    info.description = GetDefaultDescription(info.structureName);
                }
            }

            processedCount++;
        }

        Debug.Log($"[SkeletonAutoSetup] Processed {processedCount} bones on '{skeletonRoot.name}'");
        EditorUtility.DisplayDialog("Done!",
            $"Auto setup complete!\n{processedCount} bones processed.", "OK");
    }

    // Cleans FBX suffixes like ".s", ".t", ".g" from bone names
    string CleanBoneName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return rawName;

        // Remove common FBX export suffixes
        string[] suffixes = { ".s", ".t", ".g", "_s", "_t", "_g" };
        foreach (var suffix in suffixes)
            if (rawName.EndsWith(suffix))
                return rawName.Substring(0, rawName.Length - suffix.Length);

        return rawName;
    }

    // Provides a default description based on the bone name
    string GetDefaultDescription(string boneName)
    {
        // You can expand this dictionary with real anatomical descriptions
        var descriptions = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Thoracic skeleton",    "The thoracic skeleton forms the ribcage, protecting the heart and lungs." },
            { "Vertebral column",     "The vertebral column (spine) supports the body and protects the spinal cord." },
            { "Axial skeleton",       "The axial skeleton forms the central axis of the body." },
            { "Bones of upper limb",  "The upper limb bones include the humerus, radius, ulna, and hand bones." },
            { "Bones of lower limb",  "The lower limb bones include the femur, tibia, fibula, and foot bones." },
            { "Bony pelvis",          "The bony pelvis supports the spine and connects to the lower limbs." },
            { "Auditory ossicles",    "The auditory ossicles are the smallest bones in the body, located in the ear." },
            { "Teeth",                "Teeth are used for biting and chewing food." },
        };

        foreach (var kvp in descriptions)
            if (boneName.Contains(kvp.Key))
                return kvp.Value;

        // Generic fallback
        return $"{boneName} is an anatomical structure of the skeletal system. " +
               "Tap to learn more about its function and location.";
    }

}
