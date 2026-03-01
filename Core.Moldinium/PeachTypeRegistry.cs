using AlphaBee.Layouts;
using Moldinium.Baking;
using System.Diagnostics;
using System.Reflection;

namespace AlphaBee;

public class PeachTypeRegistry
{
	Int32 nextTypeNo = 1;

	AbstractBakery bakery;

	public struct Entry
	{
		public Type peachType;
		public Int32 size;
	}

	List<Entry?> peachTypesByRef = new();

	Dictionary<Type, TypeRef> canonicalTypeRefs = new();

	public PeachTypeRegistry()
	{
		var configuration = BakeryConfiguration.Create(
			new PeachyPropertyImplementationProvider()
		) with
		{ MakeValue = true };

		bakery = configuration.CreateBakery("peachy");

	}

	void AddType(Type interfaceType, ITypeConfiguration typeConfiguration, Int32 size)
	{
		//new StructLayoutTypeConfiguration<LayoutT>()
		var implementationType = bakery.GetCreatedType(interfaceType, typeConfiguration);

		AddCanonicalType(interfaceType, implementationType, size);
	}

	public void AddType(Type interfaceType)
	{
		if (interfaceType.GetCustomAttribute<PeachLayoutAttribute>()?.LayoutType is Type layoutType)
		{
			var layout = StructLayoutTypeConfiguration.Create(layoutType);

			AddType(interfaceType, layout, layoutType.SizeOf());
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	void Set(TypeRef type, Type peachType, Int32 size)
	{
		Debug.Assert(!type.IsFundamental);

		while (peachTypesByRef.Count <= type.no)
		{
			peachTypesByRef.Add(default);
		}

		peachTypesByRef[type.no] = new Entry { peachType = peachType, size = size };
	}

	void AddCanonicalType(Type interfaceType, Type implementationType, Int32 size)
	{
		var typeRef = CreateTypeRef();
		Set(typeRef, implementationType, size);
		SetCanonicalTypeRef(interfaceType, typeRef);
	}

	void SetCanonicalTypeRef(Type interfaceType, TypeRef typeRef)
	{
		if (!canonicalTypeRefs.TryAdd(interfaceType, typeRef))
		{
			throw new InvalidOperationException($"Interface ${interfaceType} already has a canonical TypeRef");
		}
	}

	static Entry Throw(TypeRef type)
	{
		throw new ArgumentException($"Type {type} is unknown");
	}

	public TypeRef GetCanonicalTypeRef(Type interfaceType)
	{
		try
		{
			return canonicalTypeRefs[interfaceType];
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new ArgumentException($"Type {interfaceType} is unknown");
		}
	}

	public Entry Get(TypeRef type)
	{
		Debug.Assert(!type.IsFundamental);

		try
		{
			return peachTypesByRef[type.no] ?? Throw(type);
		}
		catch (ArgumentOutOfRangeException)
		{
			return Throw(type);
		}
	}

	TypeRef CreateTypeRef() => new TypeRef(nextTypeNo++);
}
