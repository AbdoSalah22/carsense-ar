using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ARTouch : MonoBehaviour
{
    private Color defaultColor = Color.white;
    private Color touchedColor = Color.red;

    public TMP_Text infoBox;

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
                    // Reset all DTC markers to defaultColor
                    GameObject[] allMarkers = GameObject.FindGameObjectsWithTag("DTCMarker");

                    foreach (GameObject marker in allMarkers)
                    {
                        Renderer rend = marker.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.material.color = defaultColor;
                        }
                    }

                    // Now highlight the selected one
                    Renderer hitRenderer = hit.transform.GetComponent<Renderer>();
                    if (hitRenderer != null)
                    {
                        hitRenderer.material.color = touchedColor;
                        Debug.Log("Selected and highlighted: " + hit.transform.name);
                        infoBox.text = hit.transform.name;
                    }
                }
            }
        }
    }
}
