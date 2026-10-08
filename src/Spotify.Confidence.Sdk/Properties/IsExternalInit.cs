#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

// Required by the compiler for records on .NET Standard 2.0.
[SuppressMessage("Major Code Smell", "S2094:Classes should not be empty", Justification = "Compiler marker for record support")]
internal static class IsExternalInit
{
}
#endif
