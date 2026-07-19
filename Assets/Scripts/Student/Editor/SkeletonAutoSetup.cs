using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkeletonAutoSetup : EditorWindow
{
    private GameObject skeletonRoot;
    private bool overwriteExisting = false;
    private int processedCount = 0;
    private Vector2 scrollPos;

    // ── Bone name → description dictionary ──────────────────
    // Add more entries as needed
    private static readonly Dictionary<string, (string name, string desc, string cat)> boneData =
        new Dictionary<string, (string, string, string)>
    {
        // ── SKULL ────────────────────────────────────────────
        { "skull",           ("Skull",            "The skull is the bony structure that forms the head. It protects the brain and supports facial structures.", "Skull") },
        { "mandible",        ("Mandible",          "The mandible, or lower jaw, is the only movable bone in the skull. It holds the lower teeth.", "Skull") },
        { "frontal",         ("Frontal Bone",      "Forms the forehead and upper part of the eye sockets.", "Skull") },
        { "temporal",        ("Temporal Bone",     "Located on the sides of the skull, housing the middle and inner ear structures.", "Skull") },
        { "parietal",        ("Parietal Bone",     "Two parietal bones form the top and sides of the skull.", "Skull") },
        { "occipital",       ("Occipital Bone",    "Forms the back and base of the skull, containing the foramen magnum.", "Skull") },
        { "zygomatic",       ("Zygomatic Bone",    "Forms the cheekbone and part of the eye socket.", "Skull") },
        { "maxilla",         ("Maxilla",           "Forms the upper jaw, holds upper teeth, and forms part of the hard palate.", "Skull") },
        { "nasal",           ("Nasal Bone",        "Two small bones that form the bridge of the nose.", "Skull") },
 
        // ── VERTEBRAL COLUMN ─────────────────────────────────
        { "vertebra",        ("Vertebra",          "Vertebrae are the individual bones making up the spinal column, protecting the spinal cord.", "Vertebral Column") },
        { "cervical",        ("Cervical Vertebra", "The 7 cervical vertebrae form the neck region of the spine.", "Vertebral Column") },
        { "thoracic",        ("Thoracic Vertebra", "The 12 thoracic vertebrae articulate with the ribs.", "Vertebral Column") },
        { "lumbar",          ("Lumbar Vertebra",   "The 5 lumbar vertebrae are the largest and bear most of the body's weight.", "Vertebral Column") },
        { "sacrum",          ("Sacrum",            "A triangular bone formed by 5 fused vertebrae, connecting the spine to the pelvis.", "Vertebral Column") },
        { "coccyx",          ("Coccyx",            "The tailbone, formed by 3-5 fused vertebrae at the base of the spine.", "Vertebral Column") },
 
        // ── THORAX ───────────────────────────────────────────
        { "rib",             ("Rib",               "Ribs are curved bones forming the rib cage that protects the heart and lungs.", "Thorax") },
        { "sternum",         ("Sternum",           "The breastbone connects the ribs via cartilage and protects the heart.", "Thorax") },
        { "clavicle",        ("Clavicle",          "The collarbone connects the shoulder blade to the sternum.", "Thorax") },
 
        // ── UPPER LIMB ────────────────────────────────────────
        { "scapula",         ("Scapula",           "The shoulder blade connects the upper arm to the clavicle.", "Upper Limb") },
        { "humerus",         ("Humerus",           "The upper arm bone, connecting the shoulder to the elbow.", "Upper Limb") },
        { "radius",          ("Radius",            "One of two forearm bones, on the thumb side.", "Upper Limb") },
        { "ulna",            ("Ulna",              "One of two forearm bones, on the little finger side.", "Upper Limb") },
        { "carpals",         ("Carpals",           "8 small bones forming the wrist joint.", "Upper Limb") },
        { "metacarpal",      ("Metacarpal",        "5 bones forming the palm of the hand.", "Upper Limb") },
        { "phalanx",         ("Phalanx",           "The finger and toe bones. Each finger has 3 phalanges except the thumb which has 2.", "Upper Limb") },
 
        // ── PELVIS ───────────────────────────────────────────
        { "pelvis",          ("Pelvis",            "The basin-shaped structure supporting the spine and connecting to the lower limbs.", "Pelvis") },
        { "ilium",           ("Ilium",             "The largest part of the hip bone, forming the upper part of the pelvis.", "Pelvis") },
        { "ischium",         ("Ischium",           "The lower and back part of the hip bone.", "Pelvis") },
        { "pubis",           ("Pubis",             "The front part of the hip bone.", "Pelvis") },
 
        // ── LOWER LIMB ────────────────────────────────────────
        { "femur",           ("Femur",             "The thigh bone — the longest and strongest bone in the human body.", "Lower Limb") },
        { "patella",         ("Patella",           "The kneecap, a sesamoid bone that protects the knee joint.", "Lower Limb") },
        { "tibia",           ("Tibia",             "The shin bone — the larger of the two lower leg bones.", "Lower Limb") },
        { "fibula",          ("Fibula",            "The smaller bone running alongside the tibia in the lower leg.", "Lower Limb") },
        { "tarsals",         ("Tarsals",           "7 bones forming the ankle and back of the foot.", "Lower Limb") },
        { "calcaneus",       ("Calcaneus",         "The heel bone — the largest tarsal bone.", "Lower Limb") },
        { "metatarsal",      ("Metatarsal",        "5 bones forming the middle part of the foot.", "Lower Limb") },
    };

    [MenuItem("Anatomia 3D/Auto Setup Skeleton")]
    public static void ShowWindow()
    {
        GetWindow<SkeletonAutoSetup>("Skeleton Auto Setup");
    }

    [MenuItem("GameObject/Anatomia 3D/Auto Setup Skeleton", false, 10)]
    static void SetupFromHierarchy()
    {
        var win = GetWindow<SkeletonAutoSetup>("Skeleton Auto Setup");
        win.skeletonRoot = Selection.activeGameObject;
    }

    void OnGUI()
    {
        GUILayout.Label("Anatomia 3D — Skeleton Auto Setup",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        skeletonRoot = (GameObject)EditorGUILayout.ObjectField(
            "Skeleton Root", skeletonRoot, typeof(GameObject), true);

        overwriteExisting = EditorGUILayout.Toggle(
            "Overwrite Existing", overwriteExisting);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "This will:\n" +
            "1. Add MeshCollider to every bone\n" +
            "2. Add StructureInfo with name + description\n" +
            "3. Auto-match bone names from the dictionary",
            MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.enabled = skeletonRoot != null;
        if (GUILayout.Button("▶  Run Auto Setup", GUILayout.Height(40)))
            RunSetup();
        GUI.enabled = true;

        if (processedCount > 0)
            EditorGUILayout.HelpBox(
                $"✅ Done! {processedCount} bones processed.",
                MessageType.None);
    }

    void RunSetup()
    {
        if (!skeletonRoot) return;
        processedCount = 0;
        Undo.RegisterFullObjectHierarchyUndo(skeletonRoot, "Auto Setup Skeleton");

        var allTransforms = skeletonRoot
            .GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            if (t == skeletonRoot.transform) continue;

            // Check for mesh
            var mf = t.GetComponent<MeshFilter>();
            var smr = t.GetComponent<SkinnedMeshRenderer>();
            if (mf == null && smr == null) continue;

            // ── Add MeshCollider ────────────────────────────
            var col = t.GetComponent<MeshCollider>();
            if (col == null || overwriteExisting)
            {
                if (col && overwriteExisting) DestroyImmediate(col);
                col = t.gameObject.AddComponent<MeshCollider>();

                if (smr != null)
                {
                    Mesh baked = new Mesh();
                    smr.BakeMesh(baked);
                    col.sharedMesh = baked;
                    col.convex = false;
                }
                else if (mf != null)
                {
                    col.sharedMesh = mf.sharedMesh;
                }
            }

            // ── Add StructureInfo ────────────────────────────
            var info = t.GetComponent<StructureInfo>();
            if (info == null || overwriteExisting)
            {
                if (info && overwriteExisting) DestroyImmediate(info);
                info = t.gameObject.AddComponent<StructureInfo>();

                // Try to match bone name to dictionary
                string cleanName = CleanName(t.name);
                var match = FindBoneData(cleanName);

                info.structureName = match.HasValue
                    ? match.Value.name : FormatName(cleanName);
                info.description = match.HasValue
                    ? match.Value.desc
                    : $"{FormatName(cleanName)} is part of the skeletal system.";
                info.category = match.HasValue
                    ? match.Value.cat : "Skeletal System";
            }

            processedCount++;
        }

        Debug.Log($"[SkeletonAutoSetup] Done — {processedCount} bones processed.");
        EditorUtility.DisplayDialog("Done!",
            $"{processedCount} bones set up successfully!", "OK");
    }

    // ── Match bone name against dictionary ──────────────────
    (string name, string desc, string cat)? FindBoneData(string boneName)
    {
        string lower = boneName.ToLower();
        foreach (var kvp in boneData)
            if (lower.Contains(kvp.Key))
                return kvp.Value;
        return null;
    }

    // ── Clean FBX suffixes (.s .t .g _001 etc.) ────────────
    string CleanName(string raw)
    {
        string[] suffixes = { ".s", ".t", ".g", "_s", "_t", "_g" };
        foreach (var s in suffixes)
            if (raw.EndsWith(s))
                return raw.Substring(0, raw.Length - s.Length);
        // Remove trailing numbers like _001
        return System.Text.RegularExpressions.Regex
            .Replace(raw, @"[_\.\s]\d+$", "").Trim();
    }

    // ── Format raw name to readable ─────────────────────────
    string FormatName(string raw)
    {
        // "left_femur" → "Left Femur"
        return System.Globalization.CultureInfo.CurrentCulture
            .TextInfo.ToTitleCase(raw.Replace("_", " ").Replace(".", " "));
    }
}