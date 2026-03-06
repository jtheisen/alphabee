using AlphaBee.Utilities;
using Moldinium.Baking;
using Moldinium.Utilities;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CanonicalInfo = (AlphaBee.TypeNo typeNo, System.Type implementationType);
using PeachTypeInfo = (System.Type implementationType, AlphaBee.PeachTypeConfiguration typeConfiguration);

namespace AlphaBee;

public class PeachTypeRegistry : IPropNoResolver
{
	Int32 nextTypeNo = 1, nextPropNo = 1;

	AbstractBakery peachBakery, layoutBakery;

	public class Entry
	{
		public Entry(TypeNo TypeNo, Type InterfaceType)
		{
			this.TypeNo = TypeNo;
			this.InterfaceType = InterfaceType;
		}

		public PeachTypeLayout? Layout => Configuration?.Layout;

		public TypeNo TypeNo { get; }
		public Type InterfaceType { get; }
		public Boolean AreBaseInterfacesEnsured { get; private set; }
		public PeachTypeConfiguration? Configuration { get; private set; }
		public Type? ImplementationType { get; private set; }

		public Boolean GetImplementation([MaybeNullWhen(false)] out PeachTypeConfiguration configuration, [MaybeNullWhen(false)] out Type implementationType)
		{
			configuration = Configuration;
			implementationType = ImplementationType;

			return configuration is not null && implementationType is not null;
		}

		void ThrowFailedSetImplementation()
		{
			throw new Exception($"Implemenation for {this} was already set");
		}

		public void EnsureBaseInterfaces(PeachTypeRegistry self)
		{
			foreach (var type in InterfaceType.GetInterfaces())
			{
				self.EnsureEntry(type);
			}

			AreBaseInterfacesEnsured = true;
		}

		public void SetImplementation(PeachTypeRegistry self, PeachTypeConfiguration configuration, Type implementationType)
		{
			if (!self.typeNosByImplementationType.TryAdd(ImplementationType = implementationType, TypeNo))
			{
				ThrowFailedSetImplementation();
			}

			if (!self.typeNosByLayout.TryAdd((Configuration = configuration).Layout, TypeNo))
			{
				ThrowFailedSetImplementation();
			}

			var interfaceType = configuration.InterfaceType;

			if (!self.infosByInterfaceType.TryGetValue(interfaceType, out var list))
			{
				list = self.infosByInterfaceType[interfaceType] = new List<PeachTypeInfo>();
			}

			list.Add((implementationType, configuration));
		}
	}

	private readonly IClrTypeResolver clrTypeResolver;

	private readonly List<Entry?> typesTypesByNo = new();

	private readonly Dictionary<PropertyInfo, PropNo> propNosByPropertyInfo = new();

	private readonly Dictionary<Type, TypeNo> typeNosByInterfaceType = new();

	private readonly Dictionary<Type, TypeNo> typeNosByImplementationType = new();

	private readonly Dictionary<PeachTypeLayout, TypeNo> typeNosByLayout = new();

	private readonly Dictionary<Type, CanonicalInfo> canonicalInfoByInterfaceType = new();

	private readonly Dictionary<Type, List<PeachTypeInfo>> infosByInterfaceType = new();

	public PeachTypeRegistry(IClrTypeResolver? clrTypeResolver = null)
	{
		peachBakery = CreateBakery<PeachPropertyImplementationProvider>("peaches");
		layoutBakery = CreateBakery<LayoutPropertyImplementationProvider>("layouts", c => c with {
			PrefixBackingFields = false,
			CustomMemberModifier = LayoutBakingCustomMemberModifier.Instance
		});

		this.clrTypeResolver = clrTypeResolver ?? new ClrTypeResolver();
	}

	static AbstractBakery CreateBakery<ProviderT>(String name, Func<BakeryConfiguration, BakeryConfiguration>? modify = null)
		where ProviderT : PropertyImplementationProvider, new()
	{
		var provider = new ProviderT();

		var config = BakeryConfiguration.Create(provider) with { MakeValue = true };

		if (modify is not null)
		{
			config = modify(config);
		}

		var bakery = config.CreateBakery(name);

		return bakery;
	}

	public Int32 Count => nextTypeNo;

