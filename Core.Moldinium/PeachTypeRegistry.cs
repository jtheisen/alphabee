using AlphaBee.Utilities;
using Moldinium.Baking;
using Moldinium.Utilities;
using System.Diagnostics;
using System.Reflection;
using CanonicalInfo = (AlphaBee.TypeRef typeRef, System.Type implementationType);
using PeachTypeInfo = (System.Type implementationType, AlphaBee.PeachTypeConfiguration typeConfiguration);

namespace AlphaBee;

public class PeachTypeRegistry
{
	Int32 nextTypeNo = 1;

	AbstractBakery peachBakery, layoutBakery;

	public record Entry(TypeRef TypeRef, PeachTypeConfiguration Configuration, Type ImplementationType)
	{
		public PeachTypeLayout Layout => Configuration.Layout;

		public Type? InterfaceType { get; set; }
	}

	private readonly IClrTypeResolver clrTypeResolver;

	private readonly List<Entry?> peachTypesByRef = new();

	private readonly Dictionary<Type, TypeRef> typeRefsByImplementationType = new();

	private readonly Dictionary<PeachTypeLayout, TypeRef> typeRefsByLayout = new();

	private readonly Dictionary<Type, CanonicalInfo> canonicalInfoByInterfaceType = new();

	private readonly Dictionary<Type, List<PeachTypeInfo>> infosByInterfaceType = new();

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
		for (var i = 1; i < nextTypeNo; ++i)
		{
			var entry = peachTypesByRef[i];

			Trace.Assert(entry is not null);
			Trace.Assert(entry.TypeRef.no == i);

			Trace.Assert(entry.TypeRef.Equals(typeRefsByImplementationType[entry.ImplementationType]));

			Trace.Assert(typeRefsByLayout[entry.Layout].Equals(entry.TypeRef));

			if (entry.InterfaceType is Type interfaceType)
			{
				var infos = infosByInterfaceType[entry.InterfaceType];

				var singleMatchingInfo = infos.Single(i => i.typeConfiguration.Layout.Equals(entry.Layout));

				Trace.Assert(singleMatchingInfo.implementationType == entry.ImplementationType);
				Trace.Assert(singleMatchingInfo.typeConfiguration == entry.Configuration);
			}

			var peach = entry.ImplementationType.CreateInstance<IPeach>();

			Trace.Assert(peach.ImplementationTypeRef.Equals(entry.TypeRef));

			if (entry.InterfaceType is Type canonicalInterfaceType)
			{
				var (typeRef2, implementationType2) = canonicalInfoByInterfaceType[canonicalInterfaceType];

				Trace.Assert(typeRef2.Equals(entry.TypeRef));
				Trace.Assert(implementationType2 == entry.ImplementationType);
			}
		}

		foreach (var p in typeRefsByImplementationType)
		{
			var entry = GetEntry(p.Value);

			Trace.Assert(entry.TypeRef.Equals(p.Value));
			Trace.Assert(entry.ImplementationType.Equals(p.Key));
		}

		foreach (var p in typeRefsByLayout)
		{
			var entry = GetEntry(p.Value);

			Trace.Assert(p.Key.Equals(entry.Layout));
		}

		foreach (var p in canonicalInfoByInterfaceType)
		{
			var (interfaceType, (typeRef, implementationType)) = p;

			var entry = GetEntry(typeRef);

			Trace.Assert(interfaceType == entry.InterfaceType);
			Trace.Assert(implementationType == entry.ImplementationType);
		}
	}

	public IEnumerable<PeachTypeInfo> GetTypesForInterfaceType(Type interfaceType)
	{
		return infosByInterfaceType[interfaceType];
	}

	public Type AddStoredType(ITypeDescription description)
	{
		var configuration = PeachTypeLayout.Create(clrTypeResolver, description);

		return AddAlternativeType(configuration, new TypeRef(description.No));
	}

	public Type AddAlternativeType(PeachTypeLayout configuration, TypeRef? typeRefOrNull, Boolean allowNewImplementation = false)
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

		var configuration = PeachTypeLayout.Create(interfaceType, layoutType);

		if (typeRefsByLayout.TryGetValue(configuration, out typeRef))
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

	Boolean EnsureImplementationType(PeachTypeLayout typeLayout, out TypeRef typeRef, out Type implementationType, TypeRef? desiredTypeRef = null)
	{
		var interfaceType = typeLayout.InterfaceType;

		Trace.Assert(interfaceType.IsInterface);

		if (typeRefsByLayout.TryGetValue(typeLayout, out typeRef))
		{
			Trace.Assert(desiredTypeRef?.Equals(typeRef) ?? true, $"Trying to ensure type {desiredTypeRef}, the same layout already exists under {typeRef}");

			implementationType = GetEntry(typeRef).ImplementationType;

			return false;
		}
		else
		{
			typeRef = desiredTypeRef ?? CreateTypeRef();

			var configuration = typeLayout.ToConfiguration(typeRef);

			implementationType = peachBakery.Resolve(interfaceType, configuration);

			if (!infosByInterfaceType.TryGetValue(interfaceType, out var list))
			{
				list = infosByInterfaceType[interfaceType] = new List<PeachTypeInfo>();
			}

			list.Add((implementationType, configuration));

			SetEntry(new Entry(typeRef, configuration, implementationType));

			Debug.Assert(implementationType.Name.EndsWith(typeRef.no.ToString()), $"Created type {implementationType}'s name doesn't say it's number for TypeRef {typeRef}");

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

		if (!typeRefsByLayout.TryAdd(entry.Layout, entry.TypeRef))
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

		Trace.Assert(typeRef.no > 0);

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

				WriteTypeDescription(target, entry.Layout, i);
			}
		}
	}

	public void WriteTypeDescription(ITypeDescription target, PeachTypeLayout configuration, Int32 no)
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
