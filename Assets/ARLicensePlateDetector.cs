using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Sentis;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Unity.Mathematics;

public class ARLicensePlateDetector : MonoBehaviour
{
    [Header("AR Components")]
    public ARCameraManager arCameraManager;
    //[SerializeField] private Camera arCamera;
    //public ARRaycastManager arRaycastManager;
    //public XROrigin arSessionOrigin;
    public GameObject spherePrefab; // Assign your sphere prefab in inspector


    [Header("Detection Settings")]
    public ModelAsset modelAsset;
    [Range(0, 1)] public float confidenceThreshold = 0.5f;
    public float sphereSpawnDistance = 0.8f; // Distance in front of license plate

    private IWorker m_Worker;
    private Model m_RuntimeModel;
    private Texture2D m_CameraTexture;
    private GameObject m_CurrentSphere;
    private List<Detection> m_CurrentDetections = new List<Detection>();

    private bool detectionPaused = false;


    //public bool IsTrackingLicensePlate => m_CurrentSphere != null;
    //public GameObject CurrentTrackedObject => m_CurrentSphere;
    //public void PauseDetection() => detectionPaused = true;
    //public void ResumeDetection() => detectionPaused = false;

    //// Add these variables at the top of your class
    //[Header("Debug")]
    //public RawImage cameraFeedDisplay;
    //public bool showDetectionRects = true;
    //public Color detectionRectColor = Color.green;
    //public float detectionRectDuration = 0.1f;

    public class Detection
    {
        public float Confidence { get; set; }
        public Rect Rectangle { get; set; }
    }

    // Add these DTC-related variables at the top
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


    void Start()
    {
        // Load the model
        m_RuntimeModel = ModelLoader.Load(modelAsset);
        m_Worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, m_RuntimeModel);

