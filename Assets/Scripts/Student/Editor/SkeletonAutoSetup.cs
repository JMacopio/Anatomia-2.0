#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SkeletonAutoSetup : EditorWindow
{
    private GameObject skeletonRoot;
    private bool overwriteExisting = false;
    private bool skipCrossSections = true;
    private bool skipMuscleMarkers = true;
    private bool convexColliders = false;

    private int processedCount = 0;
    private int skippedCount = 0;
    private Vector2 scrollPos;

    // Names containing any of these are never given a collider or the
    // SkeletonModel layer. The cross-section quads are the reason bone
    // taps were being swallowed.
    private static readonly string[] skipNames =
    {
        "cross section", "crosssection", "cross_section"
    };

    // ── Bone name → description dictionary ──────────────────
    // The FBX already carries precise anatomical names, so these are used
    // for the DESCRIPTION and CATEGORY only; structureName comes from the mesh.
    private static readonly Dictionary<string, (string name, string desc, string cat)> boneData =
        new Dictionary<string, (string, string, string)>
    {
        // ── SKULL ────────────────────────────────────────────
        { "mandible",        ("Mandible",          "The mandible, or lower jaw, is the only movable bone in the skull. It holds the lower teeth.", "Skull") },
        { "frontal",         ("Frontal Bone",      "Forms the forehead and upper part of the eye sockets.", "Skull") },
        { "temporal",        ("Temporal Bone",     "Located on the sides of the skull, housing the middle and inner ear structures.", "Skull") },
        { "parietal",        ("Parietal Bone",     "Two parietal bones form the top and sides of the skull.", "Skull") },
        { "occipital",       ("Occipital Bone",    "Forms the back and base of the skull, containing the foramen magnum.", "Skull") },
        { "zygomatic",       ("Zygomatic Bone",    "Forms the cheekbone and part of the eye socket.", "Skull") },
        { "maxilla",         ("Maxilla",           "Forms the upper jaw, holds upper teeth, and forms part of the hard palate.", "Skull") },
        { "nasal",           ("Nasal Bone",        "Two small bones that form the bridge of the nose.", "Skull") },
        { "sphenoid",        ("Sphenoid Bone",     "A butterfly-shaped bone at the base of the skull.", "Skull") },
        { "ethmoid",         ("Ethmoid Bone",      "A light, spongy bone between the eye sockets forming part of the nasal cavity.", "Skull") },
        { "vomer",           ("Vomer",             "A thin bone forming the lower part of the nasal septum.", "Skull") },
        { "lacrimal",        ("Lacrimal Bone",     "The smallest bone of the face, forming part of the eye socket.", "Skull") },
        { "palatine",        ("Palatine Bone",     "Forms the back of the hard palate and part of the nasal cavity.", "Skull") },
        { "hyoid",           ("Hyoid Bone",        "A U-shaped bone in the neck that anchors the tongue muscles. It articulates with no other bone.", "Skull") },
        { "skull",           ("Skull",             "The bony structure that forms the head, protecting the brain and supporting facial structures.", "Skull") },
 
        // ── VERTEBRAL COLUMN ─────────────────────────────────
        { "atlas",           ("Atlas (C1)",        "The first cervical vertebra, supporting the skull and allowing the nodding motion.", "Vertebral Column") },
        { "axis",            ("Axis (C2)",         "The second cervical vertebra, whose dens allows the head to rotate.", "Vertebral Column") },
        { "cervical",        ("Cervical Vertebra", "The 7 cervical vertebrae form the neck region of the spine.", "Vertebral Column") },
        { "thoracic",        ("Thoracic Vertebra", "The 12 thoracic vertebrae articulate with the ribs.", "Vertebral Column") },
        { "lumbar",          ("Lumbar Vertebra",   "The 5 lumbar vertebrae are the largest and bear most of the body's weight.", "Vertebral Column") },
        { "sacrum",          ("Sacrum",            "A triangular bone formed by 5 fused vertebrae, connecting the spine to the pelvis.", "Vertebral Column") },
        { "coccyx",          ("Coccyx",            "The tailbone, formed by 3-5 fused vertebrae at the base of the spine.", "Vertebral Column") },
        { "vertebra",        ("Vertebra",          "Vertebrae are the individual bones making up the spinal column, protecting the spinal cord.", "Vertebral Column") },
 
        // ── THORAX ───────────────────────────────────────────
        { "rib",             ("Rib",               "Ribs are curved bones forming the rib cage that protects the heart and lungs.", "Thorax") },
        { "sternum",         ("Sternum",           "The breastbone connects the ribs via cartilage and protects the heart.", "Thorax") },
        { "manubrium",       ("Manubrium",         "The upper section of the sternum, articulating with the clavicles and first ribs.", "Thorax") },
        { "xiphoid",         ("Xiphoid Process",   "The small cartilaginous tip at the lower end of the sternum.", "Thorax") },
        { "costal cartilage",("Costal Cartilage",  "Cartilage connecting the ribs to the sternum, giving the chest wall flexibility.", "Thorax") },
        { "clavicle",        ("Clavicle",          "The collarbone connects the shoulder blade to the sternum.", "Thorax") },
 
        // ── UPPER LIMB ────────────────────────────────────────
        { "scapula",         ("Scapula",           "The shoulder blade connects the upper arm to the clavicle.", "Upper Limb") },
        { "humerus",         ("Humerus",           "The upper arm bone, connecting the shoulder to the elbow.", "Upper Limb") },
        { "radius",          ("Radius",            "One of two forearm bones, on the thumb side.", "Upper Limb") },
        { "ulna",            ("Ulna",              "One of two forearm bones, on the little finger side.", "Upper Limb") },
        { "scaphoid",        ("Scaphoid Bone",     "A carpal bone at the base of the thumb, the most commonly fractured wrist bone.", "Upper Limb") },
        { "lunate",          ("Lunate Bone",       "A crescent-shaped carpal bone in the proximal row of the wrist.", "Upper Limb") },
        { "triquetral",      ("Triquetral Bone",   "A pyramid-shaped carpal bone on the little finger side of the wrist.", "Upper Limb") },
        { "pisiform",        ("Pisiform Bone",     "A small pea-shaped sesamoid bone of the wrist.", "Upper Limb") },
        { "trapezium",       ("Trapezium",         "A carpal bone that articulates with the thumb's metacarpal.", "Upper Limb") },
        { "trapezoid",       ("Trapezoid Bone",    "The smallest bone in the distal row of the carpals.", "Upper Limb") },
        { "capitate",        ("Capitate Bone",     "The largest of the carpal bones, at the centre of the wrist.", "Upper Limb") },
        { "hamate",          ("Hamate Bone",       "A wedge-shaped carpal bone with a distinctive hook.", "Upper Limb") },
        { "carpal",          ("Carpals",           "8 small bones forming the wrist joint.", "Upper Limb") },
        { "metacarpal",      ("Metacarpal",        "5 bones forming the palm of the hand.", "Upper Limb") },
 
        // ── PELVIS ───────────────────────────────────────────
        { "ilium",           ("Ilium",             "The largest part of the hip bone, forming the upper part of the pelvis.", "Pelvis") },
        { "ischium",         ("Ischium",           "The lower and back part of the hip bone.", "Pelvis") },
        { "pubis",           ("Pubis",             "The front part of the hip bone.", "Pelvis") },
        { "acetabul",        ("Acetabulum",        "The cup-shaped socket of the hip bone that receives the head of the femur.", "Pelvis") },
        { "pelvis",          ("Pelvis",            "The basin-shaped structure supporting the spine and connecting to the lower limbs.", "Pelvis") },
        { "hip bone",        ("Hip Bone",          "Formed by the fused ilium, ischium and pubis.", "Pelvis") },
 
        // ── LOWER LIMB ────────────────────────────────────────
        { "femur",           ("Femur",             "The thigh bone — the longest and strongest bone in the human body.", "Lower Limb") },
        { "patella",         ("Patella",           "The kneecap, a sesamoid bone that protects the knee joint.", "Lower Limb") },
        { "tibia",           ("Tibia",             "The shin bone — the larger of the two lower leg bones.", "Lower Limb") },
        { "fibula",          ("Fibula",            "The smaller bone running alongside the tibia in the lower leg.", "Lower Limb") },
        { "calcaneus",       ("Calcaneus",         "The heel bone — the largest tarsal bone.", "Lower Limb") },
        { "talus",           ("Talus",             "The ankle bone that transmits body weight from the tibia to the foot.", "Lower Limb") },
        { "navicular",       ("Navicular Bone",    "A boat-shaped tarsal bone on the inner side of the foot.", "Lower Limb") },
        { "cuboid",          ("Cuboid Bone",       "A cube-shaped tarsal bone on the outer side of the foot.", "Lower Limb") },
        { "cuneiform",       ("Cuneiform Bone",    "Three wedge-shaped tarsal bones forming part of the arch of the foot.", "Lower Limb") },
        { "tarsal",          ("Tarsals",           "7 bones forming the ankle and back of the foot.", "Lower Limb") },
        { "metatarsal",      ("Metatarsal",        "5 bones forming the middle part of the foot.", "Lower Limb") },
 
        // ── GENERIC (checked last) ───────────────────────────
        { "phalanx",         ("Phalanx",           "The finger and toe bones. Each finger has 3 phalanges except the thumb, which has 2.", "Upper Limb") },
    };

    [MenuItem("Anatomia 3D/Auto Setup Skeleton")]
    public static void ShowWindow() => GetWindow<SkeletonAutoSetup>("Skeleton Auto Setup");

    [MenuItem("GameObject/Anatomia 3D/Auto Setup Skeleton", false, 10)]
    static void SetupFromHierarchy()
    {
        var win = GetWindow<SkeletonAutoSetup>("Skeleton Auto Setup");
        win.skeletonRoot = Selection.activeGameObject;
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Anatomia 3D — Skeleton Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        skeletonRoot = (GameObject)EditorGUILayout.ObjectField(
            "Skeleton Root", skeletonRoot, typeof(GameObject), true);

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        skipCrossSections = EditorGUILayout.Toggle("Skip Cross Sections", skipCrossSections);
        skipMuscleMarkers = EditorGUILayout.Toggle("Skip Muscle Markers", skipMuscleMarkers);
        convexColliders = EditorGUILayout.Toggle("Convex Colliders", convexColliders);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "This will, for every mesh under the root:\n" +
            "1. Add a MeshCollider matching the actual bone shape\n" +
            "2. Put it on the SkeletonModel layer\n" +
            "3. Add StructureInfo with name + description\n\n" +
            "Cross-section quads are skipped — their colliders sit in " +
            "front of the whole skeleton and swallow every tap.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.enabled = skeletonRoot != null;
        if (GUILayout.Button("▶  Run Auto Setup", GUILayout.Height(38)))
            RunSetup();
        if (GUILayout.Button("✕  Remove All Colliders", GUILayout.Height(24)))
            RemoveAll();
        GUI.enabled = true;

        if (processedCount > 0 || skippedCount > 0)
            EditorGUILayout.HelpBox(
                $"✅ {processedCount} meshes set up, {skippedCount} skipped.",
                MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    // ────────────────────────────────────────────────────────
    void RunSetup()
    {
        if (!skeletonRoot) return;

        processedCount = 0;
        skippedCount = 0;

        int skeletonLayer = LayerMask.NameToLayer("SkeletonModel");
        if (skeletonLayer == -1)
        {
            EditorUtility.DisplayDialog("Missing layer",
                "Create a layer named 'SkeletonModel' in Project Settings > " +
                "Tags and Layers first, then run this again.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(skeletonRoot, "Auto Setup Skeleton");

        var filters = skeletonRoot.GetComponentsInChildren<MeshFilter>(true);

        for (int i = 0; i < filters.Length; i++)
        {
            var mf = filters[i];
            var go = mf.gameObject;

            if (i % 25 == 0)
                EditorUtility.DisplayProgressBar("Skeleton Auto Setup",
                    go.name, (float)i / filters.Length);

            if (ShouldSkip(go.name) || mf.sharedMesh == null)
            {
                skippedCount++;
                continue;
            }

            // ── Collider ─────────────────────────────────────
            var existing = go.GetComponent<Collider>();
            if (existing != null)
            {
                if (!overwriteExisting) { skippedCount++; continue; }
                Undo.DestroyObjectImmediate(existing);
            }

            var mc = Undo.AddComponent<MeshCollider>(go);
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = convexColliders;

            // ── Layer ────────────────────────────────────────
            go.layer = skeletonLayer;

            // ── StructureInfo ────────────────────────────────
            var info = go.GetComponent<StructureInfo>();
            if (info == null)
                info = Undo.AddComponent<StructureInfo>(go);

            if (overwriteExisting || string.IsNullOrEmpty(info.structureName))
            {
                string clean = CleanName(go.name);
                var match = FindBoneData(clean);

                // The FBX names are already precise, so keep them as the title
                // and let the dictionary supply description + category.
                info.structureName = FormatName(clean);
                info.description = match.HasValue
                    ? match.Value.desc
                    : $"{FormatName(clean)} is part of the skeletal system.";
                info.category = match.HasValue ? match.Value.cat : "Skeletal System";
                EditorUtility.SetDirty(info);
            }

            processedCount++;
        }

        EditorUtility.ClearProgressBar();
        EditorSceneManagerMarkDirty();

        Debug.Log($"[SkeletonAutoSetup] {processedCount} meshes set up, " +
                  $"{skippedCount} skipped, {filters.Length} total.");
        EditorUtility.DisplayDialog("Done!",
            $"{processedCount} meshes set up.\n{skippedCount} skipped.", "OK");
    }

    void RemoveAll()
    {
        if (!skeletonRoot) return;
        Undo.RegisterFullObjectHierarchyUndo(skeletonRoot, "Remove Skeleton Colliders");

        int removed = 0;
        foreach (var col in skeletonRoot.GetComponentsInChildren<Collider>(true))
        {
            Undo.DestroyObjectImmediate(col);
            removed++;
        }

        processedCount = 0;
        skippedCount = 0;
        Debug.Log($"[SkeletonAutoSetup] Removed {removed} colliders.");
    }

    void EditorSceneManagerMarkDirty()
    {
        if (skeletonRoot != null && !EditorApplication.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(skeletonRoot.scene);
    }

    // ── Skip rules ───────────────────────────────────────────
    bool ShouldSkip(string rawName)
    {
        string lower = rawName.ToLower();

        if (skipCrossSections)
            foreach (var s in skipNames)
                if (lower.Contains(s)) return true;

        // Muscle origin / insertion markers exported as ".o", ".e",
        // ".o1", ".e1r", ".el", ".er" etc. They are attachment patches
        // painted on the bone surface, not selectable structures, and they
        // sit slightly proud of the bone so they win every raycast.
        if (skipMuscleMarkers)
        {
            string stem = System.Text.RegularExpressions.Regex
                .Replace(lower, @"\.\d+$", "");   // drop ".001"
            if (System.Text.RegularExpressions.Regex
                    .IsMatch(stem, @"\.(o|e)\d*[lr]?$"))
                return true;
        }

        return false;
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

    // ── Clean FBX suffixes (.001 .s .t .r .l etc.) ──────────
    string CleanName(string raw)
    {
        // "Calcaneus.r.001" → "Calcaneus (right)"
        string s = System.Text.RegularExpressions.Regex
            .Replace(raw, @"\.\d+$", "");            // trailing ".001"

        string side = "";
        if (System.Text.RegularExpressions.Regex.IsMatch(s, @"\.r$")) side = " (right)";
        else if (System.Text.RegularExpressions.Regex.IsMatch(s, @"\.l$")) side = " (left)";

        s = System.Text.RegularExpressions.Regex
            .Replace(s, @"\.(s|t|g|r|l)$", "");      // geometry/side suffixes

        return (s + side).Trim();
    }

    // ── Format raw name to readable ─────────────────────────
    string FormatName(string raw)
    {
        string s = raw.Replace("_", " ").Trim();
        if (s.Length == 0) return "Structure";
        // Only capitalise the first letter — anatomical names are already
        // correctly cased and ToTitleCase would mangle "Atlas (C1)".
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
#endif