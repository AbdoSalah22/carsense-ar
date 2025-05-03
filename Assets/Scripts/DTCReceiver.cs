using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DTCReceiver : MonoBehaviour
{
    [Serializable]
    public class DtcEntry
    {
        public string code;
        public string explanation;
        public string severity;
    }

    [Header("UI")]
    public TMP_Text infoBox2;

    void Start()
    {
        // Only run on Android
#if UNITY_ANDROID
        try
        {
            // Get the intent that started the Unity activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            // Retrieve the dtc_data extra
            string dtcJson = intent.Call<string>("getStringExtra", "dtc_data");

            if (!string.IsNullOrEmpty(dtcJson))
            {
                // Parse JSON into list of DtcEntry objects
                List<DtcEntry> dtcList = JsonUtilityExtensions.FromJsonList<DtcEntry>(dtcJson);
                infoBox2.text = dtcList[0].severity + dtcList[1].severity;

                // Use the data (e.g., log or display in AR)
                foreach (var dtc in dtcList)
                {
                    Debug.Log($"Code: {dtc.code}, Severity: {dtc.severity}");
                }
            }
            else
            {
                Debug.Log("No DTC data received.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving DTC data: {e.Message}");
        }
#endif
    }
}