using UnityEngine;

public class DisableDepthWriting : MonoBehaviour
{
    void Start()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000; // Transparent
            }
        }
    }
}
