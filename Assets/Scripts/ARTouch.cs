using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARTouch : MonoBehaviour
{
    // Reference your materials here
    public Material highlightMaterial;

    private GameObject selectedMarker;
    private DTCContainer selectedContainer;
    public TMP_Text infoBox;

    // Dictionary to store original materials for each renderer
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    // Keep track of the previously highlighted renderers
    private List<Renderer> highlightedRenderers = new List<Renderer>();

    public void OnNextDTCButtonPressed()
    {
        if (selectedContainer != null)
        {
            infoBox.text = selectedContainer.GetNextDTC();
        }
    }

    void Start()
    {
        if (highlightMaterial == null)
        {
            Debug.LogError("Highlight material not assigned in inspector!");
        }

        // Store original materials for all markers on start
        GameObject[] allMarkers = GameObject.FindGameObjectsWithTag("DTCMarker");
        foreach (GameObject marker in allMarkers)
        {
            StoreOriginalMaterials(marker);
        }
    }

    void StoreOriginalMaterials(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            // Only store if we haven't already
            if (!originalMaterials.ContainsKey(rend))
            {
                // Make deep copies of materials
                Material[] materialsCopy = new Material[rend.materials.Length];
                for (int i = 0; i < rend.materials.Length; i++)
                {
                    materialsCopy[i] = new Material(rend.materials[i]);
                }
                originalMaterials[rend] = materialsCopy;
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Touch detected.");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                Debug.Log("Hit object: " + hit.transform.name + " | Tag: " + hit.transform.tag);
                if (hit.transform.CompareTag("DTCMarker"))
                {
                    // First restore previously highlighted objects
                    RestorePreviousHighlighted();

                    // Get and store the renderers of the newly clicked object
                    Renderer[] hitRenderers = hit.transform.GetComponentsInChildren<Renderer>();
                    highlightedRenderers.Clear();

                    // Apply highlight material to newly selected object
                    foreach (Renderer rend in hitRenderers)
                    {
                        // Make sure original materials are stored
                        if (!originalMaterials.ContainsKey(rend))
                        {
                            StoreOriginalMaterials(hit.transform.gameObject);
                        }

                        // Track this renderer as highlighted
                        highlightedRenderers.Add(rend);

                        // Apply highlight material
                        Material[] newMaterials = new Material[rend.materials.Length];
                        for (int i = 0; i < rend.materials.Length; i++)
                        {
                            newMaterials[i] = highlightMaterial;
                        }
                        rend.materials = newMaterials;
                    }

                    // Set current selected
                    selectedMarker = hit.transform.gameObject;
                    selectedContainer = selectedMarker.GetComponent<DTCContainer>();
                    if (selectedContainer != null)
                    {
                        selectedContainer.ResetCycle();
                        infoBox.text = selectedContainer.GetCurrentDTC();
                    }
                }
            }
        }
    }

    void RestorePreviousHighlighted()
    {
        // Restore original materials only to previously highlighted renderers
        foreach (Renderer rend in highlightedRenderers)
        {
            if (originalMaterials.ContainsKey(rend))
            {
                // Restore original materials
                rend.materials = originalMaterials[rend];
            }
        }
    }

    // For cleanup
    void OnDestroy()
    {
        // Restore all materials to original state
        foreach (var entry in originalMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.materials = entry.Value;
            }
        }
    }
}