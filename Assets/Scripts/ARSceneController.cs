using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARSceneController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public ARCameraManager arCameraManager;

    [Header("Prefabs")]
    public GameObject placementPrefab;
    public GameObject defaultPrefab;
    public GameObject zoneLabelPrefab;
    public GameObject MotorPrefab, SteeringPrefab, ExhaustPrefab, BrakesPrefab, TransmissionPrefab, SuspensionPrefab, CoolingPrefab, BatteryPrefab;

    [Header("Car Model")]
    public GameObject transparentCarPrefab;
    private GameObject spawnedCar = null;

    [Header("UI")]
    public TMP_Text infoBox;
    public GameObject guideFramePanel;

    [Header("Plane Materials")]
    public Material horizontalPlaneMaterial;
    public Material verticalPlaneMaterial;

    private GameObject placementAnchor;
    private bool hasSpawnedParts = false;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private Dictionary<string, Vector3> dtcOffsets = new Dictionary<string, Vector3>()
    {
        { "Motor", new Vector3(0.1f, 0.4f, 0.4f) },
        { "Steering", new Vector3(0.3f, 0.6f, 1.1f) },
        { "Exhaust", new Vector3(-0.4f, 0.2f, 2.6f) },
        { "Brakes", new Vector3(0.4f, 0.15f, 0.6f) },
        { "Transmission", new Vector3(-0.1f, 0.35f, 0.5f) },
        { "Suspension", new Vector3(0.25f, 0.25f, 0.6f) },
        { "Cooling", new Vector3(0.0f, 0.45f, 0.2f) },
        { "Battery", new Vector3(-0.2f, 0.45f, 0.3f) },
        { "Other", new Vector3(0.0f, 0.35f, -0.15f) }
    };

    private Dictionary<string, List<DTCData>> groupedDTCs = new Dictionary<string, List<DTCData>>();

    void Start()
    {
        LoadDTCData();
        infoBox.text = "Tap on the highligted area to start";

        planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDestroy()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            float horizontalness = Mathf.Abs(Vector3.Dot(plane.normal, Vector3.up));
            bool isHorizontal = horizontalness > 0.7f;

            // Disable vertical planes, keep horizontal
            if (!isHorizontal)
            {
                plane.gameObject.SetActive(false);
            }
            else
            {
                UpdatePlaneMaterial(plane);
            }

        }

        foreach (var plane in args.updated)
        {
            // Only process vertical planes
            float horizontalness = Mathf.Abs(Vector3.Dot(plane.normal, Vector3.up));
            if (horizontalness <= 0.7f)
            {
                UpdatePlaneMaterial(plane);
            }
        }
    }

    private void UpdatePlaneMaterial(ARPlane plane)
    {
        // Calculate how horizontal the plane is (1 = completely horizontal, 0 = vertical)
        float horizontalness = Mathf.Abs(Vector3.Dot(plane.normal, Vector3.up));

        // Use a threshold to determine if it's horizontal or vertical
        bool isHorizontal = horizontalness > 0.7f;

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        if (renderer != null && horizontalPlaneMaterial != null && verticalPlaneMaterial != null)
        {
            // Apply the appropriate material
            renderer.sharedMaterial = isHorizontal ?
                horizontalPlaneMaterial :
                verticalPlaneMaterial;
        }
    }

    void Update()
    {
        if (hasSpawnedParts) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 screenPos = Input.mousePosition;
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 screenPos = Input.GetTouch(0).position;
#endif
            if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                Pose hitPose = hits[0].pose;

                // Place anchor once
                if (placementAnchor == null)
                {
                    placementAnchor = Instantiate(placementPrefab, hitPose.position, hitPose.rotation);
                }

                // Add this line to instantiate the car at the same location
                if (transparentCarPrefab != null)
                {
                    Vector3 direction = hitPose.position - Camera.main.transform.position;
                    direction.y = 0;
                    direction.Normalize();
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    spawnedCar = Instantiate(transparentCarPrefab, hitPose.position, lookRotation);
                    spawnedCar.transform.localScale = Vector3.one;
                }

                // Spawn the DTC models
                SpawnDTCObjects(hitPose.position);

                // Disable plane visuals
                foreach (ARPlane plane in planeManager.trackables)
                {
                    plane.gameObject.SetActive(false);
                }

                planeManager.enabled = false;
                infoBox.text = "DTC markers placed!";
                guideFramePanel.SetActive(false);
                hasSpawnedParts = true;
            }
        }
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

    GameObject GetPrefabForZone(string zone)
    {
        switch (zone)
        {
            case "Motor": return MotorPrefab ?? defaultPrefab;
            case "Steering": return SteeringPrefab ?? defaultPrefab;
            case "Exhaust": return ExhaustPrefab ?? defaultPrefab;
            case "Brakes": return BrakesPrefab ?? defaultPrefab;
            case "Transmission": return TransmissionPrefab ?? defaultPrefab;
            case "Suspension": return SuspensionPrefab ?? defaultPrefab;
            case "Cooling": return CoolingPrefab ?? defaultPrefab;
            case "Battery": return BatteryPrefab ?? defaultPrefab;
            case "Other": return defaultPrefab;
            default: return defaultPrefab;
        }
    }

    void SpawnDTCObjects(Vector3 basePos)
    {
        // Compute direction user was facing when tapping
        Vector3 direction = (basePos - Camera.main.transform.position);
        direction.y = 0; // ignore vertical tilt for stable horizontal orientation
        direction.Normalize();
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        foreach (var kvp in groupedDTCs)
        {
            string zone = kvp.Key;
            List<DTCData> dtcList = kvp.Value;

            if (dtcOffsets.TryGetValue(zone, out Vector3 offset))
            {
                Vector3 rotatedOffset = lookRotation * offset;
                Vector3 worldPos = basePos + rotatedOffset;
                GameObject prefab = GetPrefabForZone(zone);
                GameObject dtcObj = Instantiate(prefab, worldPos, lookRotation * prefab.transform.rotation);
                dtcObj.name = zone;
                dtcObj.tag = "DTCMarker";

                var container = dtcObj.AddComponent<DTCContainer>();
                foreach (var dtc in dtcList)
                    container.dtcs.Add($"{dtc.code}\n{dtc.description}");

                GameObject label = Instantiate(zoneLabelPrefab, worldPos + new Vector3(0, 0.1f, 0), Quaternion.identity);
                label.GetComponent<TextMeshPro>().text = zone;
            }
        }
    }

    public void ResetScene()
    {
        // Destroy placement anchor and all spawned objects
        if (placementAnchor != null)
        {
            Destroy(placementAnchor);
            placementAnchor = null;
        }

        // Destroy all DTC markers and zone labels
        GameObject[] dtcMarkers = GameObject.FindGameObjectsWithTag("DTCMarker");
        foreach (GameObject marker in dtcMarkers)
        {
            Destroy(marker);
        }

        GameObject[] zoneLabels = GameObject.FindGameObjectsWithTag("ZoneLabel");
        foreach (GameObject label in zoneLabels)
        {
            Destroy(label);
        }

        if (spawnedCar != null)
        {
            Destroy(spawnedCar);
            spawnedCar = null;
        }

        planeManager.enabled = true;

        foreach (var plane in planeManager.trackables)
        {
            float horizontalness = Mathf.Abs(Vector3.Dot(plane.normal, Vector3.up));
            bool isHorizontal = horizontalness > 0.7f;

            if (isHorizontal)
            {
                plane.gameObject.SetActive(true);

                var collider = plane.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = true;
            }
            else
            {
                plane.gameObject.SetActive(false);
            }
        }

        hasSpawnedParts = false;
        infoBox.text = "Tap on the highlighted area to start again";
        guideFramePanel.SetActive(true);
    }
}
