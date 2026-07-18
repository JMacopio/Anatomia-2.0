using UnityEngine;

public class StructureInfo : MonoBehaviour
{
    [Header("Bone Information")]
    public string structureName;
    [TextArea(3, 6)]
    public string description;
    public string category;  // e.g. "Skull", "Thorax", "Upper Limb"

}
