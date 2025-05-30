using TMPro;
using UnityEngine;

public class IntentReceiver : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text infoBox2;

    public static string ReceivedDtcJson = null;


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
                    ReceivedDtcJson = dtcJson;
                    infoBox2.text = dtcJson;
                    Debug.Log("Received DTC data: " + dtcJson);
                    //ProcessDtcData(dtcJson);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to get intent data: " + e.Message);
        }
#endif
    }

    //void ProcessDtcData(string jsonData)
    //{
    //    // Optionally log
    //    infoBox2.text = "Received DTCs. Parsing...";

    //    // Try to find the ARSceneController and call the load method
    //    ARSceneController controller = FindObjectOfType<ARSceneController>();
    //    if (controller != null)
    //    {
    //        controller.LoadDTCDataFromJson(jsonData);
    //        Debug.Log("DTC data passed to ARSceneController.");
    //    }
    //    else
    //    {
    //        Debug.LogError("ARSceneController not found in scene.");
    //    }
    //}
}