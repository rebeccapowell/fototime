using System.Collections.Generic;
using Xunit.v3;

namespace FotoTime.Tests.Utilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDockerAttribute : Attribute, ITraitAttribute
{
    public static bool IsSupported => OperatingSystem.IsLinux() || !PlatformDetection.IsRunningOnCI;

    public string? Reason { get; init; }

    public RequiresDockerAttribute(string? reason = null)
    {
        Reason = reason;
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        if (!IsSupported)
        {
            return new[] { new KeyValuePair<string, string>(XunitConstants.Category, XunitConstants.Failing) };
        }

        return Array.Empty<KeyValuePair<string, string>>();
    }
}
