#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MUSCULAR SYSTEM AUTO SETUP — Anatomia 3D
/// Adds SphereCollider + StructureInfo to every muscle mesh.
/// </summary>
public class MuscularSystemAutoSetup : EditorWindow
{
    private GameObject muscularRoot;
    private bool overwriteExisting = false;
    private bool skipHelperObjects = true;
    private int processedCount = 0;
    private int skippedCount = 0;

    // ── Muscle name → (display name, description, category) ──
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

    // ── Objects to skip ───────────────────────────────────────
    private static readonly string[] skipNames =
    {
        "cross section", "crosssection", "cross_section",
        "helper", "target", "ik_", "_ik",
        "pole", "nub", "end", "tip",
        "camera", "light", "lamp"
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
        GUILayout.Label("Anatomia 3D — Muscular System Auto Setup",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        muscularRoot = (GameObject)EditorGUILayout.ObjectField(
            "Muscular System Root", muscularRoot,
            typeof(GameObject), true);

        overwriteExisting = EditorGUILayout.Toggle(
            "Overwrite Existing", overwriteExisting);
        skipHelperObjects = EditorGUILayout.Toggle(
            "Skip Helper Objects", skipHelperObjects);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Adds SphereCollider + StructureInfo to every muscle mesh.\n" +
            "Uses a muscle name dictionary with 60+ muscles.\n" +
            "Sets SkeletonModel layer for raycasting.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        GUI.enabled = muscularRoot != null;
        if (GUILayout.Button("▶  Run Muscular Setup", GUILayout.Height(40)))
            RunSetup();
        GUI.enabled = true;

        if (processedCount > 0)
            EditorGUILayout.HelpBox(
                $"✅ Done! {processedCount} muscles processed, " +
                $"{skippedCount} skipped.", MessageType.None);
    }

    void RunSetup()
    {
        if (!muscularRoot) return;
        processedCount = 0;
        skippedCount = 0;

        // Verify SkeletonModel layer exists
        int muscleLayer = LayerMask.NameToLayer("SkeletonModel");
        if (muscleLayer == -1)
        {
            EditorUtility.DisplayDialog("Layer Missing",
                "Please create a Layer named 'SkeletonModel' first!\n" +
                "Edit → Project Settings → Tags and Layers", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            muscularRoot, "Auto Setup Muscular System");

        Transform[] all =
            muscularRoot.GetComponentsInChildren<Transform>(true);

        // Set root layer first
        muscularRoot.layer = muscleLayer;

        foreach (Transform t in all)
        {
            if (t == muscularRoot.transform) continue;

            // Set layer on every child
            t.gameObject.layer = muscleLayer;

            // Skip helper objects
            if (skipHelperObjects && ShouldSkip(t.name))
            {
                skippedCount++;
                continue;
            }

            // Only add collider to objects with mesh (muscles have geometry)
            bool hasMesh = t.GetComponent<MeshFilter>() != null
                        || t.GetComponent<SkinnedMeshRenderer>() != null;

            if (!hasMesh)
            {
                // Still add StructureInfo to named bones without mesh
                string cleanN = CleanName(t.name);
                var matchN = FindMuscleData(cleanN);
                if (!matchN.HasValue) continue; // skip unnamed transforms
            }

            // ── Collider ──────────────────────────────────────
            var existingCol = t.GetComponent<Collider>();
            if (existingCol != null)
            {
                if (overwriteExisting) DestroyImmediate(existingCol);
                else goto AddStructureInfo;
            }

            if (hasMesh)
            {
                float radius = CalculateRadius(t);
                var sphere = t.gameObject.AddComponent<SphereCollider>();
                sphere.radius = radius;
                sphere.center = Vector3.zero;
            }

        AddStructureInfo:
            // ── StructureInfo ─────────────────────────────────
            var existingInfo = t.GetComponent<StructureInfo>();
            if (existingInfo == null || overwriteExisting)
            {
                if (existingInfo && overwriteExisting)
                    DestroyImmediate(existingInfo);

                var info = t.gameObject.AddComponent<StructureInfo>();
                string clean = CleanName(t.name);
                var match = FindMuscleData(clean);

                info.structureName = match.HasValue
                    ? match.Value.name : FormatName(clean);
                info.description = match.HasValue
                    ? match.Value.desc
                    : $"{FormatName(clean)} is part of the muscular system.";
                info.category = match.HasValue
                    ? match.Value.cat : "Muscular System";
            }

            processedCount++;
        }

        Debug.Log($"[MuscularSetup] Done! " +
                  $"{processedCount} muscles, {skippedCount} skipped.");
        EditorUtility.DisplayDialog("Done!",
            $"✅ Muscular System setup complete!\n\n" +
            $"• {processedCount} muscles processed\n" +
            $"• {skippedCount} helper objects skipped\n" +
            $"• SkeletonModel layer assigned\n" +
            $"• SphereCollider + StructureInfo added", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────
    float CalculateRadius(Transform t)
    {
        var mf = t.GetComponent<MeshFilter>();
        var smr = t.GetComponent<SkinnedMeshRenderer>();
        float radius = 0.05f;

        if (mf?.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            radius = Mathf.Max(b.extents.magnitude * 0.4f, 0.03f);
        }
        else if (smr?.sharedMesh != null)
        {
            Bounds b = smr.sharedMesh.bounds;
            radius = Mathf.Max(b.extents.magnitude * 0.2f, 0.03f);
        }
        else if (t.childCount > 0)
        {
            float dist = Vector3.Distance(
                t.position, t.GetChild(0).position);
            radius = Mathf.Clamp(dist * 0.5f, 0.03f, 0.5f);
        }

        return Mathf.Clamp(radius, 0.03f, 0.8f);
    }

    bool ShouldSkip(string name)
    {
        string lower = name.ToLower();
        foreach (var s in skipNames)
            if (lower.Contains(s)) return true;
        return false;
    }

    (string name, string desc, string cat)? FindMuscleData(string name)
    {
        string lower = name.ToLower();
        // Try exact match first
        foreach (var kvp in muscleData)
            if (lower == kvp.Key) return kvp.Value;
        // Then partial match
        foreach (var kvp in muscleData)
            if (lower.Contains(kvp.Key)) return kvp.Value;
        return null;
    }

    string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        string[] suffixes = {
            ".001", ".002", ".003", ".004", ".005",
            "_L", "_R", ".L", ".R",
            "_left", "_right", ".left", ".right",
            ".s", ".t", ".g"
        };
        string result = raw;
        foreach (var s in suffixes)
            if (result.EndsWith(s, System.StringComparison.OrdinalIgnoreCase))
                result = result.Substring(0, result.Length - s.Length);
        return result.Replace("_", " ").Trim();
    }

    string FormatName(string raw)
    {
        return System.Globalization.CultureInfo.CurrentCulture
            .TextInfo.ToTitleCase(raw.ToLower());
    }
}
#endif