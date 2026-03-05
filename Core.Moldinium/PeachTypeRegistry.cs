using AlphaBee.Utilities;
using Moldinium.Baking;
using Moldinium.Utilities;
using System.Diagnostics;
using System.Reflection;
using CanonicalInfo = (AlphaBee.TypeNo typeNo, System.Type implementationType);
using PeachTypeInfo = (System.Type implementationType, AlphaBee.PeachTypeConfiguration typeConfiguration);

namespace AlphaBee;

public class PeachTypeRegistry : IPropNoResolver
{
	Int32 nextTypeNo = 1, nextPropNo = 1;

	AbstractBakery peachBakery, layoutBakery;

	public record Entry(TypeNo TypeNo, PeachTypeConfiguration Configuration, Type InterfaceType, Type ImplementationType)
	{
		public PeachTypeLayout Layout => Configuration.Layout;
	}

	private readonly IClrTypeResolver clrTypeResolver;

	private readonly List<Entry?> peachTypesByNo = new();

	private readonly Dictionary<PropertyInfo, PropNo> propNosByPropertyInfo = new();

	private readonly Dictionary<Type, TypeNo> typeNosByImplementationType = new();

	private readonly Dictionary<PeachTypeLayout, TypeNo> typeNosByLayout = new();

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
			var entry = peachTypesByNo[i];

			Trace.Assert(entry is not null);

			var (typeNo, configuration, interfaceType, implementationType) = entry;

			Trace.Assert(typeNo.no == i);

			Trace.Assert(typeNo.Equals(typeNosByImplementationType[entry.ImplementationType]));

			Trace.Assert(typeNosByLayout[configuration.Layout].Equals(entry.TypeNo));

			var infos = infosByInterfaceType[interfaceType];

			var singleMatchingInfo = infos.Single(i => i.typeConfiguration.Layout.Equals(configuration.Layout));

			Trace.Assert(singleMatchingInfo.implementationType == implementationType);
			Trace.Assert(singleMatchingInfo.typeConfiguration == configuration);

			var peach = implementationType.CreateInstance<IPeach>();

