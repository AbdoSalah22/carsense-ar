using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ImageTracker : MonoBehaviour
{
    private ARTrackedImageManager trackedImages;

    public ARLicensePlateDetector licensePlateDetector;

    public GameObject spherePrefab;

    private Dictionary<string, Vector3> dtcOffsets = new Dictionary<string, Vector3>()
    {
        { "Motor", new Vector3(0.0f, 0.2f, 0.4f) },
        { "Steering", new Vector3(0.3f, 0.3f, 0.8f) },
        { "Exhaust", new Vector3(-0.4f, 0.0f, 1.8f) },
        { "Brakes", new Vector3(0.2f, -0.1f, 0.6f) },
        { "Transmission", new Vector3(-0.3f, 0.15f, 0.5f) },
        { "Suspension", new Vector3(0.1f, -0.2f, 1.5f) },
        { "Cooling", new Vector3(0.0f, 0.25f, 0.2f) },
        { "Battery", new Vector3(-0.2f, 0.1f, 0.3f) }
    };

    private Dictionary<string, List<DTCData>> groupedDTCs = new Dictionary<string, List<DTCData>>();

    void Awake()
    {
        trackedImages = GetComponent<ARTrackedImageManager>();
        LoadDTCData();
    }

    void OnEnable()
    {
        trackedImages.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImages.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void LoadDTCData()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("dtcs");
        DTCData[] dtcs = JsonHelper.FromJson<DTCData>(jsonText.text);

        foreach (var dtc in dtcs)
        {
            if (!groupedDTCs.ContainsKey(dtc.zone))
                groupedDTCs[dtc.zone] = new List<DTCData>();

            groupedDTCs[dtc.zone].Add(dtc);
        }
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            foreach (var kvp in groupedDTCs)
            {
                string dtcZone = kvp.Key;
                List<DTCData> dtcList = kvp.Value;

                if (dtcOffsets.TryGetValue(dtcZone, out Vector3 offset))
                {
                    Vector3 worldPos = trackedImage.transform.TransformPoint(offset);
                    GameObject dtcSphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                    dtcSphere.name = dtcZone;
                    dtcSphere.transform.SetParent(trackedImage.transform);
                    dtcSphere.tag = "DTCMarker";

                    // Add component and data
                    DTCContainer container = dtcSphere.AddComponent<DTCContainer>();
                    foreach (var dtc in dtcList)
                        container.dtcs.Add($"{dtc.code} - {dtc.description}");
                }
            }
        }
    }
}
