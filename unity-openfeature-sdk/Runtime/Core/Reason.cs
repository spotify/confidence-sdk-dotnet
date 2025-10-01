using System;

namespace UnityOpenFeature.Core
{
    [Serializable]
    public enum Reason
    {
        RESOLVE_REASON_UNSPECIFIED = 0,

        // The flag was successfully resolved because one rule matched.
        RESOLVE_REASON_MATCH = 1,

        RESOLVE_REASON_STALE = 2,

        // The flag could not be resolved because no rule matched.
        RESOLVE_REASON_NO_SEGMENT_MATCH = 3,

        // The flag could not be resolved because the matching rule had no variant
        // that could be assigned.
        RESOLVE_REASON_NO_TREATMENT_MATCH = 4,

        // the flag could not be resolved because the targeting key
        // is invalid
        RESOLVE_REASON_TARGETING_KEY_ERROR = 5,

        // The flag could not be resolved because it was archived.
        RESOLVE_REASON_FLAG_ARCHIVED = 6,

        DEFAULT = 7,

        ERROR = 8
    }
}

