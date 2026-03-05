using System.Reflection;

namespace AlphaBee;


public class InterfaceRelationships
{
	public PropertyInfo? GetPropertyOfInterfaceTypeIncludingInherited(
		Type type, String name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
	{
		if (type.GetProperty(name, flags) is PropertyInfo ownProperty)
		{
			return ownProperty;
		}
		else
		{
			foreach (var interfaceType in type.GetInterfaces())
			{
				if (type.GetProperty(name, flags) is PropertyInfo property)
				{
					return property;
				}
			}
		}

		return null;
	}
}
