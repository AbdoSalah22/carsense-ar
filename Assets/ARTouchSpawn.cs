using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections.Generic;

public class ARTouchSpawn : MonoBehaviour
{
    [Header("AR Components")]
    public ARCameraManager arCameraManager;
    public GameObject spherePrefab;
    public GameObject zoneLabelPrefab;

    [Header("Spawn Settings")]
    public float sphereSpawnDistance = 0.5f;
    public TMP_Text infoBox;

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
    private bool hasSpawned = false;

    void Start()
    {
        LoadDTCData();
        infoBox.text = "Tap screen to place DTC markers.";
    }

    void Update()
    {
        if (hasSpawned) return;

        //if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 originPosition = arCameraManager.transform.position + arCameraManager.transform.forward * sphereSpawnDistance;
            SpawnDTCSpheres(originPosition);
            infoBox.text = "DTC markers placed!";
            hasSpawned = true;
        }
    }

    private void LoadDTCData()
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

    private void SpawnDTCSpheres(Vector3 originPosition)
    {
        foreach (var kvp in groupedDTCs)
        {
            string zone = kvp.Key;
            List<DTCData> dtcList = kvp.Value;

            if (dtcOffsets.TryGetValue(zone, out Vector3 offset))
            {
                Vector3 worldPos = originPosition + offset;
                GameObject dtcSphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                dtcSphere.name = zone;
                dtcSphere.tag = "DTCMarker";

                DTCContainer container = dtcSphere.AddComponent<DTCContainer>();
                foreach (var dtc in dtcList)
                    container.dtcs.Add($"{dtc.code} - {dtc.description}");
                GameObject label = Instantiate(zoneLabelPrefab, worldPos + new Vector3(0, 0.05f, 0), Quaternion.identity);
                label.GetComponent<TextMeshPro>().text = zone;
            }
        }
    }
}
