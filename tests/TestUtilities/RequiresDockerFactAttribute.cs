using System;
using System.Collections.Generic;
using Xunit;
using Xunit.v3;

namespace FotoTime.Tests.Utilities;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresDockerFactAttribute : FactAttribute, ITraitAttribute
{
    public RequiresDockerFactAttribute(string? reason = null)
    {
        if (!RequiresDockerAttribute.IsSupported)
        {
            Skip = reason ?? "Docker is required to run this test.";
        }
    }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => RequiresDockerAttribute.GetTraitValues();
}
