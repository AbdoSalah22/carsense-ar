using System;
using System.Collections.Generic;
using UnityEngine;

public static class JsonUtilityExtensions
{
    [Serializable]
    private class Wrapper<T>
    {
        public List<T> Items;
    }

    public static List<T> FromJsonList<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }
}