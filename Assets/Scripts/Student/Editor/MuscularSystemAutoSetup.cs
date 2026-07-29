#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MUSCULAR SYSTEM AUTO SETUP — Anatomia 3D
/// Adds MeshCollider + StructureInfo to every muscle mesh.
/// Mirrors SkeletonAutoSetup for consistency.
/// </summary>
public class MuscularSystemAutoSetup : EditorWindow
{
    private GameObject muscularRoot;
    private bool overwriteExisting = false;
    private bool skipCrossSections = true;
    private bool skipMuscleMarkers = true;
    private bool convexColliders = false;

    private int processedCount = 0;
    private int skippedCount = 0;
    private Vector2 scrollPos;

    // ── Skip names (cross‑section quads that swallow raycasts) ──
    private static readonly string[] skipNames =
    {
        "cross section", "crosssection", "cross_section"
    };

    // ── Muscle name → (display name, description, category) ──
    // (your existing muscleData dictionary – keep it unchanged)
    private static readonly Dictionary<string,
        (string name, string desc, string cat)> muscleData =
        new Dictionary<string, (string, string, string)>
        {
                // ── HEAD & NECK ───────────────────────────────────────
            { "frontalis",        ("Frontalis",         "Raises the eyebrows and wrinkles the forehead.", "Head & Neck") },
            { "temporalis",       ("Temporalis",         "Closes the jaw and is involved in chewing.", "Head & Neck") },
            { "masseter",         ("Masseter",           "The primary muscle used for chewing (mastication).", "Head & Neck") },
            { "orbicularis oculi",("Orbicularis Oculi",  "Closes the eyelids and is involved in blinking.", "Head & Neck") },
            { "orbicularis oris", ("Orbicularis Oris",   "Closes and protrudes the lips.", "Head & Neck") },
            { "zygomaticus",      ("Zygomaticus",        "Pulls the corners of the mouth upward when smiling.", "Head & Neck") },
            { "buccinator",       ("Buccinator",         "Compresses the cheeks during chewing and blowing.", "Head & Neck") },
            { "sternocleidomastoid",("Sternocleidomastoid","Rotates and flexes the neck; tilts the head.", "Head & Neck") },
            { "scm",              ("Sternocleidomastoid","Rotates and flexes the neck; tilts the head.", "Head & Neck") },
            { "platysma",         ("Platysma",           "Pulls the lower lip down; tenses the skin of the neck.", "Head & Neck") },
            { "trapezius",        ("Trapezius",          "Moves, rotates, and stabilizes the shoulder blade.", "Head & Neck") },
            { "splenius",         ("Splenius",           "Rotates and extends the head and neck.", "Head & Neck") },
            { "scalene",          ("Scalene",            "Flexes and rotates the cervical spine; assists breathing.", "Head & Neck") },

            // ── CHEST ─────────────────────────────────────────────
            { "pectoralis major", ("Pectoralis Major",   "The large chest muscle that flexes, adducts, and rotates the arm.", "Chest") },
            { "pectoralis minor", ("Pectoralis Minor",   "Stabilizes the scapula by drawing it toward the chest wall.", "Chest") },
            { "pectoralis",       ("Pectoralis",         "The chest muscle group responsible for arm movement.", "Chest") },
            { "pec",              ("Pectoralis",         "The chest muscle group responsible for arm movement.", "Chest") },
            { "serratus anterior",("Serratus Anterior",  "Pulls the scapula forward and around the rib cage.", "Chest") },
            { "serratus",         ("Serratus Anterior",  "Pulls the scapula forward and around the rib cage.", "Chest") },
            { "intercostal",      ("Intercostals",       "Muscles between the ribs that assist in breathing.", "Chest") },
            { "subclavius",       ("Subclavius",         "Stabilizes and depresses the clavicle.", "Chest") },

            // ── SHOULDER ──────────────────────────────────────────
            { "deltoid",          ("Deltoid",            "The shoulder muscle responsible for arm abduction and rotation.", "Shoulder") },
            { "rotator cuff",     ("Rotator Cuff",       "Group of muscles stabilizing and rotating the shoulder joint.", "Shoulder") },
            { "supraspinatus",    ("Supraspinatus",      "Initiates arm abduction and stabilizes the shoulder joint.", "Shoulder") },
            { "infraspinatus",    ("Infraspinatus",      "Externally rotates the arm and stabilizes the shoulder.", "Shoulder") },
            { "teres minor",      ("Teres Minor",        "Externally rotates the arm; part of the rotator cuff.", "Shoulder") },
            { "teres major",      ("Teres Major",        "Adducts and medially rotates the arm.", "Shoulder") },
            { "subscapularis",    ("Subscapularis",      "Internally rotates the arm; largest rotator cuff muscle.", "Shoulder") },

            // ── UPPER ARM ─────────────────────────────────────────
            { "biceps brachii",   ("Biceps Brachii",     "Flexes the elbow and supinates the forearm.", "Upper Arm") },
            { "biceps",           ("Biceps Brachii",     "Flexes the elbow and supinates the forearm.", "Upper Arm") },
            { "brachialis",       ("Brachialis",         "Primary flexor of the elbow joint.", "Upper Arm") },
            { "triceps brachii",  ("Triceps Brachii",    "Extends the elbow; the only muscle on the back of the upper arm.", "Upper Arm") },
            { "triceps",          ("Triceps Brachii",    "Extends the elbow; the only muscle on the back of the upper arm.", "Upper Arm") },
            { "coracobrachialis",  ("Coracobrachialis",  "Flexes and adducts the arm at the shoulder.", "Upper Arm") },

            // ── FOREARM ───────────────────────────────────────────
            { "brachioradialis",  ("Brachioradialis",    "Flexes the elbow, especially during rapid movement.", "Forearm") },
            { "pronator",         ("Pronator",           "Rotates the forearm to face downward (pronation).", "Forearm") },
            { "supinator",        ("Supinator",          "Rotates the forearm to face upward (supination).", "Forearm") },
            { "flexor carpi",     ("Flexor Carpi",       "Flexes and abducts or adducts the wrist.", "Forearm") },
            { "extensor carpi",   ("Extensor Carpi",     "Extends and abducts or adducts the wrist.", "Forearm") },
            { "flexor digitorum", ("Flexor Digitorum",   "Flexes the fingers and wrist.", "Forearm") },
            { "extensor digitorum",("Extensor Digitorum","Extends the fingers and wrist.", "Forearm") },
            { "palmaris longus",  ("Palmaris Longus",    "Flexes the wrist; absent in some people.", "Forearm") },
            { "anconeus",         ("Anconeus",           "Assists in elbow extension and stabilization.", "Forearm") },

            // ── ABDOMEN ───────────────────────────────────────────
            { "rectus abdominis", ("Rectus Abdominis",   "The 'six-pack' muscle; flexes the vertebral column.", "Abdomen") },
            { "rectus",           ("Rectus Abdominis",   "The 'six-pack' muscle; flexes the vertebral column.", "Abdomen") },
            { "external oblique", ("External Oblique",   "Rotates and laterally flexes the trunk.", "Abdomen") },
            { "internal oblique", ("Internal Oblique",   "Rotates and laterally flexes the trunk; compresses abdomen.", "Abdomen") },
            { "oblique",          ("Oblique",            "Rotates and laterally flexes the trunk.", "Abdomen") },
            { "transversus abdominis",("Transversus Abdominis","Compresses the abdomen; the deepest abdominal muscle.", "Abdomen") },
            { "transverse",       ("Transversus Abdominis","Compresses the abdomen; the deepest abdominal muscle.", "Abdomen") },
            { "diaphragm",        ("Diaphragm",          "The primary breathing muscle; separates chest and abdomen.", "Abdomen") },

            // ── BACK ──────────────────────────────────────────────
            { "latissimus dorsi", ("Latissimus Dorsi",   "The broad back muscle; adducts, extends, and rotates the arm.", "Back") },
            { "latissimus",       ("Latissimus Dorsi",   "The broad back muscle; adducts, extends, and rotates the arm.", "Back") },
            { "lats",             ("Latissimus Dorsi",   "The broad back muscle; adducts, extends, and rotates the arm.", "Back") },
            { "rhomboid",         ("Rhomboid",           "Retracts and elevates the scapula.", "Back") },
            { "levator scapulae", ("Levator Scapulae",   "Elevates the scapula and tilts the head.", "Back") },
            { "erector spinae",   ("Erector Spinae",     "Extends and laterally flexes the vertebral column.", "Back") },
            { "erector",          ("Erector Spinae",     "Extends and laterally flexes the vertebral column.", "Back") },
            { "multifidus",       ("Multifidus",         "Stabilizes and extends the vertebral column.", "Back") },
            { "quadratus lumborum",("Quadratus Lumborum","Laterally flexes the trunk and stabilizes the lumbar spine.", "Back") },

            // ── GLUTEAL ───────────────────────────────────────────
            { "gluteus maximus",  ("Gluteus Maximus",    "The largest muscle in the body; extends and rotates the hip.", "Gluteal") },
            { "gluteus medius",   ("Gluteus Medius",     "Abducts and medially rotates the thigh.", "Gluteal") },
            { "gluteus minimus",  ("Gluteus Minimus",    "Abducts and medially rotates the thigh.", "Gluteal") },
            { "gluteus",          ("Gluteus",            "The gluteal muscles extend, abduct, and rotate the hip.", "Gluteal") },
            { "glute",            ("Gluteus",            "The gluteal muscles extend, abduct, and rotate the hip.", "Gluteal") },
            { "piriformis",       ("Piriformis",         "Externally rotates and abducts the hip.", "Gluteal") },
            { "tensor fasciae latae",("Tensor Fasciae Latae","Abducts and medially rotates the thigh.", "Gluteal") },
            { "iliotibial",       ("Iliotibial Band",    "Stabilizes the knee and assists in hip abduction.", "Gluteal") },

            // ── THIGH ─────────────────────────────────────────────
            { "quadriceps",       ("Quadriceps",         "Group of four muscles that extend the knee.", "Thigh") },
            { "rectus femoris",   ("Rectus Femoris",     "Extends the knee and flexes the hip.", "Thigh") },
            { "vastus lateralis", ("Vastus Lateralis",   "Extends the knee; largest quadriceps muscle.", "Thigh") },
            { "vastus medialis",  ("Vastus Medialis",    "Extends the knee; stabilizes the patella.", "Thigh") },
            { "vastus intermedius",("Vastus Intermedius","Extends the knee; deep quadriceps muscle.", "Thigh") },
            { "vastus",           ("Vastus",             "Part of the quadriceps group that extends the knee.", "Thigh") },
            { "hamstring",        ("Hamstrings",         "Group of muscles that flex the knee and extend the hip.", "Thigh") },
            { "biceps femoris",   ("Biceps Femoris",     "Flexes the knee and extends the hip.", "Thigh") },
            { "semitendinosus",   ("Semitendinosus",     "Flexes the knee and extends the hip.", "Thigh") },
            { "semimembranosus",  ("Semimembranosus",    "Flexes the knee and extends the hip.", "Thigh") },
            { "sartorius",        ("Sartorius",          "The longest muscle; flexes, abducts, and rotates the hip.", "Thigh") },
            { "adductor",         ("Adductor",           "Adducts (brings together) the thigh.", "Thigh") },
            { "gracilis",         ("Gracilis",           "Adducts the thigh and flexes the knee.", "Thigh") },
            { "iliopsoas",        ("Iliopsoas",          "Flexes the hip; the primary hip flexor muscle.", "Thigh") },
            { "iliacus",          ("Iliacus",            "Flexes the hip jointly with the psoas.", "Thigh") },
            { "psoas",            ("Psoas Major",        "Flexes the hip; the deepest hip flexor.", "Thigh") },
            { "pectineus",        ("Pectineus",          "Adducts and flexes the thigh.", "Thigh") },

            // ── LOWER LEG ─────────────────────────────────────────
            { "gastrocnemius",    ("Gastrocnemius",      "The large calf muscle; plantarflexes the foot.", "Lower Leg") },
            { "soleus",           ("Soleus",             "Plantarflexes the foot; important for standing.", "Lower Leg") },
            { "calf",             ("Calf",               "The calf muscles plantarflex the foot.", "Lower Leg") },
            { "tibialis anterior",("Tibialis Anterior",  "Dorsiflexes and inverts the foot.", "Lower Leg") },
            { "tibialis",         ("Tibialis",           "Controls foot movement and ankle stability.", "Lower Leg") },
            { "peroneus",         ("Peroneus",           "Everts the foot and assists in plantarflexion.", "Lower Leg") },
            { "fibularis",        ("Fibularis",          "Everts the foot and assists in plantarflexion.", "Lower Leg") },
            { "extensor hallucis",("Extensor Hallucis",  "Extends the big toe and assists dorsiflexion.", "Lower Leg") },
            { "flexor hallucis",  ("Flexor Hallucis",    "Flexes the big toe; assists in plantarflexion.", "Lower Leg") },
            { "popliteus",        ("Popliteus",          "Unlocks the knee to allow flexion.", "Lower Leg") },
            { "achilles",         ("Achilles Tendon",    "The largest tendon; connects the calf muscles to the heel.", "Lower Leg") },

            // ── FOOT ──────────────────────────────────────────────
            { "plantar",          ("Plantar Muscles",    "Intrinsic muscles of the foot supporting the arch.", "Foot") },
            { "abductor hallucis",("Abductor Hallucis",  "Abducts and flexes the big toe.", "Foot") },
            { "flexor digitorum brevis",("Flexor Digitorum Brevis","Flexes the middle phalanges of toes 2-5.", "Foot") },

        };

