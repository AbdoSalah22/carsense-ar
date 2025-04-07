using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracker : MonoBehaviour
{
    private ARTrackedImageManager trackedImages;

    public GameObject spherePrefab;

    // Map DTC categories to Vector3 offsets (relative positions around the license plate)
    private Dictionary<string, Vector3> dtcOffsets = new Dictionary<string, Vector3>()
    {
        { "Motor", new Vector3(0.0f, 0.2f, 0.4f) },          // Forward and slightly above
        { "Steering", new Vector3(0.3f, 0.3f, 0.8f) },       // Left-forward and up
        { "Exhaust", new Vector3(-0.4f, 0.0f, 1.8f) },       // Behind the car
        { "Brakes", new Vector3(0.2f, -0.1f, 0.6f) },        // Right and slightly below
        { "Transmission", new Vector3(-0.3f, 0.15f, 0.5f) }, // Left and mid height
        { "Suspension", new Vector3(0.1f, -0.2f, 1.5f) },    // Near rear wheel area
        { "Cooling", new Vector3(0.0f, 0.25f, 0.2f) },       // Near the radiator
        { "Battery", new Vector3(-0.2f, 0.1f, 0.3f) }        // Near front-left
    };

    void Awake()
    {
        trackedImages = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        trackedImages.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImages.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            // Simulated DTCs with real-like codes
            Dictionary<string, List<string>> groupedDTCs = new Dictionary<string, List<string>>()
            {
                { "Motor", new List<string> { "P0301", "P0219", "P1326" } },
                { "Steering", new List<string> { "C1A00" } },
                { "Exhaust", new List<string> { "P0420" } },
                { "Brakes", new List<string> { "C1234", "C0040" } },
                { "Transmission", new List<string> { "P0700", "P0730" } },
                { "Suspension", new List<string> { "C1111", "C1122" } },
                { "Cooling", new List<string> { "P0117", "P0128" } },
                { "Battery", new List<string> { "B1000", "P0562" } }
            };

            foreach (var kvp in groupedDTCs)
            {
                string dtcCategory = kvp.Key;
                List<string> dtcList = kvp.Value;

                if (dtcOffsets.TryGetValue(dtcCategory, out Vector3 offset))
                {
                    Vector3 worldPos = trackedImage.transform.TransformPoint(offset);

                    GameObject dtcSphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                    dtcSphere.name = dtcCategory;
                    dtcSphere.transform.SetParent(trackedImage.transform);
                    dtcSphere.tag = "DTCMarker";

                    DTCContainer container = dtcSphere.AddComponent<DTCContainer>();
                    container.dtcs.AddRange(dtcList);
                }
            }
        }
    }
}
