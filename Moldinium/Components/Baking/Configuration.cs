using Moldinium.Common.Defaulting;
using Moldinium.Common.Misc;

namespace Moldinium.Baking;

public struct Dummy { }

public interface IBakeryComponentGenerators
{
    MixinGenerator[] GetMixInGenerators(Type type);

    AbstractMethodGenerator? GetMethodGenerator(MethodInfo method);

    AbstractPropertyGenerator? GetPropertyGenerator(PropertyInfo property);
    
    AbstractEventGenerator GetEventGenerator(EventInfo evt);
}

public class ComponentGenerators : IBakeryComponentGenerators
{
	private readonly PropertyImplementationProvider propertyImplementationProvider;
	private readonly Type propertyWrapperType;
	private readonly AbstractMethodGenerator? methodWrapperGenerator;
	private readonly AbstractEventGenerator eventGenerator;

    private readonly Dictionary<Type, AbstractPropertyGenerator> propertyGenerators = new();

    public ComponentGenerators(
		PropertyImplementationProvider propertyImplementationProvider,
        Type propertyWrapperType,
		Type? methodWrapperType,
		Type? eventImplementationType)
    {
		this.propertyImplementationProvider = propertyImplementationProvider;
		this.propertyWrapperType = propertyWrapperType;
		methodWrapperGenerator = MethodGenerator.Create(methodWrapperType ?? typeof(TrivialMethodWrapper));
		eventGenerator = EventGenerator.Create(eventImplementationType ?? typeof(GenericEventImplementation<>), typeof(TrivialEventWrapper));

        foreach (var type in propertyImplementationProvider.GetAll())
        {
			propertyGenerators[type] = PropertyGenerator.Create(type, propertyWrapperType);
		}
    }

    public MixinGenerator[] GetMixInGenerators(Type type) => new MixinGenerator[] { };

    public AbstractPropertyGenerator? GetPropertyGenerator(PropertyInfo property)
    {
		var type = propertyImplementationProvider.Get(property);

        if (!propertyGenerators.TryGetValue(type, out var generator))
        {
            throw new Exception($"Implementation type {type} was not listed by the provider");
		}

        return generator;
    }

    public AbstractMethodGenerator? GetMethodGenerator(MethodInfo method)
    {
        //if (!implemented) throw new Exception($"Method {method} on {method.DeclaringType} must have a default implementation to wrap");

        return methodWrapperGenerator;
    }

    public AbstractEventGenerator GetEventGenerator(EventInfo evt) => eventGenerator;

	public static ComponentGenerators Create(params Type[] implementations) => Create(null, implementations);

	public static ComponentGenerators Create(PropertyImplementationProvider? propertyImplementationProvider, params Type[] implementations)
    {
        foreach (var implementation in implementations)
        {
            CheckedImplementation.PreCheck(implementation);
        }

		var propertyImplementationType
	        = FindType(implementations, typeof(IPropertyImplementation));

		if (propertyImplementationProvider is PropertyImplementationProvider provider)
		{
            var dynamicImplementations = provider.GetAll();

            Trace.Assert(propertyImplementationType is null,
                "Can't provide both PropertyImplementationProvider and an IPropertyImplementation type");

            foreach (var implementation in dynamicImplementations)
            {
                CheckedImplementation.PreCheck(implementation);
            }
        }
        else
        {
            propertyImplementationProvider = new SingletonPropertyImplementationProvider(propertyImplementationType ?? typeof(SimplePropertyImplementation<>));
		}

        var methodWrapperType
            = FindType(implementations, typeof(IMethodWrapperImplementation));
		var propertyWrapperType
            = FindType(implementations, typeof(IPropertyWrapperImplementation)) ?? typeof(TrivialPropertyWrapper);
        var eventImplementationType
            = FindType(implementations, typeof(IEventImplementation));

        return new ComponentGenerators(
			propertyImplementationProvider,
			propertyWrapperType,
			methodWrapperType,
            eventImplementationType
        );
    }

    static Type? FindType(Type[] types, Type interfaceType)
    {
        var type = types
            .Where(t => TypeInterfaces.Get(t).DoesTypeImplement(interfaceType))
            .SingleOrDefault($"Multiple types of {interfaceType} found in {String.Join(", ", types.Cast<Type>())}");

        return type;
    }
}

public record BakeryConfiguration(
    IBakeryComponentGenerators Generators,
    IDefaultProvider DefaultProvider,
    ICustomMemberModifier? CustomMemberModifier = null,
	Boolean MakeAbstract = false,
    Boolean MakeValue = false,
    Boolean PrefixBackingFields = true
    )
{
	public static BakeryConfiguration Create(PropertyImplementationProvider propertyImplementationProvider, params Type[] implementations)
		=> new BakeryConfiguration(ComponentGenerators.Create(propertyImplementationProvider, implementations), Defaults.GetDefaultDefaultProvider());

	public static BakeryConfiguration Create(params Type[] implementations)
        => new BakeryConfiguration(ComponentGenerators.Create(implementations), Defaults.GetDefaultDefaultProvider());

    public static BakeryConfiguration PocGenerationConfiguration
        = new BakeryConfiguration(ComponentGenerators.Create(), Defaults.GetDefaultDefaultProvider());

    public AbstractlyBakery CreateBakery(String name) => new Bakery(name, this);
}

public class MixinGenerator { }
