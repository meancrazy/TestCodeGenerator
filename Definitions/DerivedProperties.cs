using System;

namespace Definitions;

public class DerivedProperties
{
	public DateTime UtcNow => DateTime.UtcNow;
	public bool HasUtcNow => true;
}
