using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARTouch : MonoBehaviour
{
    // Reference your materials here
    public Material highlightMaterial;
    public GameObject arInfoPanelPrefab; // Add this reference in Inspector

    private GameObject selectedMarker;
    private DTCContainer selectedContainer;
    private GameObject currentInfoPanel; // Track the current AR panel
    private Camera arCamera;

    // Dictionary to store original materials for each renderer
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    // Keep track of the previously highlighted renderers
    private List<Renderer> highlightedRenderers = new List<Renderer>();

    void Start()
    {
        arCamera = Camera.main;

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

    public void OnNextDTCButtonPressed()
    {
        if (selectedContainer != null && currentInfoPanel != null)
        {
            string dtcText = selectedContainer.GetNextDTC();
            UpdatePanelText(dtcText);
        }
    }

    public void OnPreviousDTCButtonPressed()
    {
        if (selectedContainer != null && currentInfoPanel != null)
        {
            string dtcText = selectedContainer.GetPreviousDTC();
            UpdatePanelText(dtcText);
        }
    }

    public void OnClosePanelButtonPressed()
    {
        if (currentInfoPanel != null)
        {
            Destroy(currentInfoPanel);
            currentInfoPanel = null;
            selectedMarker = null;
            selectedContainer = null;
        }
    }


    void UpdatePanelText(string text)
    {
        if (currentInfoPanel != null)
        {
            TMP_Text panelText = currentInfoPanel.GetComponentInChildren<TMP_Text>();
            if (panelText != null)
            {
                panelText.text = text;
            }
        }
    }

    void StoreOriginalMaterials(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (!originalMaterials.ContainsKey(rend))
            {
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
            Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            int layerMask = ~(1 << LayerMask.NameToLayer("UI"));
            if (Physics.Raycast(ray, out hit, 100, layerMask))
            {
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
                        if (!originalMaterials.ContainsKey(rend))
                        {
                            StoreOriginalMaterials(hit.transform.gameObject);
                        }

                        highlightedRenderers.Add(rend);

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
                        ShowARInfoPanel(selectedMarker.transform, selectedContainer.GetCurrentDTC());
                    }
                }
            }
        }

        // Update panel position to face camera if it exists
        if (currentInfoPanel != null && selectedMarker != null)
        {
            UpdatePanelPosition();
        }
    }

    void ShowARInfoPanel(Transform markerTransform, string initialText)
    {
        // Destroy existing panel if any
        if (currentInfoPanel != null)
        {
            Destroy(currentInfoPanel);
        }

        if (arInfoPanelPrefab != null)
        {
            // Position panel slightly above and behind the marker relative to camera
            Vector3 panelOffset = markerTransform.up * 0.2f +
                                -markerTransform.forward * 0.1f;

            currentInfoPanel = Instantiate(
                arInfoPanelPrefab,
                markerTransform.position + panelOffset,
                Quaternion.identity
            );

            // Set the panel as child of the marker so it moves with it
            currentInfoPanel.transform.SetParent(markerTransform);

            // Initialize text
            UpdatePanelText(initialText);

            // Make panel face camera initially
            UpdatePanelPosition();
        }

        // Initialize text
        UpdatePanelText(initialText);

        Button[] buttons = currentInfoPanel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn.name == "NextButton")  // Adjust names as per your prefab
            {
                bool hasMultiple = selectedContainer != null && selectedContainer.HasMultipleDTCs();
                btn.gameObject.SetActive(hasMultiple);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextDTCButtonPressed);
            }
            else if (btn.name == "PreviousButton") // New part
            {
                bool hasMultiple = selectedContainer != null && selectedContainer.HasMultipleDTCs();
                btn.gameObject.SetActive(hasMultiple);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPreviousDTCButtonPressed);
            }
            else if (btn.name == "CloseButton")  // Add this
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClosePanelButtonPressed);
            }
        }


    }

    void UpdatePanelPosition()
    {
        if (currentInfoPanel == null || arCamera == null) return;

        // Make panel fully face the camera in 3D space
        currentInfoPanel.transform.LookAt(arCamera.transform.position);

        // Flip the panel 180 degrees to face the camera properly
        currentInfoPanel.transform.rotation *= Quaternion.Euler(0, 180f, 0);
    }


    void RestorePreviousHighlighted()
    {
        List<Renderer> toRemove = new List<Renderer>();

        foreach (Renderer rend in highlightedRenderers)
        {
            if (rend == null)
            {
                // The renderer has been destroyed, remove from originalMaterials to clean up
                toRemove.Add(rend);
                continue;
            }

            if (originalMaterials.ContainsKey(rend))
            {
                try
                {
                    rend.materials = originalMaterials[rend];
                }
                catch (MissingReferenceException)
                {
                    toRemove.Add(rend);
                }
            }
        }

        // Remove any destroyed renderers from the dictionary
        foreach (Renderer r in toRemove)
        {
            originalMaterials.Remove(r);
        }

        highlightedRenderers.Clear();

        if (currentInfoPanel != null)
        {
            Destroy(currentInfoPanel);
        }
    }


    void OnDestroy()
    {
        foreach (var entry in originalMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.materials = entry.Value;
            }
        }
    }
}