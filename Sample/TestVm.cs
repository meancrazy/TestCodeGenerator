using Definitions;

namespace Sample;

[HasDerivedProperty(nameof(DerivedProperties.UtcNow))]
[HasDerivedProperty(nameof(DerivedProperties.HasUtcNow))]
public partial class TestVm
{
}
