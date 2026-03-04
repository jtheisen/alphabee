using Moldinium.Baking;
using System.Diagnostics;
using System.Reflection;

using PeachTypeInfo = (System.Type, AlphaBee.IPeachTypeConfiguration);

namespace AlphaBee;

public class PeachTypeRegistry
{
	Int32 nextTypeNo = 1;

	AbstractBakery peachBakery, layoutBakery;

	public record Entry(TypeRef TypeRef, PeachTypeConfiguration Configuration, Type ImplementationType)
	{
		public Type? InterfaceType { get; set; }
	}

	private readonly IClrTypeResolver clrTypeResolver;

	private readonly List<Entry?> peachTypesByRef = new();

	private readonly Dictionary<Type, TypeRef> canonicalTypeRefs = new();

	private readonly Dictionary<Type, TypeRef> typeRefsByImplementationType = new();

	private readonly Dictionary<IPeachTypeConfiguration, Type> canonicalImplementationTypesByConfiguration = new();

	private readonly Dictionary<Type, List<PeachTypeInfo>> infosByImplementationType = new();

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

	public Int32 Count => nextTypeNo;

	public IEnumerable<PeachTypeInfo> GetTypesForInterfaceType(Type interfaceType)
	{
		return infosByImplementationType[interfaceType];
	}

	public Type AddStoredType(ITypeDescription description)
	{
		var configuration = PeachTypeConfiguration.Create(clrTypeResolver, description);

		var isNewImplementationType = EnsureImplementationType(configuration, out var implementationType);

		Trace.Assert(!isNewImplementationType, "Duplicate type registration");

		var typeRef = new TypeRef(description.No);
		Set(new Entry(typeRef, configuration, implementationType));

		return implementationType;
	}

	public Type GetCanonicalImplementationType(Type interfaceType)
	{
		EnsureCanonicalImplementation(interfaceType, out _, out var implementationType);

		return implementationType;
	}

	public void EnsureCanonicalImplementation(Type interfaceType, out TypeRef typeRef, out Type implementationType)
	{
		if (canonicalTypeRefs.TryGetValue(interfaceType, out typeRef))
		{
			implementationType = Get(typeRef).ImplementationType;
		}
		else
		{
			AddType(interfaceType, out typeRef, out implementationType);
		}
	}

	public void WriteAllTypeDescriptions(Object?[] targets, ref Boolean didWrite)
	{
		var n = peachTypesByRef.Count;

		Trace.Assert(n == targets.Length);

		for (var i = 0; i < n; i++)
		{
			var entry = peachTypesByRef[i];

			if (entry is null) continue;

			var target = targets[i] as ITypeDescription;

			Trace.Assert(target is not null);

			if (target.No != i)
			{
				didWrite = true;

				WriteTypeDescription(target, entry.Configuration, i);
			}
		}
	}

	public void WriteTypeDescription(ITypeDescription target, PeachTypeConfiguration configuration, Int32 no)
	{
		target.No = no;

		target.ClrName = clrTypeResolver.GetFqTypeName(configuration.InterfaceType);
		target.Size = configuration.Size;

		var kvps = configuration.Fields.dict.ToArray();

		var n = kvps.Length;

		var descriptionEntries = new TypeDescriptionEntry[n];

		for (var i = 0; i < n; ++i)
		{
			var (property, entry) = kvps[i];

			var type = property.PropertyType;

			LookupClrType(type, out var typeRef, out var clrType);

			descriptionEntries[i] = new TypeDescriptionEntry
			{
				ClrName = clrTypeResolver.GetFqTypeName(clrType),
				Offset = entry.offset,
				Size = entry.size,
				TypeRef = typeRef
			};
		}

		target.Properties = descriptionEntries;
	}

	void AddType(Type interfaceType, out TypeRef typeRef, out Type implementationType)
	{
		var layoutType = GetCanonicalLayoutStructType(interfaceType);

		var configuration = PeachTypeConfiguration.Create(interfaceType, layoutType);

		var isNewImplementationType = EnsureImplementationType(configuration, out implementationType);

		if (isNewImplementationType)
		{
			typeRef = CreateTypeRef();
			Set(new Entry(typeRef, configuration, implementationType));
		}
		else if (!typeRefsByImplementationType.TryGetValue(implementationType, out typeRef))
		{
			throw new Exception($"Can't find TypeRef for implementation type {implementationType}");
		}

		SetCanonicalTypeRef(interfaceType, typeRef);
	}

	Boolean EnsureImplementationType(IPeachTypeConfiguration typeConfiguration, out Type implementationType)
	{
		if (!canonicalImplementationTypesByConfiguration.TryGetValue(typeConfiguration, out implementationType!))
		{
			var interfaceType = typeConfiguration.InterfaceType;

			Trace.Assert(interfaceType.IsInterface);

			implementationType = peachBakery.GetCreatedType(interfaceType, typeConfiguration);

			if (!infosByImplementationType.TryGetValue(typeConfiguration.InterfaceType, out var list))
			{
				list = infosByImplementationType[typeConfiguration.InterfaceType] = new List<PeachTypeInfo>();
			}

			list.Add((implementationType, typeConfiguration));

			canonicalImplementationTypesByConfiguration[typeConfiguration] = implementationType;

			return true;
		}

		return false;
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
		Debug.Assert(!entry.TypeRef.IsFundamental);

		if (!typeRefsByImplementationType.TryAdd(entry.ImplementationType, entry.TypeRef))
		{
			throw new Exception($"Implemenation type {entry.ImplementationType} was already added");
		}

		while (peachTypesByRef.Count <= entry.TypeRef.no)
		{
			peachTypesByRef.Add(default);
		}

		var no = entry.TypeRef.no;

		if (peachTypesByRef[no] is not null)
		{
			throw new Exception($"Type no {no} is already registered");
		}

		peachTypesByRef[no] = entry;
	}

	public Entry Get(TypeRef typeRef)
	{
		Trace.Assert(!typeRef.IsFundamental);

		try
		{
			return peachTypesByRef[typeRef.no] ?? Throw(typeRef);
		}
		catch (ArgumentOutOfRangeException)
		{
			return Throw(typeRef);
		}
	}

	void SetCanonicalTypeRef(Type interfaceType, TypeRef typeRef)
	{
		if (!canonicalTypeRefs.TryAdd(interfaceType, typeRef))
		{
			throw new InvalidOperationException($"Interface ${interfaceType} already has a canonical TypeRef");
		}

		var entry = Get(typeRef);
		entry.InterfaceType = interfaceType;
	}

	static Entry Throw(TypeRef type)
	{
		throw new ArgumentException($"Type {type} is unknown");
	}

	public void LookupClrType(Type type, out TypeRef typeRef, out Type clrType)
	{
		if (ObjectTypeKinds.GetHandlerOrNull(type) is IObjectTypeHandler handler)
		{
			typeRef = handler.TypeRef;
			clrType = type;
		}
		else
		{
			typeRef = GetCanonicalTypeRef(type);
			var entry = Get(typeRef);
			clrType = entry.InterfaceType ?? throw new Exception($"The type {type} is not canonical after all");
		}
	}

	public TypeRef GetCanonicalTypeRef(Type type)
	{
		try
		{
			return canonicalTypeRefs[type];
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new ArgumentException($"Type {type} is unknown");
		}
	}

	TypeRef CreateTypeRef() => new TypeRef(nextTypeNo++);
}
