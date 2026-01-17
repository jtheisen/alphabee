using Moldinium.Injection;
using Moldinium.Internals;

namespace AlphaBee.EntityFramework;

public interface IRoot
{
	DbSet<IFoo> Foos { get; set; }
	DbSet<IBar> Bars { get; set; }
}

public interface IFoo
{
	Guid Id { get; set; }

	ICollection<IBar> Bars { get; }
}

public interface IBar
{
	Guid Id { get; set; }

	IFoo Foo { get; set; }
}

public static class Helper
{
	public static Type GetMoldiniumRootType(Type type, Action<MoldiniumConfigurationBuilder> build)
	{
		var helperType = typeof(Helper<>).MakeGenericType(type);

		var helper = (IHelper)Activator.CreateInstance(helperType)!;

		return helper.GetMoldiniumRootType(build);
	}
}

public interface IHelper
{
	Type GetMoldiniumRootType(Action<MoldiniumConfigurationBuilder> build);
}

public class Helper<T>
{
	public Type GetMoldiniumRootType(Action<MoldiniumConfigurationBuilder> build)
		=> new Scope<T>(GetProvider(build), DependencyRuntimeMaturity.FinishedInstance).Root.GetType();

	IDependencyProvider GetProvider(Action<MoldiniumConfigurationBuilder> build, IServiceProvider? services = null)
	{
		var builder = new MoldiniumConfigurationBuilder();
		build(builder);
		var configuration = builder.Build(services);
		return DependencyProvider.Create(configuration);
	}
}

[TestClass]
public sealed class EntityFrameworkTests
{
	[TestMethod]
	public void TestConstruction()
	{
		var rootType = Helper.GetMoldiniumRootType(typeof(IRoot), builder => builder.SetMode(MoldiniumDefaultMode.Basic));


	}



}
