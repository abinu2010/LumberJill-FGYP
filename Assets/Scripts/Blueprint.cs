using UnityEngine;

[CreateAssetMenu(menuName = "Production/Blueprint", fileName = "Blueprint_")]
public class Blueprint : ScriptableObject
{
    [TextArea(4, 20)]
    public string asciiLayout;
}
