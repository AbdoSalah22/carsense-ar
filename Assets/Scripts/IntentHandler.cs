using TMPro;
using UnityEngine;

public class IntentHandler : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text infoBox2;

    void Start()
    {
        // Make sure this GameObject persists between scenes if needed
        DontDestroyOnLoad(gameObject);
    }

    public void OnDtcDataReceived(string dtcJsonString)
    {
        Debug.Log("Received DTC data: " + dtcJsonString);

        // Parse the JSON data
        DtcData[] dtcList = JsonHelper.FromJson<DtcData>(dtcJsonString);

        // Process your DTC data here
        foreach (var dtc in dtcList)
        {
            Debug.Log($"DTC Code: {dtc.code}, Explanation: {dtc.explanation}, Severity: {dtc.severity}");
        }

        // You can now use this data in your AR application
        infoBox2.text = dtcList[0].severity + dtcList[1].severity;
    }
}

[System.Serializable]
public class DtcData
{
    public string code;
    public string explanation;
    public string severity;
}
