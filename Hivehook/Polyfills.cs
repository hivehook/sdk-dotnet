#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for <c>System.Runtime.CompilerServices.IsExternalInit</c> on frameworks
/// (e.g. netstandard2.0) that predate C# 9's <c>init</c> accessor. Required so that
/// positional records compile against older TFMs.
/// </summary>
internal static class IsExternalInit { }
#endif
