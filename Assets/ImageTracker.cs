using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTracker : MonoBehaviour
{
    private ARTrackedImageManager trackedImages;

    public GameObject spherePrefab; // assign a sphere prefab in inspector

    // Map DTC names to Vector3 offsets (approximate positions relative to the license plate)
    private Dictionary<string, Vector3> dtcOffsets = new Dictionary<string, Vector3>()
    {
        { "Motor", new Vector3(0.0f, 0.2f, 0.4f) },          // Forward and slightly above
        { "Steering", new Vector3(0.3f, 0.3f, 0.8f) },    // Left-forward and up
        { "Exhaust", new Vector3(-0.4f, 0.0f, 1.8f) }        // Behind the car
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
            // Simulated DTCs (multiple Motor DTCs)
            Dictionary<string, List<string>> groupedDTCs = new Dictionary<string, List<string>>()
        {
            { "Motor", new List<string> { "Motor Overheat", "Motor Low Oil", "Motor Vibration" } },
            { "Steering", new List<string> { "Steering Sensor Fault" } },
            { "Exhaust", new List<string> { "Exhaust Leak" } }
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


    // Optional: Update logic for moving objects if needed
}
