using TMPro;
using UnityEngine;

public class IntentReceiver : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text infoBox2;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GetIntentData();
    }

    void GetIntentData()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
            {
                if (intent.Call<bool>("hasExtra", "dtc_data"))
                {
                    string dtcJson = intent.Call<string>("getStringExtra", "dtc_data");
                    infoBox2.text = dtcJson;
                    Debug.Log("Received DTC data: " + dtcJson);
                    ProcessDtcData(dtcJson);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to get intent data: " + e.Message);
        }
#endif
    }

    void ProcessDtcData(string jsonData)
    {
        // Parse JSON (same as before)
        DtcData[] dtcList = JsonUtility.FromJson<DtcData[]>(jsonData);
        foreach (var dtc in dtcList)
        {
            Debug.Log($"DTC: {dtc.code}, Severity: {dtc.severity}");
        }
    }

    [System.Serializable]
    public class DtcData
    {
        public string code;
        public string explanation;
        public string severity;
    }
}