	public void Validate()
	{
		for (var i = 1; i < nextTypeNo; ++i)
		{
			var entry = typesTypesByNo[i];

			Trace.Assert(entry is not null);

			var typeNo = entry.TypeNo;
			var interfaceType = entry.InterfaceType;

			Trace.Assert(typeNo.no == i);

			if (!entry.GetImplementation(out var configuration, out var implementationType))
			{
				continue;
			}

			Trace.Assert(typeNo.Equals(typeNosByImplementationType[implementationType]));

			var infos = infosByInterfaceType[interfaceType];

			Trace.Assert(typeNosByLayout[configuration.Layout].Equals(entry.TypeNo));

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
			Trace.Assert(entry.ImplementationType!.Equals(p.Key));
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

	internal Type AddImplementation(PeachTypeLayout configuration, TypeNo? typeNoOrNull, Boolean allowNewImplementation = false)
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

		EnsureEntry(interfaceType);

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

		Entry entry;

		if (typeNosByLayout.TryGetValue(typeLayout, out typeNo))
		{
			Trace.Assert(desiredTypeNo?.Equals(typeNo) ?? true, $"Trying to ensure type {desiredTypeNo}, the same layout already exists under {typeNo}");

			entry = GetEntry(typeNo);

			Trace.Assert(entry.InterfaceType == interfaceType);

			if (entry.ImplementationType is not null)
			{
				implementationType = entry.ImplementationType;

				return false;
			}
		}
		else
		{
			entry = EnsureEntry(interfaceType);

			typeNo = entry.TypeNo;
		}

		var configuration = typeLayout.ToConfiguration(typeNo);

		implementationType = peachBakery.Resolve(interfaceType, configuration);

		Debug.Assert(implementationType.Name.EndsWith(typeNo.no.ToString()), $"Created type {implementationType}'s name doesn't say it's number for TypeNo {typeNo}");

		return true;
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

	public Entry GetEntry(TypeNo typeNo)
	{
		Trace.Assert(!typeNo.IsFundamental);

		Trace.Assert(typeNo.no > 0);

		try
		{
			return typesTypesByNo[typeNo.no] ?? Throw(typeNo);
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

	Entry AssignTypeNo(Type interfaceType, TypeNo? desiredTypeNo = null)
	{
		Debug.Assert(!(desiredTypeNo?.IsFundamental ?? false));

		var typeNo = desiredTypeNo ?? new TypeNo(nextTypeNo++);

		var no = typeNo.no;

		while (typesTypesByNo.Count <= no)
		{
			typesTypesByNo.Add(default);
		}

		var exists = typesTypesByNo[no] is not null;

		if (typeNosByInterfaceType.TryAdd(interfaceType, typeNo) || exists)
		{
			throw new Exception($"Type no {no} is already registered");
		}

		return typesTypesByNo[no] = new Entry(typeNo, interfaceType); ;
	}

	#region Nos

	Entry EnsureEntry(Type interfaceType)
	{
		if (typeNosByInterfaceType.TryGetValue(interfaceType, out var typeNo))
		{
			return GetEntry(typeNo);
		}
		else
		{
			return AssignNos(interfaceType);
		}
	}

	Entry AssignNos(Type interfaceType)
	{
		foreach (var property in interfaceType.GetProperties())
		{
			AssignPropNo(property);
		}

		return AssignTypeNo(interfaceType);
	}

	void AssignNos(Type interfaceType, TypeNo typeNo, PeachTypeLayout layout)
	{
		AssignTypeNo(interfaceType, typeNo);

		foreach (var property in interfaceType.GetProperties())
		{
			if (layout.Properties.TryGetValue(property, out var entry))
			{
				AssignPropNo(property, entry.PropNo);
			}
			else
			{
				throw new Exception($"Missing layout property for {property}");

				//AssignPropNo(property);
			}
		}
	}

	PropNo AssignPropNo(PropertyInfo property, PropNo? desiredPropNo = null)
	{
		if (propNosByPropertyInfo.TryGetValue(property, out var propNo))
		{
			throw new Exception($"PropNo for property {property} was already assigned");
		}

		return propNosByPropertyInfo[property] = desiredPropNo ?? new PropNo(nextPropNo++);
	}

	#endregion

	#region Export / Import

	Entry AssignNos(ITypeDescription description)
	{
		var typeNo = new TypeNo(description.No);

		Trace.Assert(description.ClrName is not null);

		var interfaceType = clrTypeResolver.GetClrType(description.ClrName);

		if (description.Properties is TypeDescriptionEntry[] propertyEntries)
		{
			foreach (var propertyEntry in propertyEntries)
			{
				Trace.Assert(propertyEntry.ClrName is not null);

				var property = interfaceType.GetNonNullProperty(propertyEntry.ClrName);

				AssignPropNo(property, propertyEntry.PropertyNo);
			}
		}

		return AssignTypeNo(interfaceType, typeNo);
	}

	//void RegisterUnimplementedType(ITypeDescription description)
	//{
	//	var typeNo = new TypeNo(description.No);

	//	Trace.Assert(description.ClrName is not null);

	//	var interfaceType = clrTypeResolver.GetClrType(description.ClrName);





	//	if (description.Properties is TypeDescriptionEntry[] propertyEntries)
	//	{
	//		foreach (var propertyEntry in propertyEntries)
	//		{
	//			Trace.Assert(propertyEntry.ClrName is not null);

	//			var property = interfaceType.GetNonNullProperty(propertyEntry.ClrName);

	//			AssignPropNo(property, propertyEntry.PropertyNo);
	//		}
	//	}
	//}

	public void ImportAllTypeDescriptions(Object?[] targets)
	{
		Trace.Assert(targets is not null);

		var descriptions = targets.OfType<ITypeDescription>();

		// We need to register all types before we start creating implementations
		// because of interface inheritance: All types need to be known before
		// we start implementing.

		foreach (var description in descriptions)
		{
			AssignNos(description);
		}

		foreach (var description in descriptions)
		{
			var configuration = PeachTypeLayout.Create(clrTypeResolver, description);

			AddImplementation(configuration, new TypeNo(description.No));
		}
	}

	public void WriteAllTypeDescriptions(Object?[] targets, ref Boolean didWrite)
	{
		var n = typesTypesByNo.Count;

		Trace.Assert(n == targets?.Length);

		for (var i = 0; i < n; i++)
		{
			var entry = typesTypesByNo[i];

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

		var kvps = configuration.Properties.ToArray();

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
