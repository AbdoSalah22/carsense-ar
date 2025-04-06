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
            // Simulate fetched DTCs (in a real app, fetch them from API or DB)
            List<string> detectedDTCs = new List<string> { "Motor", "Steering", "Exhaust" };

            foreach (var dtc in detectedDTCs)
            {
                if (dtcOffsets.TryGetValue(dtc, out Vector3 offset))
                {
                    // Convert offset from local space to world space relative to the image
                    Vector3 worldPos = trackedImage.transform.TransformPoint(offset);

                    GameObject dtcSphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                    dtcSphere.name = dtc;

                    // Optionally parent it to tracked image so it moves with it
                    dtcSphere.transform.SetParent(trackedImage.transform);
                }
            }
        }

        // Optional: Update logic for moving objects if needed
    }
}
