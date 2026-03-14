using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

public class LayoutingConfiguration : ITypeConfiguration
{
	public static readonly LayoutingConfiguration Instance = new();

	public String? TypeSuffix => null;

	public Type? GetExtraTypeForProperty(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (Spanlikes.IsInlineSpanlike(property, out var extent))
		{
			return LayoutSpacerBakery.Intance.EnsureSpacerType(extent.Size, extent.UnitSize);
		}

		return null;
	}
}
