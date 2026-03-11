using Moldinium.Baking;
using Moldinium.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CanonicalInfo = (AlphaBee.TypeNo typeNo, System.Type implementationType);
using PeachTypeInfo = (System.Type implementationType, AlphaBee.PeachTypeConfiguration typeConfiguration);

namespace AlphaBee;

public class PeachTypeRegistry : IPropNoResolver
{
	Int32 nextTypeNo = 1, nextPropNo = 1, firstNewTypeNo = 0;

	Stage stage = Stage.Initial;

	AbstractBakery peachBakery, layoutBakery;

	public enum Stage
	{
		Bootstrapping,
		Declaring,
		Importing,
		Ready,

		Initial = Bootstrapping
	}

	public class DuplicateImplementationException(String message) : Exception(message) { }
	public class MultipleImplementationsException(String message) : Exception(message) { }

	[DebuggerDisplay("{ToString()}")]
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
		public Boolean IsCanonical { get; private set; }
		public Boolean IsBootstrapped { get; private set; }

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

		public void SetAsCanonical(PeachTypeRegistry self, Boolean isBootstrappedType = false)
		{
			if (self.firstNewTypeNo == 0 && !isBootstrappedType)
			{
				self.firstNewTypeNo = TypeNo.no;
			}

			if (!GetImplementation(out var configuration, out var implementationType))
			{
				throw new Exception($"Type {this} isn't implemented and can't be set as canonical");
			}

			self.canonicalInfoByInterfaceType.TryAdd(InterfaceType, (TypeNo, implementationType));

			IsCanonical = true;
			IsBootstrapped = isBootstrappedType;
		}

