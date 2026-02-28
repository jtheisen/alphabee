using System.Diagnostics;

namespace AlphaBee;

public class ComplexTypeRegistry
{
	List<Type?> peachTypes;

	public ComplexTypeRegistry()
	{
		peachTypes = new List<Type?>();
	}

	public void Set(TypeRef type, Type peachType)
	{
		Debug.Assert(!type.IsFundamental);

		while (peachTypes.Count < type.no)
		{
			peachTypes.Add(null);
		}

		peachTypes[type.no] = peachType;
	}

	static Type Throw(TypeRef type)
	{
		throw new ArgumentException($"Type {type} is unknown");
	}

	public Type Get(TypeRef type)
	{
		Debug.Assert(!type.IsFundamental);

		try
		{
			return peachTypes[type.no] ?? Throw(type);
		}
		catch (ArgumentOutOfRangeException)
		{
			return Throw(type);
		}
	}
}
