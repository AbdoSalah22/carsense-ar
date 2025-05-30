using System.Collections.Generic;

[System.Serializable]
public class DTCData
{
    public string code;
    public string explanation;
    public string zone;
}

[System.Serializable]
public class DTCDataList
{
    public List<DTCData> dtcs;
}