			Trace.Assert(peach.ImplementationTypeNo.Equals(typeNo));
		}

		foreach (var p in typeNosByImplementationType)
		{
			var entry = GetEntry(p.Value);

			Trace.Assert(entry.TypeNo.Equals(p.Value));
			Trace.Assert(entry.ImplementationType.Equals(p.Key));
		}

		foreach (var p in typeNosByLayout)
		{
			var entry = GetEntry(p.Value);

			Trace.Assert(p.Key.Equals(entry.Layout));
		}

		foreach (var p in canonicalInfoByInterfaceType)
		{
			var (interfaceType, (typeNo, implementationType)) = p;

			var entry = GetEntry(typeNo);

			Trace.Assert(interfaceType == entry.InterfaceType);
			Trace.Assert(implementationType == entry.ImplementationType);
		}
	}

	public IEnumerable<PeachTypeInfo> GetTypesForInterfaceType(Type interfaceType)
	{
		return infosByInterfaceType[interfaceType];
	}

	public Type AddAlternativeType(PeachTypeLayout configuration, TypeNo? typeNoOrNull, Boolean allowNewImplementation = false)
	{
		if (!EnsureImplementationType(configuration, out var typeNo, out var implementationType, typeNoOrNull))
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

	public void EnsureCanonicalImplementation(Type interfaceType, out TypeNo typeNo, out Type implementationType)
	{
		if (canonicalInfoByInterfaceType.TryGetValue(interfaceType, out var info))
		{
			typeNo = info.typeNo;
			implementationType = info.implementationType;
		}
		else if (infosByInterfaceType.TryGetValue(interfaceType, out var list) && list.Count > 0)
		{
			var first = list[0];

			typeNo = typeNosByImplementationType[first.implementationType];
			implementationType = first.implementationType;

			SetAsCanonical(interfaceType, typeNo);
		}
		else
		{
			CreateCanonicalType(interfaceType, out typeNo, out implementationType);
		}
	}

	public PropNo GetPropNo(PropertyInfo propertyInfo)
	{
		return propNosByPropertyInfo[propertyInfo];
	}

	void CreateCanonicalType(Type interfaceType, out TypeNo typeNo, out Type implementationType)
	{
		Trace.Assert(!canonicalInfoByInterfaceType.ContainsKey(interfaceType), $"Type {interfaceType} already has a canonical implementation");

		Trace.Assert(!infosByInterfaceType.ContainsKey(interfaceType), $"Type {interfaceType} already has an implementation");

		EnsurePropNos(interfaceType);

		var layoutType = GetCanonicalLayoutStructType(interfaceType);

		var layout = PeachTypeLayout.Create(interfaceType, layoutType, this);

		Trace.Assert(!typeNosByLayout.ContainsKey(layout), $"Type {interfaceType} gets a layout that is already known, even though the respective implementation is not");

		EnsureImplementationType(layout, out typeNo, out implementationType);

		SetAsCanonical(interfaceType, typeNo);
	}

	Boolean EnsureImplementationType(PeachTypeLayout typeLayout, out TypeNo typeNo, out Type implementationType, TypeNo? desiredTypeNo = null)
	{
		var interfaceType = typeLayout.InterfaceType;

		Trace.Assert(interfaceType.IsInterface);

		if (typeNosByLayout.TryGetValue(typeLayout, out typeNo))
		{
			Trace.Assert(desiredTypeNo?.Equals(typeNo) ?? true, $"Trying to ensure type {desiredTypeNo}, the same layout already exists under {typeNo}");

			var entry = GetEntry(typeNo);

			Trace.Assert(entry.InterfaceType == interfaceType);

			implementationType = entry.ImplementationType;

			return false;
		}
		else
		{
			EnsurePropNos(typeLayout.InterfaceType, typeLayout);

			typeNo = desiredTypeNo ?? CreateTypeNo();

			var configuration = typeLayout.ToConfiguration(typeNo);

			implementationType = peachBakery.Resolve(interfaceType, configuration);

			if (!infosByInterfaceType.TryGetValue(interfaceType, out var list))
			{
				list = infosByInterfaceType[interfaceType] = new List<PeachTypeInfo>();
			}

			list.Add((implementationType, configuration));

			SetEntry(new Entry(typeNo, configuration, interfaceType, implementationType));

			Debug.Assert(implementationType.Name.EndsWith(typeNo.no.ToString()), $"Created type {implementationType}'s name doesn't say it's number for TypeNo {typeNo}");

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
		Debug.Assert(!entry.TypeNo.IsFundamental);

		if (!typeNosByImplementationType.TryAdd(entry.ImplementationType, entry.TypeNo))
		{
			Throw(entry);
		}

		if (!typeNosByLayout.TryAdd(entry.Layout, entry.TypeNo))
		{
			Throw(entry);
		}

		while (peachTypesByNo.Count <= entry.TypeNo.no)
		{
			peachTypesByNo.Add(default);
		}

		var no = entry.TypeNo.no;

		if (peachTypesByNo[no] is not null)
		{
			throw new Exception($"Type no {no} is already registered");
		}

		peachTypesByNo[no] = entry;
	}

	public Entry GetEntry(TypeNo typeNo)
	{
		Trace.Assert(!typeNo.IsFundamental);

		Trace.Assert(typeNo.no > 0);

		try
		{
			return peachTypesByNo[typeNo.no] ?? Throw(typeNo);
		}
		catch (ArgumentOutOfRangeException)
		{
			return Throw(typeNo);
		}
	}

	void SetAsCanonical(Type interfaceType, TypeNo typeNo)
	{
		var entry = GetEntry(typeNo);

		if (!canonicalInfoByInterfaceType.TryAdd(interfaceType, (typeNo, entry.ImplementationType)))
		{
			throw new InvalidOperationException($"Interface ${interfaceType} already has a canonical TypeNo");
		}
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

	static Entry Throw(TypeNo type)
	{
		throw new ArgumentException($"Type {type} is unknown");
	}

	public void LookupClrType(Type type, out TypeNo typeNo, out Type clrType)
	{
		if (ObjectTypeKinds.GetHandlerOrNull(type) is IObjectTypeHandler handler)
		{
			typeNo = handler.TypeNo;
			clrType = type;
		}
		else
		{
			var info = LookupCanonical(type);
			typeNo = info.typeNo;
			var entry = GetEntry(info.typeNo);
			clrType = entry.InterfaceType ?? throw new Exception($"The type {type} is not canonical after all");
		}
	}

	TypeNo CreateTypeNo() => new TypeNo(nextTypeNo++);

	#region PropNos

	public void EnsurePropNos(Type interfaceType)
	{
		foreach (var property in interfaceType.GetProperties())
		{
			EnsurePropNo(property);
		}
	}

	void EnsurePropNos(Type interfaceType, PeachTypeLayout layout)
	{
		foreach (var property in interfaceType.GetProperties())
		{
			if (layout.Properties.dict.TryGetValue(property, out var entry))
			{
				EnsurePropNo(property, entry.PropNo);
			}

			EnsurePropNo(property);
		}
	}

	PropNo EnsurePropNo(PropertyInfo property, PropNo? desiredPropNo = null)
	{
		if (!propNosByPropertyInfo.TryGetValue(property, out var propNo))
		{
			propNo = propNosByPropertyInfo[property] = desiredPropNo ?? new PropNo(nextPropNo++);
		}

		return propNo;
	}

	#endregion

	#region Export / Import

	public Type AddStoredType(ITypeDescription description)
	{
		var configuration = PeachTypeLayout.Create(clrTypeResolver, description);

		return AddAlternativeType(configuration, new TypeNo(description.No));
	}

	public void WriteAllTypeDescriptions(Object?[] targets, ref Boolean didWrite)
	{
		var n = peachTypesByNo.Count;

		Trace.Assert(n == targets.Length);

		for (var i = 0; i < n; i++)
		{
			var entry = peachTypesByNo[i];

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

			LookupClrType(type, out var typeNo, out var clrType);

			descriptionEntries[i] = new TypeDescriptionEntry
			{
				ClrName = clrTypeResolver.GetFqTypeName(clrType),
				PropertyNo = GetPropNo(property).no,
				Offset = entry.Offset,
				Size = entry.Size,
				TypeNo = typeNo
			};
		}

		target.Properties = descriptionEntries;
	}

	#endregion
}
