using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARTouch : MonoBehaviour
{
    private Color defaultColor = Color.white;
    private Color touchedColor = Color.red;

    private GameObject selectedMarker;
    private DTCContainer selectedContainer;

    public TMP_Text infoBox;

    public void OnNextDTCButtonPressed()
    {
        if (selectedContainer != null)
        {
            infoBox.text = selectedContainer.GetNextDTC();
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
                    // Reset colors
                    GameObject[] allMarkers = GameObject.FindGameObjectsWithTag("DTCMarker");
                    foreach (GameObject marker in allMarkers)
                    {
                        Renderer rend = marker.GetComponent<Renderer>();
                        if (rend != null)
                            rend.material.color = defaultColor;
                    }

                    // Highlight selected
                    Renderer hitRenderer = hit.transform.GetComponent<Renderer>();
                    if (hitRenderer != null)
                        hitRenderer.material.color = touchedColor;

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
}
