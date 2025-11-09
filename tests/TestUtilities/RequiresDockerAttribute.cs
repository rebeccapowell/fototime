using System;
using System.Collections.Generic;
using Xunit.v3;

namespace FotoTime.Tests.Utilities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDockerAttribute : Attribute, ITraitAttribute
{
    private static readonly Lazy<bool> DockerIsAvailable = new(CheckDockerAvailability);

    public static bool IsSupported => DockerIsAvailable.Value;

    public string? Reason { get; init; }

    public RequiresDockerAttribute(string? reason = null)
    {
        Reason = reason;
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => GetTraitValues();

    internal static IReadOnlyCollection<KeyValuePair<string, string>> GetTraitValues()
    {
        if (!IsSupported)
        {
            return new[] { new KeyValuePair<string, string>(XunitConstants.Category, XunitConstants.Failing) };
        }

        return Array.Empty<KeyValuePair<string, string>>();
    }

    private static bool CheckDockerAvailability()
    {
        if (PlatformDetection.IsRunningOnCI && !OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info --format '{{json .ServerVersion}}'",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit((int)TimeSpan.FromSeconds(5).TotalMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore cleanup failures.
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
