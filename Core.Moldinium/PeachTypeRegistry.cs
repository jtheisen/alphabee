using Moldinium.Baking;
using System.Diagnostics;
using System.Reflection;

using PeachTypeInfo = (System.Type interfaceType, AlphaBee.IPeachTypeConfiguration typeConfiguration);
using CanonicalInfo = (AlphaBee.TypeRef typeRef, System.Type implementationType);

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

	private readonly Dictionary<Type, TypeRef> typeRefsByImplementationType = new();

	private readonly Dictionary<IPeachTypeConfiguration, TypeRef> typeRefsByConfiguration = new();

	private readonly Dictionary<Type, CanonicalInfo> canonicalInfoByInterfaceType = new();

	//private readonly Dictionary<IPeachTypeConfiguration, Type> canonicalImplementationTypesByConfiguration = new();

	private readonly Dictionary<Type, List<PeachTypeInfo>> infosByImplementationType = new();

	public PeachTypeRegistry(IClrTypeResolver? clrTypeResolver = null)
	{
		peachBakery = CreateBakery<PeachPropertyImplementationProvider>("peaches");
		layoutBakery = CreateBakery<LayoutPropertyImplementationProvider>("layouts", prefixBackingFields: false);

		this.clrTypeResolver = clrTypeResolver ?? new ClrTypeResolver();
	}

	static AbstractBakery CreateBakery<ProviderT>(String name, Boolean prefixBackingFields = true)
		where ProviderT : PropertyImplementationProvider, new()
	{
		var provider = new ProviderT();

		var config = BakeryConfiguration.Create(provider) with { MakeValue = true, PrefixBackingFields = prefixBackingFields };

		var bakery = config.CreateBakery(name);

		return bakery;
	}

	public Int32 Count => nextTypeNo;

	public void Validate()
	{
		for (var i = 0; i < nextTypeNo; ++i)
		{
			var entry = peachTypesByRef[i];

			Trace.Assert(entry is not null);
			Trace.Assert(entry.TypeRef.no == i);

			Trace.Assert(entry.TypeRef.Equals(typeRefsByImplementationType[entry.ImplementationType]));

			var info = infosByImplementationType[entry.ImplementationType];

			
		}
	}

	public IEnumerable<PeachTypeInfo> GetTypesForInterfaceType(Type interfaceType)
	{
		return infosByImplementationType[interfaceType];
	}

	public Type AddStoredType(ITypeDescription description)
	{
		var configuration = PeachTypeConfiguration.Create(clrTypeResolver, description);

		return AddAlternativeType(configuration, new TypeRef(description.No));
	}

	public Type AddAlternativeType(PeachTypeConfiguration configuration, TypeRef? typeRefOrNull, Boolean allowNewImplementation = false)
	{
		if (!EnsureImplementationType(configuration, out var typeRef, out var implementationType, typeRefOrNull))
		{
			Trace.Assert(allowNewImplementation, "Duplicate type registration");
		}

		return implementationType;
	}

	public Type EnsureCanonicalImplementation(Type interfaceType)
	{
		EnsureCanonicalImplementation(interfaceType, out _, out var implementationType);

		return implementationType;
	}

	public void EnsureCanonicalImplementation(Type interfaceType, out TypeRef typeRef, out Type implementationType)
	{
		if (canonicalInfoByInterfaceType.TryGetValue(interfaceType, out var info))
		{
			typeRef = info.typeRef;
			implementationType = info.implementationType;
		}
		else
		{
			AddCanonicalType(interfaceType, out typeRef, out implementationType);
		}
	}

	void AddCanonicalType(Type interfaceType, out TypeRef typeRef, out Type implementationType)
	{
		Trace.Assert(!canonicalInfoByInterfaceType.ContainsKey(interfaceType), $"Type {interfaceType} already has a canonical implementation");

		var layoutType = GetCanonicalLayoutStructType(interfaceType);

		var configuration = PeachTypeConfiguration.Create(interfaceType, layoutType);

		if (typeRefsByConfiguration.TryGetValue(configuration, out typeRef))
		{
			var entry = GetEntry(typeRef);

			SetAsCanonical(interfaceType, typeRef);

			implementationType = entry.ImplementationType;
		}
		else
		{
			EnsureImplementationType(configuration, out typeRef, out implementationType);

			SetAsCanonical(interfaceType, typeRef);
		}
	}

	Boolean EnsureImplementationType(PeachTypeConfiguration typeConfiguration, out TypeRef typeRef, out Type implementationType, TypeRef? desiredTypeRef = null)
	{
		var interfaceType = typeConfiguration.InterfaceType;

		Trace.Assert(interfaceType.IsInterface);

		if (typeRefsByConfiguration.TryGetValue(typeConfiguration, out typeRef))
		{
			Trace.Assert(desiredTypeRef?.Equals(typeRef) ?? true, $"Trying to ensure type {desiredTypeRef}, the same layout already exists under {typeRef}");

			implementationType = GetEntry(typeRef).ImplementationType;

			return false;
		}
		else
		{
			typeRef = desiredTypeRef ?? CreateTypeRef();

			implementationType = peachBakery.Resolve(interfaceType, (typeConfiguration, typeRef.no));

			if (!infosByImplementationType.TryGetValue(typeConfiguration.InterfaceType, out var list))
			{
				list = infosByImplementationType[typeConfiguration.InterfaceType] = new List<PeachTypeInfo>();
			}

			list.Add((implementationType, typeConfiguration));

			SetEntry(new Entry(typeRef, typeConfiguration, implementationType));

			return true;
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
			return layoutBakery.Resolve(interfaceType);
		}
	}

	void Throw(Entry entry)
	{
		throw new Exception($"Implemenation type {entry.ImplementationType} was already set");
	}

	void SetEntry(Entry entry)
	{
		Debug.Assert(!entry.TypeRef.IsFundamental);

		if (!typeRefsByImplementationType.TryAdd(entry.ImplementationType, entry.TypeRef))
		{
			Throw(entry);
		}

		if (!typeRefsByConfiguration.TryAdd(entry.Configuration, entry.TypeRef))
		{
			Throw(entry);
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

	public Entry GetEntry(TypeRef typeRef)
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

	void SetAsCanonical(Type interfaceType, TypeRef typeRef)
	{
		var entry = GetEntry(typeRef);

		if (!canonicalInfoByInterfaceType.TryAdd(interfaceType, (typeRef, entry.ImplementationType)))
		{
			throw new InvalidOperationException($"Interface ${interfaceType} already has a canonical TypeRef");
		}

		entry.InterfaceType = interfaceType;
	}

	public CanonicalInfo LookupCanonical(Type type)
	{
		try
		{
			return canonicalInfoByInterfaceType[type];
		}
		catch (ArgumentOutOfRangeException)
		{
			throw new ArgumentException($"Type {type} is unknown");
		}
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
			var info = LookupCanonical(type);
			typeRef = info.typeRef;
			var entry = GetEntry(info.typeRef);
			clrType = entry.InterfaceType ?? throw new Exception($"The type {type} is not canonical after all");
		}
	}

	TypeRef CreateTypeRef() => new TypeRef(nextTypeNo++);

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

		var kvps = configuration.Properties.dict.ToArray();

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
}