    // ── Muscle origin/insertion markers ───────────────────────
    // These are painted patches on bones that represent muscle attachments.
    // They are small, sit proud of the bone surface, and win every raycast.
    // We skip them so taps fall through to the underlying bone/muscle.
    private static readonly string[] muscleMarkerPatterns =
    {
        ".o", ".e", ".o1", ".e1", ".el", ".er", ".ol", ".or",
        ".o_", ".e_", ".o1_", ".e1_"
    };

    [MenuItem("Anatomia 3D/Auto Setup Muscular System")]
    public static void ShowWindow() =>
        GetWindow<MuscularSystemAutoSetup>("Muscular System Setup");

    [MenuItem("GameObject/Anatomia 3D/Auto Setup Muscular System", false, 11)]
    static void SetupFromHierarchy()
    {
        var win = GetWindow<MuscularSystemAutoSetup>("Muscular System Setup");
        win.muscularRoot = Selection.activeGameObject;
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Anatomia 3D — Muscular System Auto Setup",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        muscularRoot = (GameObject)EditorGUILayout.ObjectField(
            "Muscular System Root", muscularRoot,
            typeof(GameObject), true);

        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        skipCrossSections = EditorGUILayout.Toggle("Skip Cross Sections", skipCrossSections);
        skipMuscleMarkers = EditorGUILayout.Toggle("Skip Muscle Markers", skipMuscleMarkers);
        convexColliders = EditorGUILayout.Toggle("Convex Colliders", convexColliders);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Adds MeshCollider + StructureInfo to every muscle mesh.\n" +
            "Uses a muscle name dictionary with 60+ muscles.\n" +
            "Sets SkeletonModel layer for raycasting.\n\n" +
            "Cross‑section quads and muscle attachment markers are skipped.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.enabled = muscularRoot != null;
        if (GUILayout.Button("▶  Run Muscular Setup", GUILayout.Height(38)))
            RunSetup();
        if (GUILayout.Button("✕  Remove All Colliders", GUILayout.Height(24)))
            RemoveAll();
        GUI.enabled = true;

