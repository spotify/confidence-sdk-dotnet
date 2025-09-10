namespace UnityOpenFeature.Core
{
    public class ResolutionDetails<T>
    {
        public T Value { get; set; }
        public string FlagKey { get; set; }
        public Reason Reason { get; set; } = UnityOpenFeature.Core.Reason.RESOLVE_REASON_UNSPECIFIED;
        public string Variant { get; set; }
        public ErrorCode ErrorCode { get; set; } = ErrorCode.None;
        public string ErrorMessage { get; set; }

        public ResolutionDetails(T value, string flagKey) { Value = value; FlagKey = flagKey; }

        public static ResolutionDetails<T> Error(string flagKey, T defaultValue, ErrorCode errorCode, string errorMessage)
        {
            return new ResolutionDetails<T>(defaultValue, flagKey) { Reason = UnityOpenFeature.Core.Reason.ERROR, ErrorCode = errorCode, ErrorMessage = errorMessage };
        }
    }
}

