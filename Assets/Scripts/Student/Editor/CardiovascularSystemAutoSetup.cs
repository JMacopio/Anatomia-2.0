#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// CARDIOVASCULAR SYSTEM AUTO SETUP — Anatomia 3D
// Adds MeshCollider + StructureInfo to every cardiovascular structure.
// Menu: Anatomia 3D → Auto Setup Cardiovascular System

public class CardiovascularSystemAutoSetup : EditorWindow
{
    private GameObject cardiovascularRoot;
    private bool overwriteExisting = false;
    private bool skipHelperObjects = true;
    private bool convexColliders = false;

    private int processedCount = 0;
    private int skippedCount = 0;
    private Vector2 scrollPos;

    // ── Objects to skip ───────────────────────────────────────────────
    private static readonly string[] skipNames =
    {
        "cross section", "crosssection", "cross_section",
        "helper", "target", "ik_", "_ik",
        "pole", "nub", "end", "tip",
        "camera", "light", "lamp", "armature"
    };

    // ── Cardiovascular name → (display name, description, category) ──
    private static readonly Dictionary<string,
        (string name, string desc, string cat)> cardioData =
        new Dictionary<string, (string, string, string)>
    {
        // ── HEART ─────────────────────────────────────────────────────
        { "heart",              ("Heart",                  "The heart is a muscular organ that pumps blood throughout the body via the circulatory system.", "Heart") },
        { "cardiac",            ("Cardiac Muscle",         "Cardiac muscle is involuntary striated muscle tissue found only in the heart.", "Heart") },
        { "myocardium",         ("Myocardium",             "The muscular middle layer of the heart wall that contracts to pump blood.", "Heart") },
        { "pericardium",        ("Pericardium",            "The protective double-walled sac surrounding the heart and the roots of the great vessels.", "Heart") },
        { "epicardium",         ("Epicardium",             "The outer layer of the heart wall, also the inner layer of the pericardium.", "Heart") },
        { "endocardium",        ("Endocardium",            "The innermost layer of tissue lining the chambers of the heart.", "Heart") },

        // ── HEART CHAMBERS ────────────────────────────────────────────
        { "left atrium",        ("Left Atrium",            "The upper-left chamber that receives oxygenated blood from the pulmonary veins.", "Heart Chambers") },
        { "right atrium",       ("Right Atrium",           "The upper-right chamber that receives deoxygenated blood from the body via the vena cava.", "Heart Chambers") },
        { "left ventricle",     ("Left Ventricle",         "The lower-left chamber that pumps oxygenated blood to the body through the aorta.", "Heart Chambers") },
        { "right ventricle",    ("Right Ventricle",        "The lower-right chamber that pumps deoxygenated blood to the lungs through the pulmonary artery.", "Heart Chambers") },
        { "atrium",             ("Atrium",                 "The upper chambers of the heart that receive blood from veins.", "Heart Chambers") },
        { "ventricle",          ("Ventricle",              "The lower chambers of the heart that pump blood out to the body or lungs.", "Heart Chambers") },
        { "septum",             ("Septum",                 "The wall dividing the left and right sides of the heart.", "Heart Chambers") },
        { "interventricular",   ("Interventricular Septum","The wall separating the left and right ventricles of the heart.", "Heart Chambers") },
        { "interatrial",        ("Interatrial Septum",     "The wall separating the left and right atria of the heart.", "Heart Chambers") },
        { "apex",               ("Heart Apex",             "The pointed lower tip of the heart, formed by the left ventricle.", "Heart") },
        { "base",               ("Heart Base",             "The broad upper part of the heart from which the great vessels emerge.", "Heart") },

        // ── HEART VALVES ──────────────────────────────────────────────
        { "mitral",             ("Mitral Valve",           "The bicuspid valve between the left atrium and left ventricle. Prevents backflow.", "Heart Valves") },
        { "bicuspid",           ("Bicuspid (Mitral) Valve","Controls blood flow between the left atrium and left ventricle.", "Heart Valves") },
        { "tricuspid",          ("Tricuspid Valve",        "The valve between the right atrium and right ventricle with three cusps.", "Heart Valves") },
        { "aortic valve",       ("Aortic Valve",           "Controls blood flow from the left ventricle into the aorta.", "Heart Valves") },
        { "pulmonary valve",    ("Pulmonary Valve",        "Controls blood flow from the right ventricle into the pulmonary artery.", "Heart Valves") },
        { "semilunar",          ("Semilunar Valve",        "Half-moon shaped valves (aortic and pulmonary) that prevent backflow into the ventricles.", "Heart Valves") },
        { "chordae",            ("Chordae Tendineae",      "Tendon-like cords connecting the papillary muscles to the tricuspid and mitral valves.", "Heart Valves") },
        { "papillary",          ("Papillary Muscles",      "Muscles that control the mitral and tricuspid valves via the chordae tendineae.", "Heart Valves") },
        { "cusp",               ("Valve Cusp",             "A flap of the heart valve that opens and closes to regulate blood flow.", "Heart Valves") },

        // ── GREAT VESSELS ─────────────────────────────────────────────
        { "aorta",              ("Aorta",                  "The largest artery in the body, carrying oxygenated blood from the left ventricle to the body.", "Great Vessels") },
        { "ascending aorta",    ("Ascending Aorta",        "The first section of the aorta that rises from the left ventricle.", "Great Vessels") },
        { "descending aorta",   ("Descending Aorta",       "The section of the aorta that travels downward through the chest and abdomen.", "Great Vessels") },
        { "aortic arch",        ("Aortic Arch",            "The curved section of the aorta that connects the ascending and descending aorta.", "Great Vessels") },
        { "arch",               ("Aortic Arch",            "The curved section of the aorta giving rise to the major head and arm vessels.", "Great Vessels") },
        { "pulmonary artery",   ("Pulmonary Artery",       "Carries deoxygenated blood from the right ventricle to the lungs.", "Great Vessels") },
        { "pulmonary trunk",    ("Pulmonary Trunk",        "The main vessel that divides into left and right pulmonary arteries.", "Great Vessels") },
        { "pulmonary vein",     ("Pulmonary Vein",         "Carries oxygenated blood from the lungs back to the left atrium.", "Great Vessels") },
        { "superior vena cava", ("Superior Vena Cava",     "The large vein returning deoxygenated blood from the upper body to the right atrium.", "Great Vessels") },
        { "inferior vena cava", ("Inferior Vena Cava",     "The large vein returning deoxygenated blood from the lower body to the right atrium.", "Great Vessels") },
        { "vena cava",          ("Vena Cava",              "The large veins returning deoxygenated blood from the body to the heart.", "Great Vessels") },
        { "svc",                ("Superior Vena Cava",     "Returns deoxygenated blood from the head, neck and arms to the right atrium.", "Great Vessels") },
        { "ivc",                ("Inferior Vena Cava",     "Returns deoxygenated blood from the lower body to the right atrium.", "Great Vessels") },

        // ── CORONARY VESSELS ──────────────────────────────────────────
        { "coronary",           ("Coronary Artery",        "Arteries that supply oxygenated blood to the heart muscle itself.", "Coronary Vessels") },
        { "left coronary",      ("Left Coronary Artery",   "Supplies blood to the left side of the heart including the left ventricle.", "Coronary Vessels") },
        { "right coronary",     ("Right Coronary Artery",  "Supplies blood to the right side of the heart and the SA/AV nodes.", "Coronary Vessels") },
        { "lad",                ("Left Anterior Descending","Supplies blood to the front of the heart; most commonly blocked in heart attacks.", "Coronary Vessels") },
        { "left anterior",      ("Left Anterior Descending","Supplies blood to the front of the left ventricle.", "Coronary Vessels") },
        { "circumflex",         ("Circumflex Artery",      "Supplies blood to the left atrium and the back of the left ventricle.", "Coronary Vessels") },
        { "marginal",           ("Marginal Artery",        "A branch of the right coronary artery supplying the right ventricle.", "Coronary Vessels") },
        { "posterior descending",("Posterior Descending",  "Supplies blood to the bottom and back of the heart.", "Coronary Vessels") },
        { "coronary sinus",     ("Coronary Sinus",         "A collection of veins that drain deoxygenated blood from the heart muscle.", "Coronary Vessels") },

        // ── MAJOR ARTERIES ────────────────────────────────────────────
        { "brachiocephalic",    ("Brachiocephalic Artery", "The first major branch of the aortic arch supplying the right arm and right side of the head.", "Major Arteries") },
        { "common carotid",     ("Common Carotid Artery",  "Supplies blood to the head and neck; divides into internal and external carotid.", "Major Arteries") },
        { "carotid",            ("Carotid Artery",         "Major artery supplying the brain, neck, and face with oxygenated blood.", "Major Arteries") },
        { "subclavian",         ("Subclavian Artery",      "Supplies blood to the arms, neck, thoracic wall, and brain.", "Major Arteries") },
        { "brachial",           ("Brachial Artery",        "Main artery of the upper arm; used for blood pressure measurement.", "Major Arteries") },
        { "radial",             ("Radial Artery",          "Artery of the forearm used to measure pulse at the wrist.", "Major Arteries") },
        { "ulnar",              ("Ulnar Artery",           "Artery of the forearm on the little finger side.", "Major Arteries") },
        { "celiac",             ("Celiac Trunk",           "The first major branch of the abdominal aorta supplying digestive organs.", "Major Arteries") },
        { "mesenteric",         ("Mesenteric Artery",      "Supplies blood to the intestines.", "Major Arteries") },
        { "renal",              ("Renal Artery",           "Supplies oxygenated blood to the kidneys.", "Major Arteries") },
        { "iliac",              ("Iliac Artery",           "Supplies blood to the pelvis and lower limbs.", "Major Arteries") },
        { "femoral",            ("Femoral Artery",         "The main artery of the thigh supplying the lower limb.", "Major Arteries") },
        { "popliteal",          ("Popliteal Artery",       "Continuation of the femoral artery behind the knee.", "Major Arteries") },
        { "tibial",             ("Tibial Artery",          "Supplies blood to the lower leg and foot.", "Major Arteries") },
        { "vertebral",          ("Vertebral Artery",       "Supplies blood to the brainstem, cerebellum, and spinal cord.", "Major Arteries") },

        // ── MAJOR VEINS ───────────────────────────────────────────────
        { "jugular",            ("Jugular Vein",           "Returns deoxygenated blood from the brain, face, and neck to the heart.", "Major Veins") },
        { "subclavian vein",    ("Subclavian Vein",        "Returns blood from the arm to the superior vena cava.", "Major Veins") },
        { "portal",             ("Portal Vein",            "Carries nutrient-rich blood from the digestive organs to the liver.", "Major Veins") },
        { "hepatic",            ("Hepatic Vein",           "Returns blood from the liver to the inferior vena cava.", "Major Veins") },
        { "renal vein",         ("Renal Vein",             "Returns deoxygenated blood from the kidneys to the inferior vena cava.", "Major Veins") },
        { "femoral vein",       ("Femoral Vein",           "Returns blood from the lower limb to the iliac vein.", "Major Veins") },
        { "saphenous",          ("Saphenous Vein",         "The longest vein in the body, running along the leg.", "Major Veins") },

        // ── CONDUCTION SYSTEM ─────────────────────────────────────────
        { "sinoatrial",         ("Sinoatrial Node (SA)",   "The natural pacemaker of the heart located in the right atrium.", "Conduction System") },
        { "sa node",            ("SA Node",                "The sinoatrial node — the heart's natural pacemaker.", "Conduction System") },
        { "atrioventricular",   ("Atrioventricular Node (AV)","Receives the impulse from the SA node and transmits it to the ventricles.", "Conduction System") },
        { "av node",            ("AV Node",                "Delays electrical impulse between atria and ventricles.", "Conduction System") },
        { "bundle of his",      ("Bundle of His",          "Conducts electrical impulses from the AV node to the ventricles.", "Conduction System") },
        { "purkinje",           ("Purkinje Fibers",        "Specialized fibers that rapidly conduct impulses through the ventricles.", "Conduction System") },
        { "bundle branch",      ("Bundle Branch",          "Left and right branches carrying electrical signals down the septum.", "Conduction System") },

        // ── BLOOD VESSELS (GENERIC) ───────────────────────────────────
        { "artery",             ("Artery",                 "Blood vessels that carry oxygenated blood away from the heart to the body.", "Blood Vessels") },
        { "vein",               ("Vein",                   "Blood vessels that carry deoxygenated blood back to the heart.", "Blood Vessels") },
        { "capillary",          ("Capillary",              "The smallest blood vessels where exchange of oxygen, nutrients, and waste occurs.", "Blood Vessels") },
        { "vessel",             ("Blood Vessel",           "Tubular structures that carry blood throughout the body.", "Blood Vessels") },
        { "tunica",             ("Tunica",                 "The layered wall structure of blood vessels including intima, media, and adventitia.", "Blood Vessels") },

        // ── LYMPHATIC ─────────────────────────────────────────────────
        { "lymph",              ("Lymphatic Vessel",       "Vessels that carry lymph fluid and help remove waste from tissues.", "Lymphatic") },
        { "thoracic duct",      ("Thoracic Duct",          "The largest lymphatic vessel, draining most of the body's lymph.", "Lymphatic") },
    };

