using Moldinium.Baking;
using System.Diagnostics;
using System.Reflection;

namespace AlphaBee;

public class PeachTypeRegistry
{
	Int32 nextTypeNo = 1;

	AbstractBakery peachBakery, layoutBakery;

	public readonly struct Entry
	{
		public readonly TypeRef typeRef;
		public readonly Type implementationType;
		public readonly Type interfaceType;
		public readonly Int32 size;

		public Entry(TypeRef typeRef, Type implementationType, Type interfaceType, Int32 size)
		{
			this.typeRef = typeRef;
			this.implementationType = implementationType;
			this.interfaceType = interfaceType;
			this.size = size;
		}
	}

	private readonly IClrTypeResolver clrTypeResolver;

	private readonly List<Entry?> peachTypesByRef = new();

	private readonly Dictionary<Type, TypeRef> canonicalTypeRefs = new();

	private readonly Dictionary<IPeachTypeConfiguration, Type> peachTypesByConfiguration = new();

	private readonly Dictionary<Type, List<IPeachTypeConfiguration>> peachTypeConfigurationsByType = new();

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

	public Type AddType(ITypeDescription description)
	{
		var typeConfiguration = PeachTypeConfiguration.Create(clrTypeResolver, description);

		var implementationType = GetImplementationType(typeConfiguration);

		return implementationType;
	}

	public void AddType(Type interfaceType)
	{
		var layoutType = GetCanonicalLayoutStructType(interfaceType);

		var configuration = PeachTypeConfiguration.Create(interfaceType, layoutType);

		var implementationType = GetImplementationType(configuration);

		AddCanonicalType(interfaceType, implementationType, configuration.Size);
	}

	Type GetImplementationType(IPeachTypeConfiguration typeConfiguration)
	{
		if (peachTypesByConfiguration.TryGetValue(typeConfiguration, out var peachType))
		{
			return peachType;
		}
		else
		{
			var interfaceType = typeConfiguration.ClrType;

			Trace.Assert(interfaceType.IsInterface);

			var implementationType = peachBakery.GetCreatedType(interfaceType, typeConfiguration);

			if (!peachTypeConfigurationsByType.TryGetValue(typeConfiguration.ClrType, out var list))
			{
				list = peachTypeConfigurationsByType[typeConfiguration.ClrType] = new List<IPeachTypeConfiguration>();
			}

			list.Add(typeConfiguration);

			return peachTypesByConfiguration[typeConfiguration] = implementationType;
		}
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

	void Set(in Entry entry)
	{
		Debug.Assert(!entry.typeRef.IsFundamental);

		while (peachTypesByRef.Count <= entry.typeRef.no)
		{
			peachTypesByRef.Add(default);
		}

		peachTypesByRef[entry.typeRef.no] = entry;
	}

	void AddCanonicalType(Type interfaceType, Type implementationType, Int32 size)
	{
		var typeRef = CreateTypeRef();
		Set(new Entry( typeRef, implementationType, size);
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
