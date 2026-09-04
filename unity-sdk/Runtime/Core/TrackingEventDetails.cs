using System.Collections.Generic;

namespace UnityOpenFeature.Core
{
    public class TrackingEventDetails
    {
        public double? Value { get; set; }
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();
    }
}
