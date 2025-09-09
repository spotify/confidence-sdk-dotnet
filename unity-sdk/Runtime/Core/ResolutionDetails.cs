namespace UnityOpenFeature.Core
{
    public class ResolutionDetails<T>
    {
        public T Value { get; set; }
        public string FlagKey { get; set; }
        public Reason Reason { get; set; } = Reason.Static;
        public string Variant { get; set; }
        public ErrorCode ErrorCode { get; set; } = ErrorCode.None;
        public string ErrorMessage { get; set; }

        public ResolutionDetails(T value, string flagKey) { Value = value; FlagKey = flagKey; }

        public static ResolutionDetails<T> Error(string flagKey, T defaultValue, ErrorCode errorCode, string errorMessage)
        {
            return new ResolutionDetails<T>(defaultValue, flagKey) { Reason = Reason.Error, ErrorCode = errorCode, ErrorMessage = errorMessage };
        }
    }
}

