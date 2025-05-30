//using TMPro;
//using UnityEngine;

//public class IntentHandler : MonoBehaviour
//{
//    [Header("UI")]
//    public TMP_Text infoBox2;

//    public ARSceneController arSceneController;

//    public static string LatestDTCJson = null;

//    void Start()
//    {
//        // Make sure this GameObject persists between scenes if needed
//        DontDestroyOnLoad(gameObject);
//    }

//    public void OnDtcDataReceived(string dtcJsonString)
//    {
//        Debug.Log("Received DTC data from Flutter: " + dtcJsonString);
//        LatestDTCJson = dtcJsonString;

//        if (arSceneController != null)
//        {
//            arSceneController.ReceiveDTCDataAndSpawn(dtcJsonString);
//        }
//        else
//        {
//            Debug.LogWarning("ARSceneController is not assigned in IntentHandler!");
//        }
//        infoBox2.text = LatestDTCJson;

//        // Parse the JSON data
//        DTCData[] dtcList = JsonHelper.FromJson<DTCData>(LatestDTCJson);
//    }
//}

