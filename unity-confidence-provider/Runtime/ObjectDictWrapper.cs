using System;
using System.Collections.Generic;
using UnityEngine;

// Wrapper class for Dictionary<string, object> serialization
[Serializable]
public class ObjectDictWrapper
{
    public List<string> keys;
    public List<string> values; // Serialized as JSON strings

    public ObjectDictWrapper(Dictionary<string, object> dict)
    {
        keys = new List<string>(dict.Count);
        values = new List<string>(dict.Count);

        foreach (var kv in dict)
        {
            keys.Add(kv.Key);
            values.Add(JsonUtility.ToJson(kv.Value)); // Serialize value to JSON string
        }
    }

    public Dictionary<string, object> ToDictionary(Type valueType)
    {
        var dict = new Dictionary<string, object>(keys.Count);
        for (int i = 0; i < keys.Count; i++)
        {
            dict[keys[i]] = JsonUtility.FromJson(values[i], valueType);
        }
        return dict;
    }
}