        // Set up AR camera callback
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived;
        }

        LoadDTCData();
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

    // Add this new method to spawn DTC spheres
    private void SpawnDTCSpheres(Vector3 originPosition)
    {
        foreach (var kvp in groupedDTCs)
        {
            string dtcZone = kvp.Key;
            List<DTCData> dtcList = kvp.Value;

            if (dtcOffsets.TryGetValue(dtcZone, out Vector3 offset))
            {
                Vector3 worldPos = originPosition + offset;
                GameObject dtcSphere = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                dtcSphere.name = dtcZone;
                dtcSphere.tag = "DTCMarker";

                // Add component and data
                DTCContainer container = dtcSphere.AddComponent<DTCContainer>();
                foreach (var dtc in dtcList)
                    container.dtcs.Add($"{dtc.code} - {dtc.description}");
            }
        }
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (detectionPaused) return;

        // Get the camera image
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        // Convert to Texture2D
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        if (m_CameraTexture == null || m_CameraTexture.width != image.width || m_CameraTexture.height != image.height)
        {
            m_CameraTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        }

        //if (cameraFeedDisplay != null)
        //{
        //    cameraFeedDisplay.texture = m_CameraTexture;
        //}

        image.Convert(conversionParams, m_CameraTexture.GetRawTextureData<byte>());
        m_CameraTexture.Apply();
        image.Dispose();

        // Process the image
        ProcessCameraImage(m_CameraTexture);
    }

    void ProcessCameraImage(Texture2D texture)
    {
        // Create a resized texture for model input (640x640)
        Texture2D resizedTexture = new Texture2D(640, 640, TextureFormat.RGBA32, false);

        // Use Unity's built-in scaling functionality
        RenderTexture rt = RenderTexture.GetTemporary(640, 640);
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        resizedTexture.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
        resizedTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        // Now correctly normalize the pixels from the resized texture
        Color[] pixels = resizedTexture.GetPixels();
        float[] normalizedPixels = new float[pixels.Length * 3];

        for (int i = 0; i < pixels.Length; i++)
        {
            // Normalize to 0-1 range and arrange as BGR (what YOLO expects)
            normalizedPixels[i * 3] = pixels[i].b;
            normalizedPixels[i * 3 + 1] = pixels[i].g;
            normalizedPixels[i * 3 + 2] = pixels[i].r;
        }

        // Create the input tensor with the normalized pixels
        using TensorFloat inputTensor = new TensorFloat(new TensorShape(1, 3, 640, 640), normalizedPixels);

        // Execute the model
        m_Worker.Execute(inputTensor);

        // Get output
        using TensorFloat outputTensor = m_Worker.PeekOutput() as TensorFloat;
        outputTensor.MakeReadable();

        // Process detections
        m_CurrentDetections = new List<Detection>(ProcessOutput(outputTensor));

        // Handle AR visualization
        UpdateARVisualization();

        //// Display resized texture for debugging if needed
        //if (cameraFeedDisplay != null)
        //{
        //    // Optional: To see exactly what the model sees
        //    cameraFeedDisplay.texture = resizedTexture;
        //}
    }


    private Detection[] ProcessOutput(TensorFloat output)
    {
        float[] outputData = output.ToReadOnlyArray();
        int numBoxes = 25200;
        int dimensions = 6; // [x, y, w, h, conf, class] for your model

        var detections = new List<Detection>();

        for (int i = 0; i < numBoxes; i++)
        {
            int baseIndex = i * dimensions;
            if (baseIndex + 4 >= outputData.Length) continue;

            float confidence = outputData[baseIndex + 4];

            // Add debug output to check confidence values
            if (confidence > 0.1f)
            {
                Debug.Log($"Found confidence: {confidence}");
            }

            if (confidence < confidenceThreshold) continue;

            // These coordinates are normalized to 0-1 in the 640x640 space
            float x = outputData[baseIndex];
            float y = outputData[baseIndex + 1];
            float width = outputData[baseIndex + 2];
            float height = outputData[baseIndex + 3];

            // Scale back to camera texture dimensions
            float scaledX = x * m_CameraTexture.width;
            float scaledY = y * m_CameraTexture.height;
            float scaledWidth = width * m_CameraTexture.width;
            float scaledHeight = height * m_CameraTexture.height;

            detections.Add(new Detection
            {
                Confidence = confidence,
                Rectangle = new Rect(scaledX - scaledWidth / 2, scaledY - scaledHeight / 2, scaledWidth, scaledHeight)
            });

            Debug.Log($"Detection at ({scaledX}, {scaledY}) with size ({scaledWidth}, {scaledHeight}) and confidence {confidence}");
        }

        return NonMaximumSuppression(detections.ToArray(), 0.5f);
    }

    private void UpdateARVisualization()
    {
        if (m_CurrentDetections.Count == 0)
        {
            if (m_CurrentSphere != null)
            {
                Destroy(m_CurrentSphere);
                m_CurrentSphere = null;
            }
            return;
        }

        // Get the most confident detection
        Detection bestDetection = m_CurrentDetections[0];
        for (int i = 1; i < m_CurrentDetections.Count; i++)
        {
            if (m_CurrentDetections[i].Confidence > bestDetection.Confidence)
            {
                bestDetection = m_CurrentDetections[i];
            }
        }

        // Calculate spawn position in AR space
        Vector2 plateCenter = bestDetection.Rectangle.center;
        Vector3 spawnPosition = CalculateARPosition(plateCenter);

        // Create or update sphere
        if (m_CurrentSphere == null)
        {
            //m_CurrentSphere = Instantiate(spherePrefab, spawnPosition, Quaternion.identity);
            SpawnDTCSpheres(spawnPosition);
            detectionPaused = true;
            Debug.Log("LETS GO!! YOLO PAUSED.");
            m_Worker?.Dispose();
        }
        else
        {
            m_CurrentSphere.transform.position = spawnPosition;
        }
    }

    public Vector3 CalculateARPosition(Vector2 screenPosition)
    {
        // Fallback: spawn in front of the camera
        Debug.Log("Raycast not working. Using fallback position.");
        //return arCamera.transform.position + arCamera.transform.forward * sphereSpawnDistance;
        return arCameraManager.transform.position + arCameraManager.transform.forward * sphereSpawnDistance;
    }


    private Detection[] NonMaximumSuppression(Detection[] detections, float iouThreshold)
    {
        // Sort by confidence (descending)
        var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();
        var selectedDetections = new System.Collections.Generic.List<Detection>();

        while (sortedDetections.Count > 0)
        {
            // Take the highest confidence detection
            var current = sortedDetections[0];
            selectedDetections.Add(current);
            sortedDetections.RemoveAt(0);

            // Remove overlapping detections
            for (int i = sortedDetections.Count - 1; i >= 0; i--)
            {
                if (CalculateIOU(current.Rectangle, sortedDetections[i].Rectangle) > iouThreshold)
                {
                    sortedDetections.RemoveAt(i);
                }
            }
        }

        return selectedDetections.ToArray();
    }

    private float CalculateIOU(Rect a, Rect b)
    {
        // Calculate intersection over union
        float intersectionArea = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
                               Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

        float unionArea = a.width * a.height + b.width * b.height - intersectionArea;
        return intersectionArea / unionArea;
    }

    void OnDestroy()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
        m_Worker?.Dispose();
    }
}