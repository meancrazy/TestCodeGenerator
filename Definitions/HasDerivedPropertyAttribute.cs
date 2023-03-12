using System;

namespace Definitions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class HasDerivedPropertyAttribute : Attribute
{
	public HasDerivedPropertyAttribute(string propertyName)
	{
	}
}
