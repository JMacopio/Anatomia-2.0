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

    // ── CARDIOVASCULAR name → (display name, description, category) ──
    private static readonly Dictionary<string,
    (string name, string desc, string cat)> cardioData =
    new Dictionary<string, (string, string, string)>
    {

    // ── CARDIOVASCULAR SYSTEM ─────────────────────────────────────
    { "cardiovascular system",
        ("Cardiovascular System",
         "The cardiovascular system consists of the heart, blood vessels, and blood. It includes pulmonary circulation, which carries blood between the heart and lungs, and systemic circulation, which carries blood between the heart and the rest of the body.",
         "Cardiovascular System") },

    { "circulatory system",
        ("Circulatory System",
         "The circulatory system includes the heart, blood vessels, and blood. It transports oxygen, nutrients, and other substances throughout the body and removes metabolic waste.",
         "Cardiovascular System") },

    // ── HEART ─────────────────────────────────────────────────────
    { "heart",
        ("Heart",
         "The heart is a muscular organ that pumps blood through the blood vessels of the circulatory system. It is located between the lungs in the middle compartment of the chest and contains four chambers in humans.",
         "Heart") },

    { "cardiac muscle",
        ("Cardiac Muscle",
         "Cardiac muscle is the specialized muscle tissue of the heart responsible for producing the contractions that pump blood through the circulatory system.",
         "Heart") },

    { "endocardium",
        ("Endocardium",
         "The endocardium is the innermost layer of tissue lining the chambers of the heart. It also covers the heart valves and lies directly beneath the myocardium.",
         "Heart") },

    { "pericardium",
        ("Pericardium",
         "The pericardium is a double-walled sac containing the heart and the roots of the great vessels. It protects the heart, reduces friction during movement, and contains pericardial fluid.",
         "Heart") },

    { "epicardium",
        ("Epicardium",
         "The epicardium is the outer layer of the heart wall and forms the visceral layer of the serous pericardium.",
         "Heart") },

    // ── HEART SURFACES & LANDMARKS ─────────────────────────────────
    { "base of heart",
        ("Base of Heart",
         "The base of the heart is the broad posterior portion of the heart. It is formed mainly by the left atrium with contributions from the right atrium.",
         "Heart") },

    { "apex of heart",
        ("Apex of Heart",
         "The apex is the pointed inferior end of the heart and is formed mainly by the left ventricle.",
         "Heart") },

    { "anterior surface of heart",
        ("Anterior Surface of Heart",
         "The anterior surface is the front-facing surface of the heart, formed mainly by the right ventricle with contributions from the right atrium and left ventricle.",
         "Heart") },

    { "inferior surface of heart",
        ("Inferior Surface of Heart",
         "The inferior surface of the heart rests mainly on the diaphragm and is formed primarily by the ventricles.",
         "Heart") },

    { "coronary sulcus",
        ("Coronary Sulcus",
         "The coronary sulcus is a groove on the surface of the heart that separates the atria from the ventricles and contains important coronary vessels.",
         "Heart") },

    { "anterior interventricular sulcus",
        ("Anterior Interventricular Sulcus",
         "A groove on the anterior surface of the heart marking the separation between the right and left ventricles. It contains the anterior interventricular artery and associated veins.",
         "Heart") },

    { "sulcus terminalis of heart",
        ("Sulcus Terminalis of Heart",
         "A groove on the right atrium marking the external boundary between the smooth posterior portion and the rough anterior portion of the atrium.",
         "Heart") },

    // ── HEART CHAMBERS ────────────────────────────────────────────
    { "left atrium",
        ("Left Atrium",
         "The left atrium is the upper chamber of the heart that receives oxygenated blood from the pulmonary veins and passes it to the left ventricle.",
         "Heart Chambers") },

    { "right atrium",
        ("Right Atrium",
         "The right atrium is the upper chamber of the heart that receives venous blood from the superior and inferior venae cavae and the coronary sinus.",
         "Heart Chambers") },

    { "left ventricle",
        ("Left Ventricle",
         "The left ventricle is the lower chamber that receives blood from the left atrium and pumps it into the aorta for systemic circulation.",
         "Heart Chambers") },

    { "right ventricle",
        ("Right Ventricle",
         "The right ventricle is the lower chamber that receives blood from the right atrium and pumps it through the pulmonary trunk toward the lungs.",
         "Heart Chambers") },

    { "atrium",
        ("Atrium",
         "An atrium is an upper chamber of the heart through which blood enters the ventricles. The human heart has a left and a right atrium.",
         "Heart Chambers") },

    { "ventricle",
        ("Ventricle",
         "A ventricle is a lower chamber of the heart that receives blood from an atrium and pumps it toward the lungs or the rest of the body.",
         "Heart Chambers") },

    // ── HEART SEPTA ───────────────────────────────────────────────
    { "interatrial septum",
        ("Interatrial Septum",
         "The interatrial septum is the wall of tissue separating the right and left atria of the heart.",
         "Heart Chambers") },

    { "interventricular septum",
        ("Interventricular Septum",
         "The interventricular septum is the strong wall separating the right and left ventricles of the heart. Much of it is muscular.",
         "Heart Chambers") },

    { "atrioventricular septum",
        ("Atrioventricular Septum",
         "The atrioventricular septum is a portion of the tissue separating the atrial and ventricular regions of the heart.",
         "Heart Chambers") },

    // ── HEART VALVES ──────────────────────────────────────────────
    { "aortic valve",
        ("Aortic Valve",
         "The aortic valve lies between the left ventricle and the aorta. It opens during ventricular contraction to allow blood into the aorta and closes to prevent backflow.",
         "Heart Valves") },

    { "pulmonary valve",
        ("Pulmonary Valve",
         "The pulmonary valve lies between the right ventricle and the pulmonary artery. It opens during ventricular contraction and prevents blood from flowing back into the right ventricle.",
         "Heart Valves") },

    { "right atrioventricular valve",
        ("Tricuspid Valve",
         "The tricuspid valve is the right atrioventricular valve located between the right atrium and right ventricle. It prevents blood from flowing back into the atrium during ventricular contraction.",
         "Heart Valves") },

    { "tricuspid valve",
        ("Tricuspid Valve",
         "The tricuspid valve is the right atrioventricular valve with three cusps. It regulates blood flow from the right atrium into the right ventricle.",
         "Heart Valves") },

    { "atrioventricular valve",
        ("Atrioventricular Valve",
         "Atrioventricular valves regulate blood flow between the atria and ventricles and help prevent backward flow during ventricular contraction.",
         "Heart Valves") },

    { "left atrioventricular orifice",
        ("Left Atrioventricular Orifice",
         "The left atrioventricular orifice is the opening between the left atrium and left ventricle through which blood passes.",
         "Heart Valves") },

    { "right atrioventricular orifice",
        ("Right Atrioventricular Orifice",
         "The right atrioventricular orifice is the opening between the right atrium and right ventricle through which blood passes.",
         "Heart Valves") },

    { "valve of coronary sinus",
        ("Valve of Coronary Sinus",
         "The valve of the coronary sinus is a fold of tissue at the opening of the coronary sinus into the right atrium. It may help prevent blood from flowing backward into the coronary sinus.",
         "Heart Valves") },

    // ── GREAT VESSELS ─────────────────────────────────────────────
    { "aorta",
        ("Aorta",
         "The aorta is the main and largest artery of the body. It originates from the left ventricle and distributes oxygenated blood throughout the systemic circulation.",
         "Great Vessels") },

    { "ascending aorta",
        ("Ascending Aorta",
         "The ascending aorta is the first portion of the aorta, beginning at the base of the left ventricle and ascending upward before continuing into the aortic arch.",
         "Great Vessels") },

    { "aortic arch",
        ("Aortic Arch",
         "The aortic arch is the curved portion of the aorta between the ascending and descending aorta. It gives rise to major arteries supplying the head, neck, and upper limbs.",
         "Great Vessels") },

    { "descending aorta",
        ("Descending Aorta",
         "The descending aorta begins at the aortic arch and travels downward through the thorax and abdomen. It consists of thoracic and abdominal portions.",
         "Great Vessels") },

    { "thoracic aorta",
        ("Thoracic Aorta",
         "The thoracic aorta is the portion of the descending aorta located within the thorax.",
         "Great Vessels") },

    { "abdominal aorta",
        ("Abdominal Aorta",
         "The abdominal aorta is the portion of the aorta located within the abdomen. It eventually divides into the common iliac arteries.",
         "Great Vessels") },

    { "root of aorta",
        ("Root of Aorta",
         "The aortic root is the portion of the aorta beginning at the aortic valve and extending to the ascending aorta.",
         "Great Vessels") },

    { "pulmonary trunk",
        ("Pulmonary Trunk",
         "The pulmonary trunk is the main pulmonary artery arising from the right side of the heart. It carries deoxygenated blood from the right ventricle toward the lungs.",
         "Great Vessels") },

    { "pulmonary artery",
        ("Pulmonary Artery",
         "The pulmonary arteries carry deoxygenated blood from the right side of the heart to the lungs. They are an exception to the usual pattern of arteries carrying oxygenated blood.",
         "Great Vessels") },

    { "pulmonary arteries",
        ("Pulmonary Arteries",
         "The pulmonary arteries are vessels of the pulmonary circulation that carry deoxygenated blood from the right side of the heart to the lungs.",
         "Great Vessels") },

    { "pulmonary vein",
        ("Pulmonary Vein",
         "The pulmonary veins carry oxygenated blood from the lungs to the left atrium. There are normally four main pulmonary veins, two from each lung.",
         "Great Vessels") },

    { "pulmonary veins",
        ("Pulmonary Veins",
         "The pulmonary veins return oxygenated blood from the lungs to the left atrium as part of the pulmonary circulation.",
         "Great Vessels") },

    { "superior vena cava",
        ("Superior Vena Cava",
         "The superior vena cava is a large vein that returns deoxygenated blood from the upper half of the body to the right atrium.",
         "Great Vessels") },

    { "inferior vena cava",
        ("Inferior Vena Cava",
         "The inferior vena cava is a large vein that carries deoxygenated blood from the lower and middle portions of the body to the right atrium.",
         "Great Vessels") },

    { "vena cava",
        ("Vena Cava",
         "The venae cavae are the superior and inferior venae cavae, which return deoxygenated blood from the systemic circulation to the right atrium.",
         "Great Vessels") },

    // ── CORONARY CIRCULATION ──────────────────────────────────────
    { "coronary circulation",
        ("Coronary Circulation",
         "Coronary circulation is the blood circulation supplying the heart muscle. Coronary arteries deliver oxygenated blood to the myocardium, while cardiac veins drain blood from the heart muscle.",
         "Coronary Vessels") },

    { "cardiac vessels",
        ("Cardiac Vessels",
         "Cardiac vessels are the blood vessels that supply and drain the heart muscle as part of the coronary circulation.",
         "Coronary Vessels") },

    { "left coronary artery",
        ("Left Coronary Artery",
         "The left coronary artery arises from the aorta above the left cusp of the aortic valve and supplies blood to the left side of the heart muscle.",
         "Coronary Vessels") },

    { "right coronary artery",
        ("Right Coronary Artery",
         "The right coronary artery originates from the right aortic sinus and travels along the right coronary sulcus. It supplies the right side of the heart and portions of the interventricular septum.",
         "Coronary Vessels") },

    { "left anterior descending artery",
        ("Left Anterior Descending Artery",
         "The left anterior descending artery is a branch of the left coronary artery that travels along the anterior interventricular sulcus and supplies portions of the heart muscle.",
         "Coronary Vessels") },

    { "lad",
        ("Left Anterior Descending Artery",
         "The LAD, or left anterior descending artery, is a branch of the left coronary artery that runs along the anterior interventricular sulcus.",
         "Coronary Vessels") },

    { "circumflex artery of heart",
        ("Circumflex Artery",
         "The circumflex artery is a branch of the left coronary artery that follows the left portion of the coronary sulcus around the heart.",
         "Coronary Vessels") },

    { "circumflex artery",
        ("Circumflex Artery",
         "The circumflex artery is a branch of the left coronary artery that travels along the coronary sulcus.",
         "Coronary Vessels") },

    { "coronary sinus",
        ("Coronary Sinus",
         "The coronary sinus is a large venous vessel formed by veins of the heart. It collects less-oxygenated blood from the heart muscle and drains it into the right atrium.",
         "Coronary Vessels") },

    { "cardiac veins",
        ("Cardiac Veins",
         "Cardiac veins drain less-oxygenated blood from the heart muscle and return it toward the right atrium, primarily through the coronary sinus.",
         "Coronary Vessels") },

    { "great cardiac vein",
        ("Great Cardiac Vein",
         "The great cardiac vein is a major vein of the heart that contributes to the coronary venous drainage.",
         "Coronary Vessels") },

    { "middle cardiac vein",
        ("Middle Cardiac Vein",
         "The middle cardiac vein is a cardiac vein that participates in drainage of blood from the heart muscle toward the coronary sinus.",
         "Coronary Vessels") },

    // ── CONDUCTION SYSTEM ─────────────────────────────────────────
    { "electrical conduction system",
        ("Electrical Conduction System of the Heart",
         "The electrical conduction system transmits signals that coordinate contraction of the heart. The signal normally begins at the sinoatrial node, passes to the atrioventricular node, and continues through the Bundle of His and bundle branches.",
         "Conduction System") },

    { "sinoatrial node",
        ("Sinoatrial Node",
         "The sinoatrial node is a group of pacemaker cells located in the wall of the right atrium. It generates electrical impulses that establish the normal rhythm of the heart.",
         "Conduction System") },

    { "sa node",
        ("Sinoatrial Node",
         "The SA node is the heart's natural pacemaker. It produces electrical impulses that initiate the normal heartbeat.",
         "Conduction System") },

    { "atrioventricular node",
        ("Atrioventricular Node",
         "The atrioventricular node is part of the heart's electrical conduction system. It receives electrical impulses from the atria and conducts them toward the ventricles.",
         "Conduction System") },

    { "av node",
        ("Atrioventricular Node",
         "The AV node electrically connects the atria and ventricles and helps coordinate the timing of ventricular contraction.",
         "Conduction System") },

    // ── MAJOR ARTERIES ────────────────────────────────────────────
    { "brachiocephalic artery",
        ("Brachiocephalic Artery",
         "The brachiocephalic artery is a major branch of the aortic arch that supplies the right side of the head and neck and the right upper limb.",
         "Major Arteries") },

    { "brachiocephalic",
        ("Brachiocephalic Artery",
         "The brachiocephalic artery is a major branch of the aortic arch supplying the right side of the head and neck and the right upper limb.",
         "Major Arteries") },

    { "common carotid artery",
        ("Common Carotid Artery",
         "The common carotid arteries are major arteries of the head and neck. Each divides into internal and external carotid arteries.",
         "Major Arteries") },

    { "carotid artery",
        ("Carotid Artery",
         "The carotid arteries are major vessels supplying the head and neck, including the brain and face.",
         "Major Arteries") },

    { "internal carotid artery",
        ("Internal Carotid Artery",
         "The internal carotid artery arises from the common carotid artery and supplies the brain and eyes.",
         "Major Arteries") },

    { "external carotid artery",
        ("External Carotid Artery",
         "The external carotid artery arises from the common carotid artery and supplies structures of the face, scalp, and neck.",
         "Major Arteries") },

    { "subclavian artery",
        ("Subclavian Artery",
         "The subclavian arteries are major arteries located below the clavicles. They supply the upper limbs and give branches to the head and thorax.",
         "Major Arteries") },

    { "brachial artery",
        ("Brachial Artery",
         "The brachial artery is the major artery of the upper arm. It continues from the axillary artery and divides into the radial and ulnar arteries near the elbow.",
         "Major Arteries") },

    { "radial artery",
        ("Radial Artery",
         "The radial artery is the main artery on the lateral side of the forearm. It arises from the brachial artery and continues toward the hand.",
         "Major Arteries") },

    { "ulnar artery",
        ("Ulnar Artery",
         "The ulnar artery is the main artery on the medial side of the forearm. It arises from the brachial artery and contributes to the blood supply of the hand.",
         "Major Arteries") },

    { "renal artery",
        ("Renal Artery",
         "The renal arteries are paired arteries that supply blood to the kidneys and arise from the abdominal aorta.",
         "Major Arteries") },

    { "femoral artery",
        ("Femoral Artery",
         "The femoral artery is the main arterial supply to the thigh and lower limb. It continues from the external iliac artery and becomes the popliteal artery near the adductor hiatus.",
         "Major Arteries") },

    { "popliteal artery",
        ("Popliteal Artery",
         "The popliteal artery is a continuation of the femoral artery behind the knee. It eventually divides into the anterior and posterior tibial arteries.",
         "Major Arteries") },

    { "anterior tibial artery",
        ("Anterior Tibial Artery",
         "The anterior tibial artery supplies the anterior compartment of the leg and the dorsal surface of the foot.",
         "Major Arteries") },

    { "posterior tibial artery",
        ("Posterior Tibial Artery",
         "The posterior tibial artery supplies the posterior compartment of the leg and the plantar surface of the foot.",
         "Major Arteries") },

    { "vertebral artery",
        ("Vertebral Artery",
         "The vertebral arteries are branches of the subclavian arteries that travel through the cervical region and contribute to the blood supply of the brain and spinal cord.",
         "Major Arteries") },

    // ── MAJOR VEINS ───────────────────────────────────────────────
    { "jugular vein",
        ("Jugular Vein",
         "The jugular veins return deoxygenated blood from the head and neck toward the heart. They include the internal and external jugular veins.",
         "Major Veins") },

    { "internal jugular vein",
        ("Internal Jugular Vein",
         "The internal jugular vein drains blood from the brain and superficial regions of the face and neck. It joins the subclavian vein to form the brachiocephalic vein.",
         "Major Veins") },

    { "external jugular vein",
        ("External Jugular Vein",
         "The external jugular vein drains much of the exterior of the cranium and portions of the face and neck before emptying into the subclavian vein.",
         "Major Veins") },

    { "subclavian vein",
        ("Subclavian Vein",
         "The subclavian vein drains blood from the upper limb and joins the internal jugular vein to form the brachiocephalic vein.",
         "Major Veins") },

    { "brachiocephalic vein",
        ("Brachiocephalic Vein",
         "The brachiocephalic veins are formed by the union of the internal jugular and subclavian veins. The left and right brachiocephalic veins join to form the superior vena cava.",
         "Major Veins") },

    { "hepatic portal vein",
        ("Hepatic Portal Vein",
         "The hepatic portal vein carries blood from the gastrointestinal tract, gallbladder, pancreas, and spleen to the liver.",
         "Major Veins") },

    { "portal vein",
        ("Hepatic Portal Vein",
         "The hepatic portal vein carries blood from the digestive organs and related structures to the liver before it returns to the systemic circulation.",
         "Major Veins") },

    { "hepatic veins",
        ("Hepatic Veins",
         "The hepatic veins drain blood from the liver into the inferior vena cava.",
         "Major Veins") },

    { "renal vein",
        ("Renal Vein",
         "The renal veins drain blood from the kidneys and return it to the inferior vena cava.",
         "Major Veins") },

    { "femoral vein",
        ("Femoral Vein",
         "The femoral vein accompanies the femoral artery in the thigh and continues from the popliteal vein before becoming the external iliac vein.",
         "Major Veins") },

    { "great saphenous vein",
        ("Great Saphenous Vein",
         "The great saphenous vein is a large superficial vein of the lower limb and is the longest vein in the body. It returns blood from the foot, leg, and thigh.",
         "Major Veins") },

    { "popliteal vein",
        ("Popliteal Vein",
         "The popliteal vein is a deep vein located behind the knee that continues into the femoral vein.",
         "Major Veins") },

    // ── MICRO-CIRCULATION ─────────────────────────────────────────
    { "artery",
        ("Artery",
         "An artery is a blood vessel that carries blood away from the heart. Most arteries carry oxygenated blood, while pulmonary arteries carry deoxygenated blood to the lungs.",
         "Blood Vessels") },

    { "vein",
        ("Vein",
         "A vein is a blood vessel that carries blood toward the heart. Most veins carry deoxygenated blood, with pulmonary veins being an important exception.",
         "Blood Vessels") },

    { "capillary",
        ("Capillary",
         "A capillary is a very small blood vessel connecting arterioles and venules. Capillaries are major sites for exchange of oxygen, nutrients, waste, and other substances with surrounding tissues.",
         "Blood Vessels") },

    { "arteriole",
        ("Arteriole",
         "An arteriole is a small blood vessel that branches from an artery and leads toward capillaries. Its muscular walls help regulate vascular resistance and blood flow.",
         "Blood Vessels") },

    // ── PULMONARY CIRCULATION ─────────────────────────────────────
    { "pulmonary circulation",
        ("Pulmonary Circulation",
         "Pulmonary circulation carries deoxygenated blood from the right side of the heart to the lungs, where it is oxygenated, and returns the oxygenated blood to the left side of the heart.",
         "Pulmonary Circulation") },

    { "pulmonary vessels",
        ("Pulmonary Vessels",
         "Pulmonary vessels include the pulmonary arteries and pulmonary veins that transport blood between the heart and lungs.",
         "Pulmonary Circulation") },

    // ── SYSTEMIC CIRCULATION ──────────────────────────────────────
    { "systemic arteries",
        ("Systemic Arteries",
         "Systemic arteries carry oxygenated blood from the left side of the heart to tissues throughout the body.",
         "Systemic Circulation") },

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