#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// MUSCULAR SYSTEM AUTO SETUP — Anatomia 3D
// Adds MeshCollider + StructureInfo to every muscle mesh.
// Mirrors SkeletonAutoSetup

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
            { "rectus oculi",     ("Rectus Oculi",       "Muscles that move the eyeball.", "Head & Neck") }, //added
            // Real FBX names for these are e.g. "Superior rectus muscle" — no "oculi" in them —
            // so they need their own keys or they fall through to the generic "rectus"/"oblique" keys below.
            { "superior rectus",  ("Superior Rectus",    "Extraocular muscle that elevates the eyeball.", "Head & Neck") },
            { "inferior rectus",  ("Inferior Rectus",    "Extraocular muscle that depresses the eyeball.", "Head & Neck") },
            { "medial rectus",    ("Medial Rectus",      "Extraocular muscle that adducts the eyeball.", "Head & Neck") },
            { "lateral rectus",   ("Lateral Rectus",     "Extraocular muscle that abducts the eyeball.", "Head & Neck") },
            { "superior oblique", ("Superior Oblique",   "Extraocular muscle that rotates and depresses the eyeball.", "Head & Neck") },
            { "inferior oblique", ("Inferior Oblique",   "Extraocular muscle that rotates and elevates the eyeball.", "Head & Neck") },
            { "capitis",          ("Capitis Muscle",     "Deep muscle of the neck that moves or stabilizes the head.", "Head & Neck") },
            { "arytenoid",        ("Arytenoid Muscle",   "Intrinsic laryngeal muscle that moves the vocal folds.", "Head & Neck") },
            { "transverse arytenoid",("Transverse Arytenoid","Intrinsic laryngeal muscle that adducts the vocal folds.", "Head & Neck") },
            { "cricothyroid",     ("Cricothyroid",       "Tenses the vocal folds; the only larynx muscle innervated externally.", "Head & Neck") },
            { "transverse part of trapezius",("Trapezius","Moves, rotates, and stabilizes the shoulder blade.", "Head & Neck") },
            { "temporalis",       ("Temporalis",         "A broad fan-shaped muscle on the side of the skull that elevates and retracts the mandible during chewing.", "Head & Neck") },
            { "masseter",         ("Masseter",           "The masseter is one of the strongest muscles of mastication. It elevates the mandible to close the jaw during chewing.", "Head & Neck") },
            { "orbicularis oculi",("Orbicularis Oculi",  "A circular facial muscle that closes the eyelids and assists in blinking.", "Head & Neck") },
            { "orbicularis oris", ("Orbicularis Oris",   "A circular muscle surrounding the mouth that controls lip movement.", "Head & Neck") },
            { "zygomaticus",      ("Zygomaticus",        "Pulls the corners of the mouth upward when smiling.", "Head & Neck") },
            { "buccinator",       ("Buccinator",         "A facial muscle that compresses the cheeks during chewing, blowing, and sucking.", "Head & Neck") },
            { "sternocleidomastoid",("Sternocleidomastoid","Rotates and flexes the neck; tilts the head.", "Head & Neck") },
            { "scm",              ("Sternocleidomastoid","Rotates and flexes the neck; tilts the head.", "Head & Neck") },
            { "platysma",         ("Platysma",           "A thin superficial neck muscle that depresses the lower jaw and tenses the skin of the neck.", "Head & Neck") },
            { "trapezius",        ("Trapezius",          "Moves, rotates, and stabilizes the shoulder blade.", "Head & Neck") },
            { "splenius",         ("Splenius",           "Rotates and extends the head and neck.", "Head & Neck") },
            { "scalene",          ("Scalene",            "Flexes and rotates the cervical spine; assists breathing.", "Head & Neck") },
            { "oblique capitis",  ("Oblique Capitis",    "Rotates the head.", "Head & Neck") }, //added

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
            { "supraspinatus",    ("Supraspinatus",      "A rotator cuff muscle that initiates arm abduction and stabilizes the shoulder joint.", "Shoulder") },
            { "infraspinatus",    ("Infraspinatus",      "A rotator cuff muscle that laterally rotates the arm and stabilizes the shoulder.", "Shoulder") },
            { "teres minor",      ("Teres Minor",        "A rotator cuff muscle that assists with lateral rotation of the arm.", "Shoulder") },
            { "teres major",      ("Teres Major",        "A muscle that extends, adducts, and medially rotates the arm at the shoulder.", "Shoulder") },
            { "subscapularis",    ("Subscapularis",      "A rotator cuff muscle that medially rotates the humerus and stabilizes the shoulder joint.", "Shoulder") },

            // ── UPPER ARM ─────────────────────────────────────────
            { "biceps brachii",   ("Biceps Brachii",     "Flexes the elbow and supinates the forearm.", "Upper Arm") },
            { "biceps",           ("Biceps Brachii",     "Flexes the elbow and supinates the forearm.", "Upper Arm") },
            { "brachialis",       ("Brachialis",         "The primary flexor of the elbow joint located beneath the biceps brachii.", "Upper Arm") },
            { "triceps brachii",  ("Triceps Brachii",    "Extends the elbow; the only muscle on the back of the upper arm.", "Upper Arm") },
            { "triceps",          ("Triceps Brachii",    "Extends the elbow; the only muscle on the back of the upper arm.", "Upper Arm") },
            { "coracobrachialis",  ("Coracobrachialis",  "A muscle that flexes and adducts the arm at the shoulder.", "Upper Arm") },

            // ── FOREARM ───────────────────────────────────────────
            { "brachioradialis",  ("Brachioradialis",    "A forearm muscle that flexes the elbow, especially when the forearm is in a neutral position.", "Forearm") },
            { "pronator",         ("Pronator",           "Rotates the forearm to face downward (pronation).", "Forearm") },
            { "supinator",        ("Supinator",          "A muscle that rotates the forearm laterally, turning the palm upward. (supination).", "Forearm") },
            { "flexor carpi",     ("Flexor Carpi",       "Flexes and abducts or adducts the wrist.", "Forearm") },
            { "extensor carpi",   ("Extensor Carpi",     "Extends and abducts or adducts the wrist.", "Forearm") },
            { "flexor digitorum", ("Flexor Digitorum",   "Flexes the fingers and wrist.", "Forearm") },
            { "extensor digitorum",("Extensor Digitorum","Extends the fingers and wrist.", "Forearm") },
            { "palmaris longus",  ("Palmaris Longus",    "Flexes the wrist; absent in some people.", "Forearm") },
            { "anconeus",         ("Anconeus",           "A small muscle that assists the triceps in extending the elbow and stabilizing the joint.", "Forearm") },
            { "metacarpal ligament",("Metacarpal Ligament","Ligament stabilizing the metacarpal bones of the hand.", "Hand") },
            { "lumbrical muscles of hand",("Lumbricals (Hand)","Flex the fingers at the knuckles while extending the other joints.", "Hand") },
            { "interossei muscles of hand",("Interossei (Hand)","Abduct/adduct the fingers and assist finger flexion.", "Hand") },
            { "digiti minimi of hand",("Digiti Minimi (Hand)","Muscles acting on the little finger.", "Hand") },
            { "of hand",          ("Hand Muscle",        "Intrinsic muscle or ligament of the hand.", "Hand") },

            // ── ABDOMEN ───────────────────────────────────────────
            { "rectus abdominis", ("Rectus Abdominis",   "The paired abdominal muscle that flexes the trunk and compresses abdominal contents", "Abdomen") },
            { "rectus",           ("Rectus Abdominis",   "The 'six-pack' muscle; flexes the vertebral column.", "Abdomen") },
            { "external oblique", ("External Oblique",   "The largest abdominal muscle that rotates and flexes the trunk.", "Abdomen") },
            { "internal oblique", ("Internal Oblique",   "An abdominal muscle that assists trunk rotation and supports the abdominal wall.", "Abdomen") },
            { "oblique",          ("Oblique",            "Rotates and laterally flexes the trunk.", "Abdomen") },
            { "transversus abdominis",("Transversus Abdominis","The deepest abdominal muscle that compresses and stabilizes the abdominal cavity.", "Abdomen") },
            { "transverse",       ("Transversus Abdominis","Compresses the abdomen; the deepest abdominal muscle.", "Abdomen") },
            { "diaphragm",        ("Diaphragm",          "The primary breathing muscle; separates chest and abdomen.", "Abdomen") },

            // ── BACK ──────────────────────────────────────────────
            { "latissimus dorsi", ("Latissimus Dorsi",   "The broad back muscle; adducts, extends, and rotates the arm.", "Back") },
            { "latissimus",       ("Latissimus Dorsi",   "The broad back muscle; adducts, extends, and rotates the arm.", "Back") },
            { "lats",             ("Latissimus Dorsi",   "The broad back muscle; adducts, extends, and rotates the arm.", "Back") },
            { "rhomboid",         ("Rhomboid",           "Retracts and elevates the scapula.", "Back") },
            { "levator scapulae", ("Levator Scapulae",   "Elevates the scapula and tilts the head.", "Back") },
            { "erector spinae",   ("Erector Spinae",     "A group of muscles that extends and laterally flexes the vertebral column.", "Back") },
            { "erector",          ("Erector Spinae",     "Extends and laterally flexes the vertebral column.", "Back") },
            { "multifidus",       ("Multifidus",         "A deep back muscle that stabilizes individual vertebrae during movement.", "Back") },
            { "quadratus lumborum",("Quadratus Lumborum","A deep abdominal wall muscle that laterally flexes the trunk and stabilizes the pelvis.", "Back") },

            // ── GLUTEAL ───────────────────────────────────────────
            { "gluteus maximus",  ("Gluteus Maximus",    "The largest gluteal muscle responsible for hip extension and external rotation.", "Gluteal") },
            { "gluteus medius",   ("Gluteus Medius",     "A muscle that abducts the thigh and stabilizes the pelvis during walking.", "Gluteal") },
            { "gluteus minimus",  ("Gluteus Minimus",    "The smallest gluteal muscle that assists hip abduction and medial rotation.", "Gluteal") },
            { "gluteus",          ("Gluteus",            "The gluteal muscles extend, abduct, and rotate the hip.", "Gluteal") },
            { "glute",            ("Gluteus",            "The gluteal muscles extend, abduct, and rotate the hip.", "Gluteal") },
            { "piriformis",       ("Piriformis",         "A muscle that laterally rotates the extended thigh and stabilizes the hip joint.", "Gluteal") },
            { "tensor fasciae latae",("Tensor Fasciae Latae","Abducts and medially rotates the thigh.", "Gluteal") },
            { "iliotibial",       ("Iliotibial Band",    "Stabilizes the knee and assists in hip abduction.", "Gluteal") },

            // ── THIGH ─────────────────────────────────────────────
            { "quadriceps",       ("Quadriceps",         "Group of four muscles that extend the knee.", "Thigh") },
            { "rectus femoris",   ("Rectus Femoris",     "A quadriceps muscle that extends the knee and flexes the hip.", "Thigh") },
            { "vastus lateralis", ("Vastus Lateralis",   "The largest quadriceps muscle that extends the knee.", "Thigh") },
            { "vastus medialis",  ("Vastus Medialis",    "A quadriceps muscle important for stabilizing the patella during knee extension.", "Thigh") },
            { "vastus intermedius",("Vastus Intermedius","A deep quadriceps muscle that extends the knee.", "Thigh") },
            { "vastus",           ("Vastus",             "Part of the quadriceps group that extends the knee.", "Thigh") },
            { "hamstring",        ("Hamstrings",         "Group of muscles that flex the knee and extend the hip.", "Thigh") },
            { "biceps femoris",   ("Biceps Femoris",     "A hamstring muscle that flexes the knee and extends the hip.", "Thigh") },
            { "semitendinosus",   ("Semitendinosus",     "A hamstring muscle involved in knee flexion and hip extension.", "Thigh") },
            { "semimembranosus",  ("Semimembranosus",    "A hamstring muscle that extends the hip and flexes the knee.", "Thigh") },
            { "sartorius",        ("Sartorius",          "The longest muscle in the body that flexes, abducts, and laterally rotates the thigh.", "Thigh") },
            { "adductor",         ("Adductor",           "Adducts (brings together) the thigh.", "Thigh") },
            { "gracilis",         ("Gracilis",           "A long, slender muscle that adducts the thigh and flexes the knee.", "Thigh") },
            { "iliopsoas",        ("Iliopsoas",          "Flexes the hip; the primary hip flexor muscle.", "Thigh") },
            { "iliacus",          ("Iliacus",            "Flexes the hip jointly with the psoas.", "Thigh") },
            { "psoas",            ("Psoas Major",        "Flexes the hip; the deepest hip flexor.", "Thigh") },
            { "pectineus",        ("Pectineus",          "Adducts and flexes the thigh.", "Thigh") },

            // ── LOWER LEG ─────────────────────────────────────────
            { "gastrocnemius",    ("Gastrocnemius",      "The large superficial calf muscle responsible for plantarflexion of the ankle and knee flexion.", "Lower Leg") },
            { "soleus",           ("Soleus",             "A deep calf muscle that produces plantarflexion during standing and walking.", "Lower Leg") },
            { "calf",             ("Calf",               "The calf muscles plantarflex the foot.", "Lower Leg") },
            { "tibialis anterior",("Tibialis Anterior",  "The primary muscle responsible for dorsiflexion of the foot.", "Lower Leg") },
            { "tibialis",         ("Tibialis",           "Controls foot movement and ankle stability.", "Lower Leg") },
            { "peroneus",         ("Peroneus",           "Everts the foot and assists in plantarflexion.", "Lower Leg") },
            { "fibularis",        ("Fibularis",          "Everts the foot and assists in plantarflexion.", "Lower Leg") },
            { "extensor hallucis",("Extensor Hallucis",  "Extends the big toe and assists dorsiflexion.", "Lower Leg") },
            { "flexor hallucis",  ("Flexor Hallucis",    "Flexes the big toe; assists in plantarflexion.", "Lower Leg") },
            { "popliteus",        ("Popliteus",          "Unlocks the knee to allow flexion.", "Lower Leg") },
            { "achilles",         ("Achilles Tendon",    "The largest tendon; connects the calf muscles to the heel.", "Lower Leg") },
            { "intermuscular septum of leg",("Intermuscular Septum","Fibrous partition separating muscle compartments of the leg.", "Lower Leg") },

            // ── FOOT ──────────────────────────────────────────────
            { "plantar",          ("Plantar Muscles",    "Intrinsic muscles of the foot supporting the arch.", "Foot") },
            { "abductor hallucis",("Abductor Hallucis",  "An intrinsic foot muscle that abducts and flexes the great toe.", "Foot") },
            { "flexor digitorum brevis",("Flexor Digitorum Brevis","An intrinsic foot muscle that flexes the lateral four toes.", "Foot") },
            { "metatarsal ligament",("Metatarsal Ligament","Ligament stabilizing the metatarsal bones of the foot.", "Foot") },

            // ---- EXPANDED COVERAGE - matches this asset full anatomical naming ----
            { "digastric", ("Digastric", "Has two bellies joined by a tendon; opens the jaw and raises the hyoid bone.", "Head & Neck") },
            { "genioglossus", ("Genioglossus", "The main muscle of the tongue; protrudes and depresses it.", "Head & Neck") },
            { "geniohyoid", ("Geniohyoid", "Elevates the hyoid bone and widens the pharynx during swallowing.", "Head & Neck") },
            { "hyoglossus", ("Hyoglossus", "Depresses and retracts the tongue.", "Head & Neck") },
            { "mylohyoid", ("Mylohyoid", "Forms the floor of the mouth; elevates the hyoid and tongue.", "Head & Neck") },
            { "omohyoid", ("Omohyoid", "Depresses the hyoid bone; stabilizes it during swallowing and speech.", "Head & Neck") },
            { "sternohyoid", ("Sternohyoid", "Depresses the hyoid bone.", "Head & Neck") },
            { "sternothyroid", ("Sternothyroid", "Depresses the thyroid cartilage of the larynx.", "Head & Neck") },
            { "stylohyoid", ("Stylohyoid", "Elevates and retracts the hyoid bone during swallowing.", "Head & Neck") },
            { "thyrohyoid", ("Thyrohyoid", "Raises the larynx and lowers the hyoid bone.", "Head & Neck") },
            { "hyoid muscles", ("Hyoid Muscles", "Group of muscles that raise or lower the hyoid bone and larynx.", "Head & Neck") },
            { "palatopharyngeus", ("Palatopharyngeus", "Narrows the pharynx and raises the larynx during swallowing.", "Head & Neck") },
            { "stylopharyngeus", ("Stylopharyngeus", "Elevates and widens the pharynx during swallowing.", "Head & Neck") },
            { "pharyngeal", ("Pharyngeal Muscles", "Muscles of the throat that propel food during swallowing.", "Head & Neck") },
            { "laryngeal", ("Laryngeal Muscles", "Intrinsic muscles of the voice box that control the vocal folds.", "Head & Neck") },
            { "pterygoid", ("Pterygoid", "Chewing muscle that moves the jaw side-to-side and forward.", "Head & Neck") },
            { "mentalis", ("Mentalis", "Raises and wrinkles the skin of the chin.", "Head & Neck") },
            { "nasalis", ("Nasalis", "Compresses or widens the nostrils.", "Head & Neck") },
            { "occipitalis", ("Occipitalis", "Pulls the scalp backward.", "Head & Neck") },
            { "procerus", ("Procerus", "Wrinkles the skin between the eyebrows.", "Head & Neck") },
            { "risorius", ("Risorius", "Pulls the corner of the mouth sideways, as in a grin.", "Head & Neck") },
            { "temporoparietalis", ("Temporoparietalis", "A vestigial scalp muscle; tightens the scalp.", "Head & Neck") },
            { "epicrani", ("Epicranius", "The broad scalp muscle-aponeurosis complex covering the skull.", "Head & Neck") },
            { "auricular", ("Auricular Muscles", "Small muscles around the ear; mostly vestigial in humans.", "Head & Neck") },
            { "auditory ossicles", ("Auditory Ossicle Muscles", "Tiny muscles that dampen sound transmitted through the middle ear.", "Head & Neck") },
            { "masticatory", ("Masticatory Muscles", "The muscle group responsible for chewing.", "Head & Neck") },
            { "facial muscles", ("Facial Muscles", "Muscles that produce facial expressions.", "Head & Neck") },
            { "soft palate", ("Soft Palate Muscles", "Muscles that close off the nasal cavity during swallowing.", "Head & Neck") },
            { "tongue", ("Tongue Muscles", "Muscles that shape and move the tongue.", "Head & Neck") },
            { "scalenus", ("Scalene", "Flexes and rotates the cervical spine; assists breathing.", "Head & Neck") },
            { "suboccipital", ("Suboccipital Muscles", "Small deep muscles at the base of the skull that fine-tune head movement.", "Head & Neck") },
            { "cervical", ("Cervical Fascia", "Connective tissue layer of the neck.", "Head & Neck") },
            { "temporal fascia", ("Temporal Fascia", "Connective tissue covering the temporalis chewing muscle.", "Head & Neck") },
            { "muscles of head", ("Head Muscles", "Muscles of the head region.", "Head & Neck") },
            { "muscles of neck", ("Neck Muscles", "Muscles of the neck region.", "Head & Neck") },
            { "extra-ocular", ("Extraocular Muscles", "The muscles that move the eyeball.", "Head & Neck") },
            { "iliocostalis colli", ("Iliocostalis Cervicis", "Extends and laterally flexes the neck.", "Head & Neck") },
            { "longissimus colli", ("Longissimus Cervicis", "Extends and laterally flexes the neck.", "Head & Neck") },
            { "longus colli", ("Longus Colli", "Flexes and stabilizes the cervical spine.", "Head & Neck") },
            { "semispinalis colli", ("Semispinalis Cervicis", "Extends and rotates the neck.", "Head & Neck") },
            { "spinalis colli", ("Spinalis Cervicis", "Extends the neck.", "Head & Neck") },
            { "interspinales colli", ("Interspinales Cervicis", "Stabilizes and extends the neck between vertebrae.", "Head & Neck") },
            { "iliocostalis", ("Iliocostalis", "The lateral column of the erector spinae muscle group.", "Back") },
            { "longissimus", ("Longissimus", "The intermediate column of the erector spinae muscle group.", "Back") },
            { "semispinalis", ("Semispinalis", "Extends and rotates the vertebral column.", "Back") },
            { "spinalis", ("Spinalis", "The medial column of the erector spinae muscle group.", "Back") },
            { "interspinales", ("Interspinales", "Small muscles that stabilize and extend the spine between vertebrae.", "Back") },
            { "intertransversarii", ("Intertransversarii", "Small muscles that stabilize and laterally flex the spine.", "Back") },
            { "transversospinal", ("Transversospinal Muscles", "Deep back muscle group that stabilizes and rotates the spine.", "Back") },
            { "spinotransversales", ("Spinotransversales", "Deep back muscle group connecting the spine to the transverse processes.", "Back") },
            { "epaxial", ("Epaxial Muscles", "The deep back-extensor muscle group.", "Back") },
            { "hypaxial", ("Hypaxial Muscles", "Trunk muscle group involved in flexion and breathing.", "Back") },
            { "thoracolumbar fascia", ("Thoracolumbar Fascia", "Connective tissue layer that anchors the back's deep muscles.", "Back") },
            { "muscles of thorax", ("Thorax Muscles", "Muscles of the chest region.", "Chest") },
            { "fasciae of head", ("Head Fascia", "Connective tissue layer of the head.", "Head & Neck") },
            { "fasciae of thorax", ("Thoracic Fascia", "Connective tissue layer of the chest.", "Chest") },
            { "transversus thoracis", ("Transversus Thoracis", "Assists in forced exhalation by pulling down on the ribs.", "Chest") },
            { "abdominal fascia", ("Abdominal Fascia", "Connective tissue layer of the abdominal wall.", "Abdomen") },
            { "muscles of abdomen", ("Abdominal Muscles", "Muscles of the abdominal wall.", "Abdomen") },
            { "fasciae of abdomen", ("Abdominal Fascia", "Connective tissue layer of the abdominal wall.", "Abdomen") },
            { "inguinal", ("Inguinal Ligament", "Ligament forming the boundary between the abdomen and thigh.", "Abdomen") },
            { "pyramidalis", ("Pyramidalis", "Small muscle that tenses the linea alba of the abdominal wall.", "Abdomen") },
            { "transversalis fascia", ("Transversalis Fascia", "Deep connective tissue layer of the abdominal wall.", "Abdomen") },
            { "intertubercular", ("Intertubercular Tendon Sheath", "Sheath surrounding the biceps tendon in the shoulder groove.", "Shoulder") },
            { "scapulohumeral", ("Scapulohumeral Muscles", "Muscle group connecting the scapula to the humerus.", "Shoulder") },
            { "brachial fascia", ("Brachial Fascia", "Connective tissue sheath of the upper arm.", "Upper Arm") },
            { "intermuscular septum of arm", ("Intermuscular Septum", "Fibrous partition separating muscle compartments of the arm.", "Upper Arm") },
            { "muscles of upper limb", ("Upper Limb Muscles", "Muscles of the arm region.", "Upper Arm") },
            { "fasciae of upper limb", ("Upper Limb Fascia", "Connective tissue layer of the arm.", "Upper Arm") },
            { "antebrachial", ("Antebrachial Fascia", "Connective tissue sheath of the forearm.", "Forearm") },
            { "retinaculum of wrist", ("Wrist Retinaculum", "Fibrous band that holds tendons in place as they cross the wrist.", "Forearm") },
            { "carpal tendon sheath", ("Carpal Tendon Sheath", "Synovial sheath surrounding tendons as they cross the wrist.", "Forearm") },
            { "common flexor tendon sheath", ("Common Flexor Tendon Sheath", "Synovial sheath surrounding the wrist flexor tendons.", "Forearm") },
            { "extensors carpi", ("Extensor Carpi", "Extends and abducts or adducts the wrist.", "Forearm") },
            { "tendon sheaths of upper limb", ("Upper Limb Tendon Sheaths", "Synovial sheaths surrounding tendons of the arm.", "Forearm") },
            { "pollicis", ("Pollicis Muscle/Tendon", "Muscle or tendon acting on the thumb.", "Hand") },
            { "manus", ("Hand Structure", "Muscle, tendon, or ligament of the hand.", "Hand") },
            { "palmar aponeurosis", ("Palmar Aponeurosis", "Fibrous sheet that protects tendons in the palm.", "Hand") },
            { "palmar interossei", ("Palmar Interossei", "Adduct the fingers toward the middle finger.", "Hand") },
            { "gemellus", ("Gemellus", "Deep hip muscle that externally rotates the thigh.", "Gluteal") },
            { "quadratus femoris", ("Quadratus Femoris", "Externally rotates the thigh at the hip.", "Gluteal") },
            { "fascia lata", ("Fascia Lata", "The deep fascia that encases the thigh muscles; anchors the IT band.", "Thigh") },
            { "femoral intermuscular septum", ("Intermuscular Septum", "Fibrous partition separating muscle compartments of the thigh.", "Thigh") },
            { "patellar retinaculum", ("Patellar Retinaculum", "Fibrous band that stabilizes the kneecap.", "Thigh") },
            { "muscles of lower limb", ("Lower Limb Muscles", "Muscles of the leg region.", "Thigh") },
            { "fasciae of lower limb", ("Lower Limb Fascia", "Connective tissue layer of the leg.", "Thigh") },
            { "crural", ("Crural Fascia", "Connective tissue sheath of the lower leg.", "Lower Leg") },
            { "calcaneal", ("Calcaneal Tendon", "The Achilles tendon; connects the calf muscles to the heel bone.", "Lower Leg") },
            { "popliteal", ("Popliteal Fascia", "Connective tissue behind the knee.", "Lower Leg") },
            { "retinaculum of ankle", ("Ankle Retinaculum", "Fibrous band that holds tendons in place as they cross the ankle.", "Lower Leg") },
            { "fibular retinaculum", ("Fibular Retinaculum", "Fibrous band that holds the peroneal tendons behind the ankle.", "Lower Leg") },
            { "tendon sheaths of lower limb", ("Lower Limb Tendon Sheaths", "Synovial sheaths surrounding tendons of the leg.", "Lower Leg") },
            { "of foot", ("Foot Muscle", "Intrinsic muscle or ligament of the foot.", "Foot") },
            { "muscles of foot", ("Foot Muscles", "Intrinsic muscles of the foot.", "Foot") },
            { "quadratus plantae", ("Quadratus Plantae", "A plantar muscle that assists the flexor digitorum longus in toe flexion.", "Foot") },
            { "tarsal tendon sheath", ("Tarsal Tendon Sheath", "Synovial sheath surrounding tendons crossing the ankle/foot bones.", "Foot") },
            { "tendon sheaths of toes", ("Toe Tendon Sheaths", "Synovial sheaths surrounding the toe flexor/extensor tendons.", "Foot") },
            { "coccygeus", ("Coccygeus", "Pelvic floor muscle that supports the pelvic organs.", "Pelvic Floor") },
            { "iliococcygeus", ("Iliococcygeus", "Part of levator ani; supports the pelvic floor.", "Pelvic Floor") },
            { "pubo", ("Pubococcygeus", "Part of levator ani; supports the pelvic floor and controls continence.", "Pelvic Floor") },
            { "perineal", ("Perineal Muscles", "Muscles of the pelvic floor supporting the pelvic organs.", "Pelvic Floor") },
            { "muscles of pelvis", ("Pelvic Muscles", "Muscles that support and move the pelvis.", "Pelvic Floor") },

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
    // IMPORTANT: pick the LONGEST (most specific) matching key, not just
    // the first one Dictionary happens to enumerate. Short generic keys
    // like "rectus", "oblique", "transverse" are meant as abdominal
    // shorthand, but those same words also appear in unrelated structures
    // elsewhere (e.g. the eye's "Superior rectus muscle", the hand's
    // "transverse metacarpal ligament"). Without this, whichever entry
    // the dictionary iterates to first silently wins, even if it's wrong.
    (string name, string desc, string cat)? FindMuscleData(string muscleName)
    {
        string lower = muscleName.ToLower();
        string bestKey = null;
        (string, string, string)? best = null;

        foreach (var kvp in muscleData)
        {
            if (!lower.Contains(kvp.Key)) continue;
            if (bestKey == null || kvp.Key.Length > bestKey.Length)
            {
                bestKey = kvp.Key;
                best = kvp.Value;
            }
        }
        return best;
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