    [MenuItem("Anatomia 3D/Auto Setup Cardiovascular System")]
    public static void ShowWindow() =>
        GetWindow<CardiovascularSystemAutoSetup>("Cardiovascular System Setup");

    [MenuItem("GameObject/Anatomia 3D/Auto Setup Cardiovascular System", false, 12)]
    static void SetupFromHierarchy()
    {
        var win = GetWindow<CardiovascularSystemAutoSetup>("Cardiovascular System Setup");
        win.cardiovascularRoot = Selection.activeGameObject;
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Anatomia 3D — Cardiovascular System Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        cardiovascularRoot = (GameObject)EditorGUILayout.ObjectField(
            "Cardiovascular Root", cardiovascularRoot, typeof(GameObject), true);

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        skipHelperObjects = EditorGUILayout.Toggle("Skip Helper Objects", skipHelperObjects);
        convexColliders = EditorGUILayout.Toggle("Convex Colliders", convexColliders);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "This will, for every mesh under the root:\n" +
            "1. Add a MeshCollider matching the mesh shape\n" +
            "2. Put it on the SkeletonModel layer\n" +
            "3. Add StructureInfo with name + description\n\n" +
            "Helper objects (e.g., 'helper', 'ik_') are skipped.\n" +
            "Includes 60+ cardiovascular terms.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.enabled = cardiovascularRoot != null;
        if (GUILayout.Button("▶  Run Cardiovascular Setup", GUILayout.Height(38)))
            RunSetup();
        if (GUILayout.Button("✕  Remove All Colliders", GUILayout.Height(24)))
            RemoveAll();
        GUI.enabled = true;

        if (processedCount > 0 || skippedCount > 0)
            EditorGUILayout.HelpBox(
                $"✅ {processedCount} structures set up, {skippedCount} skipped.",
                MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    // ────────────────────────────────────────────────────────
    void RunSetup()
    {
        if (!cardiovascularRoot) return;

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

        Undo.RegisterFullObjectHierarchyUndo(cardiovascularRoot, "Auto Setup Cardiovascular");

        // Collect all MeshFilter and SkinnedMeshRenderer components
        var meshFilters = cardiovascularRoot.GetComponentsInChildren<MeshFilter>(true);
        var skinnedRenderers = cardiovascularRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        // Combine for progress
        int total = meshFilters.Length + skinnedRenderers.Length;
        int processed = 0;

        // Process MeshFilter objects
        foreach (var mf in meshFilters)
        {
            var go = mf.gameObject;
            if (processed % 25 == 0)
                EditorUtility.DisplayProgressBar("Cardiovascular Setup", go.name, (float)processed / total);

            if (ShouldSkip(go.name) || mf.sharedMesh == null)
            {
                skippedCount++;
                processed++;
                continue;
            }

            ProcessSingle(go, mf.sharedMesh, skeletonLayer);
            processedCount++;
            processed++;
        }

        // Process SkinnedMeshRenderer objects
        foreach (var smr in skinnedRenderers)
        {
            var go = smr.gameObject;
            if (processed % 25 == 0)
                EditorUtility.DisplayProgressBar("Cardiovascular Setup", go.name, (float)processed / total);

            if (ShouldSkip(go.name) || smr.sharedMesh == null)
            {
                skippedCount++;
                processed++;
                continue;
            }

            ProcessSingle(go, smr.sharedMesh, skeletonLayer);
            processedCount++;
            processed++;
        }

        EditorUtility.ClearProgressBar();
        EditorSceneManagerMarkDirty();

        Debug.Log($"[CardiovascularSetup] {processedCount} structures set up, " +
                  $"{skippedCount} skipped, {total} total.");
        EditorUtility.DisplayDialog("Done!",
            $"{processedCount} structures set up.\n{skippedCount} skipped.", "OK");
    }

    void ProcessSingle(GameObject go, Mesh mesh, int layer)
    {
        // ── Collider ─────────────────────────────────────
        var existing = go.GetComponent<Collider>();
        if (existing != null)
        {
            if (!overwriteExisting) return;
            Undo.DestroyObjectImmediate(existing);
        }

        var mc = Undo.AddComponent<MeshCollider>(go);
        mc.sharedMesh = mesh;
        mc.convex = convexColliders;

        // ── Layer ────────────────────────────────────────
        go.layer = layer;

        // ── StructureInfo ────────────────────────────────
        var info = go.GetComponent<StructureInfo>();
        if (info == null)
            info = Undo.AddComponent<StructureInfo>(go);

        if (overwriteExisting || string.IsNullOrEmpty(info.structureName))
        {
            string clean = CleanName(go.name);
            var match = FindCardioData(clean);

            // Use the mesh name as the title, but dictionary provides description/category
            info.structureName = FormatName(clean);
            info.description = match.HasValue
                ? match.Value.desc
                : $"{FormatName(clean)} is part of the cardiovascular system.";
            info.category = match.HasValue ? match.Value.cat : "Cardiovascular System";
            EditorUtility.SetDirty(info);
        }
    }

    void RemoveAll()
    {
        if (!cardiovascularRoot) return;
        Undo.RegisterFullObjectHierarchyUndo(cardiovascularRoot, "Remove Cardiovascular Colliders");

        int removed = 0;
        foreach (var col in cardiovascularRoot.GetComponentsInChildren<Collider>(true))
        {
            Undo.DestroyObjectImmediate(col);
            removed++;
        }

        processedCount = 0;
        skippedCount = 0;
        Debug.Log($"[CardiovascularSetup] Removed {removed} colliders.");
    }

    void EditorSceneManagerMarkDirty()
    {
        if (cardiovascularRoot != null && !EditorApplication.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(cardiovascularRoot.scene);
    }

    // ── Skip rules ───────────────────────────────────────────
    bool ShouldSkip(string rawName)
    {
        string lower = rawName.ToLower();

        if (skipHelperObjects)
            foreach (var s in skipNames)
                if (lower.Contains(s)) return true;

        return false;
    }

    // ── Match cardio name against dictionary ──────────────────
    (string name, string desc, string cat)? FindCardioData(string name)
    {
        string lower = name.ToLower();
        // Exact match first
        if (cardioData.TryGetValue(lower, out var exact)) return exact;
        // Partial match (check each key)
        foreach (var kvp in cardioData)
            if (lower.Contains(kvp.Key))
                return kvp.Value;
        return null;
    }

    // ── Clean FBX suffixes (.001 .s .t .r .l etc.) ──────────
    string CleanName(string raw)
    {
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
        // Capitalise first letter only
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
#endif