        if (processedCount > 0 || skippedCount > 0)
            EditorGUILayout.HelpBox(
                $"✅ {processedCount} muscles set up, {skippedCount} skipped.",
                MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    void RunSetup()
    {
        if (!muscularRoot) return;

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

        Undo.RegisterFullObjectHierarchyUndo(muscularRoot, "Auto Setup Muscular System");

        // Collect all mesh‑bearing objects (MeshFilter + SkinnedMeshRenderer)
        var meshFilters = muscularRoot.GetComponentsInChildren<MeshFilter>(true);
        var skinnedRenderers = muscularRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        List<Component> allMeshComponents = new List<Component>();
        allMeshComponents.AddRange(meshFilters);
        allMeshComponents.AddRange(skinnedRenderers);

        int total = allMeshComponents.Count;

        for (int i = 0; i < total; i++)
        {
            Component comp = allMeshComponents[i];
            GameObject go = comp.gameObject;

            if (i % 25 == 0)
                EditorUtility.DisplayProgressBar("Muscular Auto Setup",
                    go.name, (float)i / total);

            if (ShouldSkip(go.name))
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
            if (comp is MeshFilter mf && mf.sharedMesh != null)
                mc.sharedMesh = mf.sharedMesh;
            else if (comp is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                mc.sharedMesh = smr.sharedMesh;
            else
            {
                // No mesh – skip (shouldn't happen)
                skippedCount++;
                continue;
            }

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
                var match = FindMuscleData(clean);

                // Use the clean name as the display title,
                // and the dictionary supplies description + category.
                info.structureName = FormatName(clean);
                info.description = match.HasValue
                    ? match.Value.desc
                    : $"{FormatName(clean)} is part of the muscular system.";
                info.category = match.HasValue
                    ? match.Value.cat : "Muscular System";
                EditorUtility.SetDirty(info);
            }

            processedCount++;
        }

        EditorUtility.ClearProgressBar();
        MarkSceneDirty();

        Debug.Log($"[MuscularSetup] {processedCount} muscles set up, " +
                  $"{skippedCount} skipped, {total} total.");
        EditorUtility.DisplayDialog("Done!",
            $"{processedCount} muscles set up.\n{skippedCount} skipped.", "OK");
    }

    void RemoveAll()
    {
        if (!muscularRoot) return;
        Undo.RegisterFullObjectHierarchyUndo(muscularRoot, "Remove Muscular Colliders");

        int removed = 0;
        foreach (var col in muscularRoot.GetComponentsInChildren<Collider>(true))
        {
            Undo.DestroyObjectImmediate(col);
            removed++;
        }

        processedCount = 0;
        skippedCount = 0;
        Debug.Log($"[MuscularSetup] Removed {removed} colliders.");
    }

    void MarkSceneDirty()
    {
        if (muscularRoot != null && !EditorApplication.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(muscularRoot.scene);
    }

    // ── Skip rules (same as SkeletonAutoSetup) ───────────────
    bool ShouldSkip(string rawName)
    {
        string lower = rawName.ToLower();

        // Cross‑section quads
        if (skipCrossSections)
            foreach (var s in skipNames)
                if (lower.Contains(s)) return true;

        // Muscle origin/insertion markers
        if (skipMuscleMarkers)
        {
            string stem = System.Text.RegularExpressions.Regex
                .Replace(lower, @"\.\d+$", "");   // drop ".001"
            foreach (var pat in muscleMarkerPatterns)
                if (stem.Contains(pat))
                    return true;
        }

        return false;
    }

    // ── Match muscle name against dictionary ─────────────────
    (string name, string desc, string cat)? FindMuscleData(string muscleName)
    {
        string lower = muscleName.ToLower();
        foreach (var kvp in muscleData)
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
        if (s.Length == 0) return "Muscle";
        // Capitalise first letter only – preserve acronyms/casing.
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
#endif