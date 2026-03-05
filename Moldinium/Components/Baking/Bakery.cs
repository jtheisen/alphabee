using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Moldinium.Common.Defaulting;

namespace Moldinium.Baking;

public abstract class AbstractBakery
{
    Dictionary<(Type type, String? suffix), Type> bakedTypes = new();

    public T Create<T>(ITypeConfiguration? variant = null)
    {
        var type = Resolve(typeof(T), variant);

        var instance = Activator.CreateInstance(type);

        if (instance is T t)
        {
            return t;
        }
        else
        {
            throw new Exception("Unexpectedly got null or the wrong type from activator");
        }
    }

    public Type Resolve(Type interfaceOrBaseType, ITypeConfiguration? variant = null)
    {
        var key = (interfaceOrBaseType, variant?.TypeSuffix);

		if (!bakedTypes.TryGetValue(key, out var bakedType))
        {
            bakedTypes[key] = bakedType = CreateType(interfaceOrBaseType, variant);
        }

        return bakedType;
    }

    protected abstract Type CreateType(Type interfaceOrBaseType, ITypeConfiguration? typeConfiguration = null);
}

public abstract class AbstractlyBakery : AbstractBakery
{
    protected readonly string name;
    protected readonly bool makeAbstract;
    protected readonly AssemblyBuilder assemblyBuilder;
    protected readonly ModuleBuilder moduleBuilder;
    protected readonly TypeAttributes typeAttributes;

    HashSet<Assembly> accessedAssemblies = new HashSet<Assembly>();

    public AbstractlyBakery(String name, TypeAttributes typeAttributes = TypeAttributes.Public | TypeAttributes.Abstract)
    {
        this.name = name;
        this.typeAttributes = typeAttributes;
        assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule(name);
    }

	/** FullName is of the form [namespace].[parent-class-name]+[type-name]^[parameters] and that is almost the form
     * the TypeBuilder expects - just the '+' is something it rejects (escapes) which is sensible given the type isn't
     * going to actually be a nested type. The next best thing is to just replace the '+' with a '.'. */
	protected String GetTypeName(Type interfaceOrBaseType, String? suffix)
	{
		return $"{interfaceOrBaseType.FullName?.Replace('+', '.')}{suffix}";
	}

	protected override Type CreateType(Type interfaceOrBaseType, ITypeConfiguration? typeConfiguration)
    {
        var name = GetTypeName(interfaceOrBaseType, typeConfiguration?.TypeSuffix);

        return CreateImpl(name, interfaceOrBaseType, typeConfiguration);
    }

    protected abstract Type CreateImpl(String name, Type interfaceOrBaseType, ITypeConfiguration? typeConfiguration);

    protected void EnsureAccessToAssembly(Assembly assembly)
    {
        if (accessedAssemblies.Add(assembly))
        {
            var ignoresAccessChecksTo = new CustomAttributeBuilder
            (
                typeof(IgnoresAccessChecksToAttribute).GetConstructor(new Type[] { typeof(String) })
                ?? throw new InternalErrorException("Can't get constructor for IgnoresAccessChecksToAttribute"),
                new object[] { assembly.GetName().Name ?? throw new InternalErrorException("Can't get name for assembly") }
            );

            assemblyBuilder.SetCustomAttribute(ignoresAccessChecksTo);
        }
    }
}

public class Bakery : AbstractlyBakery
{
    readonly BakeryConfiguration configuration;
    readonly IBakeryComponentGenerators generators;
    readonly IDefaultProvider defaultProvider;

    public String Name => name;

    public Bakery(String name)
        : this(name, BakeryConfiguration.PocGenerationConfiguration) { }

    public Bakery(String name, BakeryConfiguration configuration)
        : base(name, TypeAttributes.Public)
    {
        this.configuration = configuration;
        generators = this.configuration.Generators;
        defaultProvider = this.configuration.DefaultProvider;
    }

    (ImplementationMapping mapping, Type[] publicMixins) Analyze(Type interfaceOrBaseType, IEnumerable<Type> extraInterfaces)
    {
        var processor = new AnalyzingBakingProcessor(generators);

        processor.VisitFirst(interfaceOrBaseType);

        foreach (var extraInterface in extraInterfaces)
        {
            processor.VisitFirst(extraInterface);
        }

        var interfaceTypes = processor.Interfaces;
        var mixinTypes = processor.PublicMixins;

        var mapping = new ImplementationMapping(interfaceTypes.Concat(mixinTypes).ToHashSet());

        return (mapping, mixinTypes.ToArray());
    }

    protected override Type CreateImpl(
        String name, Type interfaceOrBaseType, ITypeConfiguration? typeConfiguration)
    {
		var baseType =
			configuration.MakeValue ? typeof(ValueType) :
			interfaceOrBaseType.IsClass ? interfaceOrBaseType : null;

        var extraInterfaces = typeConfiguration?.GetExtraInterfaces() ?? Enumerable.Empty<Type>();

		var (interfaceMapping, publicMixins) = Analyze(interfaceOrBaseType, extraInterfaces);

		var processor = new BuildingBakingProcessor(
			name, baseType, typeAttributes, interfaceMapping, defaultProvider, generators,
            EnsureAccessToAssembly, moduleBuilder, configuration.PrefixBackingFields, typeConfiguration);

		return processor.Create(interfaceMapping.Interfaces, publicMixins);
	}
}
