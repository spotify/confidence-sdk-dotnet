using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityOpenFeature.Core
{
    [Serializable]
    public class EvaluationContext
    {
        [SerializeField] private string targetingKey = "";
        [SerializeField] public List<ContextAttribute> attributes = new List<ContextAttribute>();

        public string TargetingKey { get => targetingKey; set => targetingKey = value ?? ""; }

        public EvaluationContext() { }
        public EvaluationContext(string targetingKey) { this.targetingKey = targetingKey ?? ""; }

        public EvaluationContext SetAttribute(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return this;
            var existing = attributes.FindIndex(a => a.Key == key);
            var attr = new ContextAttribute { Key = key, Value = value?.ToString() ?? "" };
            if (existing >= 0) attributes[existing] = attr; else attributes.Add(attr);
            return this;
        }

        public T GetAttribute<T>(string key, T defaultValue = default)
        {
            var attr = attributes.Find(a => a.Key == key);
            if (attr == null) return defaultValue;
            try
            {
                if (typeof(T) == typeof(string)) return (T)(object)attr.Value;
                if (typeof(T) == typeof(int)) return (T)(object)int.Parse(attr.Value);
                if (typeof(T) == typeof(float)) return (T)(object)float.Parse(attr.Value);
                if (typeof(T) == typeof(bool)) return (T)(object)bool.Parse(attr.Value);
                return defaultValue; // Complex object deserialization not supported
            }
            catch { return defaultValue; }
        }

        [Serializable] public class ContextAttribute { public string Key; public string Value; }
    }
}

