using System.Collections.Generic;
using UnityEngine;

public class DTCContainer : MonoBehaviour
{
    public List<string> dtcs = new List<string>();
    private int currentIndex = 0;

    public string GetCurrentDTC()
    {
        if (dtcs.Count == 0) return "";
        return dtcs[currentIndex];
    }

    public string GetNextDTC()
    {
        if (dtcs.Count == 0) return "";

        currentIndex = (currentIndex + 1) % dtcs.Count;
        return dtcs[currentIndex];
    }

    public string GetPreviousDTC()
    {
        if (dtcs.Count == 0) return "";

        currentIndex = (currentIndex - 1 + dtcs.Count) % dtcs.Count;
        return dtcs[currentIndex];
    }

    public void ResetCycle()
    {
        currentIndex = 0;
    }

    public bool HasMultipleDTCs()
    {
        return dtcs != null && dtcs.Count > 1;
    }

}
