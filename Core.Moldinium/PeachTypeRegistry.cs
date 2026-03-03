using Moldinium.Baking;
using System.Diagnostics;
using System.Reflection;

namespace AlphaBee;

public class PeachTypeRegistry
{
	Int32 nextTypeNo = 1;

	AbstractBakery peachBakery, layoutBakery;

	public struct Entry
	{
		public Type peachType;
		public Int32 size;
	}

	private readonly IClrTypeResolver clrTypeResolver;

	private readonly List<Entry?> peachTypesByRef = new();

	private readonly Dictionary<Type, TypeRef> canonicalTypeRefs = new();

	public PeachTypeRegistry(IClrTypeResolver? clrTypeResolver = null)
	{
		var implProvider = new PeachPropertyImplementationProvider();

		var peachBakeryConfiguration
			= BakeryConfiguration.Create(implProvider) with { MakeValue = true };

		peachBakery = peachBakeryConfiguration.CreateBakery("peaches");

		var layoutBakeryConfiguration
			= BakeryConfiguration.Create() with { MakeValue = true };

		layoutBakery = layoutBakeryConfiguration.CreateBakery("layouts");
		this.clrTypeResolver = clrTypeResolver ?? new ClrTypeResolver();
	}

	public void AddType(ITypeDescription description)
	{
		var typeConfiguration = PeachTypeConfiguration.Create(clrTypeResolver, description);

		AddType(typeConfiguration);
	}

	void AddType(IPeachTypeConfiguration typeConfiguration)
	{
		var interfaceType = typeConfiguration.ClrType;

		Trace.Assert(interfaceType.IsInterface);

		//new StructLayoutTypeConfiguration<LayoutT>()
		var implementationType = peachBakery.GetCreatedType(interfaceType, typeConfiguration);

		AddCanonicalType(interfaceType, implementationType, typeConfiguration.Size);
	}

	public void AddType(Type interfaceType)
	{
		var layoutType = GetCanonicalLayoutStructType(interfaceType);

		var layout = PeachTypeConfiguration.Create(interfaceType, layoutType);

		AddType(layout);
	}

	Type GetCanonicalLayoutStructType(Type interfaceType)
	{
		if (interfaceType.GetCustomAttribute<PeachLayoutAttribute>()?.LayoutType is Type layoutType)
		{
			return layoutType;
		}
		else
		{
			return layoutBakery.GetCreatedType(interfaceType);
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