		public override String ToString()
		{
			var implementationSuffix = GetImplementation(out _, out var implementationType) ? $" {implementationType}" : null;

			return $"[{TypeNo} {InterfaceType}{implementationSuffix}]";
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

	public PeachTypeRegistry(Stage initialStage)
		: this(null, initialStage)
	{
	}

	public PeachTypeRegistry(IClrTypeResolver? clrTypeResolver = null, Stage initialStage = Stage.Initial)
	{
		peachBakery = CreateBakery<PeachPropertyImplementationProvider>("peaches");
		layoutBakery = CreateBakery<LayoutPropertyImplementationProvider>("layouts", c => c with {
			CustomMemberModifier = LayoutBakingCustomMemberModifier.Instance
		});

		this.clrTypeResolver = clrTypeResolver ?? new ClrTypeResolver();

		while (stage < initialStage)
		{
			SetStage(stage + 1);
		}
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

	void AssertStage(Stage expectedStage)
	{
		Debug.Assert(stage == expectedStage, $"The stage is required to be {expectedStage} but was {stage}");

		switch (stage)
		{
			case Stage.Importing:
				Trace.Assert(firstNewTypeNo == 0);
				break;
			default:
				break;
		}
	}

	public void SetStage(Stage newStage)
	{
		Trace.Assert(stage + 1 == newStage);

		stage = newStage;
	}

	public Type AssignImplementation(PeachTypeLayout layout)
	{
		AssertStage(Stage.Importing);

		if (!typeNosByInterfaceType.TryGetValue(layout.InterfaceType, out var typeNo))
		{
			throw new Exception($"Can't make implementation for unknown type {layout.InterfaceType}");
		}

		var entry = GetEntry(typeNo);

		if (entry.GetImplementation(out var configuration, out _) && configuration.Layout != layout)
		{
			throw new MultipleImplementationsException($"Multiple implementations for the same interface type are currently not supported");
		}

		MakeImplementation(entry, layout, out _, out var implementationType);

		return implementationType;
	}

	public void BootstrapImplementation<InterfaceT>()
	{
		CreateCanonicalType(typeof(InterfaceT), out _, out _, isBootstrappedType: true);
	}

	public Type EnsureCanonicalImplementation(Type interfaceType)
	{
		AssertStage(Stage.Ready);

		EnsureCanonicalImplementation(interfaceType, out _, out var implementationType);

		return implementationType;
	}

	Boolean GetCanonicalImplementationOrAssertStateReady(Type interfaceType, [MaybeNullWhen(false)] out TypeNo typeNo, [MaybeNullWhen(false)] out Type implementationType)
	{
		if (canonicalInfoByInterfaceType.TryGetValue(interfaceType, out var info))
		{
			// This path is also used during bootstrapping when the registry isn't creating new types yet

			typeNo = info.typeNo;
			implementationType = info.implementationType;

			return true;
		}
		else
		{
			typeNo = default;
			implementationType = default;

			if (stage == Stage.Bootstrapping)
			{
				throw new Exception($"The type {interfaceType} was not registered for bootstrapping");
			}

			AssertStage(Stage.Ready);

			return false;
		}
	}

	public void EnsureCanonicalImplementation(Type interfaceType, out TypeNo typeNo, out Type implementationType)
	{
		if (GetCanonicalImplementationOrAssertStateReady(interfaceType, out typeNo, out implementationType!))
		{
			return;
		}
		else if (infosByInterfaceType.TryGetValue(interfaceType, out var list) && list.Count > 0)
		{
			var first = list[0];

			implementationType = first.implementationType;
			typeNo = typeNosByImplementationType[implementationType];

			var entry = GetEntry(typeNo);

			entry.SetAsCanonical(this);
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

	void CreateCanonicalType(Type interfaceType, out TypeNo typeNo, out Type implementationType, Boolean isBootstrappedType = false)
	{
		Trace.Assert(!canonicalInfoByInterfaceType.ContainsKey(interfaceType), $"Type {interfaceType} already has a canonical implementation");

		Trace.Assert(!infosByInterfaceType.ContainsKey(interfaceType), $"Type {interfaceType} already has an implementation");

		var entry = EnsureEntry(interfaceType);

		typeNo = entry.TypeNo;

		entry.EnsureBaseInterfaces(this);

		var layoutType = GetCanonicalLayoutStructType(interfaceType);

		var layout = PeachTypeLayout.Create(interfaceType, layoutType, this);

		Trace.Assert(!typeNosByLayout.ContainsKey(layout), $"Type {interfaceType} gets a layout that is already known, even though the respective implementation is not");

		MakeImplementation(entry, layout, out _, out implementationType);

		entry.SetAsCanonical(this, isBootstrappedType);
	}

	void MakeImplementation(Entry entry, PeachTypeLayout typeLayout, out PeachTypeConfiguration configuration, out Type implementationType)
	{
		var interfaceType = typeLayout.InterfaceType;

		Trace.Assert(interfaceType.IsInterface);

		var typeNo = entry.TypeNo;

		if (typeNosByLayout.ContainsKey(typeLayout))
		{
			throw new DuplicateImplementationException($"An implementation with the same layout already exists");
		}

		configuration = typeLayout.ToConfiguration(typeNo);

		implementationType = peachBakery.Resolve(interfaceType, configuration);

		Debug.Assert(implementationType.Name.EndsWith(typeNo.no.ToString()), $"Created type {implementationType}'s name doesn't say it's number for TypeNo {typeNo}");

		entry.SetImplementation(this, configuration, implementationType);
	}

	Type GetCanonicalLayoutStructType(Type interfaceType)
	{
		if (interfaceType.GetCustomAttribute<PeachLayoutAttribute>()?.LayoutType is Type layoutType)
		{
			Trace.Assert(layoutType.IsValueType, $"The explicitly given layout type for {interfaceType} is expected to be a struct");

			return layoutType;
		}
		else
		{
			return layoutBakery.Resolve(interfaceType);
		}
	}

	public void GetImplementation(TypeNo typeNo, out Type implementationType, out Int32 size)
	{
		var entry = GetEntry(typeNo);

		if (!entry.GetImplementation(out var configuration, out implementationType!))
		{
			throw new Exception($"No implementation found for {typeNo}");
		}

		size = configuration.Size;
	}

	Entry GetEntry(TypeNo typeNo)
	{
		static Entry Throw(TypeNo type)
		{
			throw new ArgumentException($"Type {type} is unknown");
		}

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

	public void LookupCanonical(Type type, out TypeNo typeNo, out Type implementationType, Boolean implementIfMissing = false)
	{
		AssertStage(Stage.Ready);

		if (implementIfMissing)
		{
			EnsureCanonicalImplementation(type, out typeNo, out implementationType);
		}
		else
		{
			try
			{
				(typeNo, implementationType) = canonicalInfoByInterfaceType[type];
			}
			catch (ArgumentOutOfRangeException)
			{
				throw new ArgumentException($"Type {type} is unknown");
			}
		}
	}

	public void LookupClrType(Type type, out TypeNo typeNo, out Type clrType, Boolean implementIfMissing = false)
	{
		AssertStage(Stage.Ready);

		if (FundamentalTypes.GetHandlerOrNull(type) is ITypeHandler handler)
		{
			typeNo = handler.TypeNo;
			clrType = type;
		}
		else
		{
			LookupCanonical(type, out typeNo, out _, implementIfMissing);
			var entry = GetEntry(typeNo);
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

		if (!typeNosByInterfaceType.TryAdd(interfaceType, typeNo) || exists)
		{
			throw new Exception($"Type no {no} is already registered");
		}

		return typesTypesByNo[no] = new Entry(typeNo, interfaceType); ;
	}

	#region Nos

	public void EnsureTypeNosForTesting(Type interfaceType)
	{
		EnsureEntry(interfaceType).EnsureBaseInterfaces(this);
	}

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
		AssertStage(Stage.Declaring);

		var typeNo = new TypeNo(description.No);

		Trace.Assert(description.ClrName is not null);

		var interfaceType = clrTypeResolver.GetClrType(description.ClrName);

		if (description.Properties is IPropertyDescription[] propertyEntries)
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

	public void ReadyEmpty()
	{
		AssertStage(Stage.Bootstrapping);

		SetStage(Stage.Declaring);
		SetStage(Stage.Importing);
		SetStage(Stage.Ready);
	}

	public void ImportAllTypeDescriptions(Object?[] targets)
	{
		AssertStage(Stage.Bootstrapping);

		SetStage(Stage.Declaring);

		Trace.Assert(targets is not null);

		var descriptions = targets.OfType<ITypeDescription>();

		// We need to register all types before we start creating implementations
		// because of interface inheritance: All types need to be known before
		// we start implementing.

		foreach (var description in descriptions)
		{
			AssignNos(description);
		}

		SetStage(Stage.Importing);

		foreach (var description in descriptions)
		{
			if (description.Properties is null)
			{
				// This is an unimplemented type

				continue;
			}

			var configuration = PeachTypeLayout.Create(clrTypeResolver, description);

			AssignImplementation(configuration);
		}

		SetStage(Stage.Ready);
	}

	public record BookkeepingTypeFactory(
		Func<ITypeDescription> CreateTypeDecription,
		Func<IPropertyDescription> CreatePropertyDecription
	);

	public void ExportAllTypeDescriptions(BookkeepingTypeFactory factory, Object?[] targets, ref Boolean didWrite)
	{
		AssertStage(Stage.Ready);

		var n = typesTypesByNo.Count;

		Trace.Assert(n == targets?.Length);

		for (var i = 0; i < n; i++)
		{
			var entry = typesTypesByNo[i];

			if (entry is null || entry.IsBootstrapped) continue;

			if (targets[i] is not ITypeDescription target)
			{
				targets[i] = target = factory.CreateTypeDecription();
			}

			Trace.Assert(target is not null);

			if (target.No != i)
			{
				didWrite = true;

				var layout = entry.Layout;

				WriteTypeDescription(factory, target, entry.InterfaceType, layout, i);
			}
		}
	}

	public void WriteTypeDescription(BookkeepingTypeFactory factory, ITypeDescription target, Type interfaceType, PeachTypeLayout? layout, Int32 no)
	{
		AssertStage(Stage.Ready);

		target.No = no;

		target.ClrName = clrTypeResolver.GetFqTypeName(interfaceType);

		if (layout is null)
		{
			target.Size = 0;
			target.Properties = null;

			return;
		}

		target.Size = layout.Size;

		var kvps = layout.Properties.ToArray();

		var n = kvps.Length;

		var properties = new IPropertyDescription[n];

		for (var i = 0; i < n; ++i)
		{
			var (property, entry) = kvps[i];

			var propertyType = property.PropertyType;

			LookupClrType(propertyType, out var typeNo, out var clrType, implementIfMissing: true);

			var p = properties[i] = factory.CreatePropertyDecription();
			p.ClrName = property.GetFqPropertyName();
			p.PropertyNo = GetPropNo(property).no;
			p.Offset = entry.Offset;
			p.Size = entry.Size;
			p.TypeNo = typeNo;
		}

		target.Properties = properties;
	}

	#endregion
}
