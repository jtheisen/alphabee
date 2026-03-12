namespace AlphaBee;

[TestClass]
public class AutoLayoutBakingTests
{
	public interface IFoo
	{
		public Int32 No { get; set; }

		public String String { get; set; }
	}

	public interface IBase
	{
		public Int32 Foo { get; }
	}

	public interface IDerived : IBase
	{
		public Int32 Bar { get; }
	}

	[DataTestMethod]
	[DataRow(typeof(IFoo))]
	[DataRow(typeof(IDerived))]
	public void TestBaking(Type type)
	{
		var typeRegistry = new PeachTypeRegistry(PeachTypeRegistry.Stage.Ready);

		typeRegistry.EnsureCanonicalImplementation(type, out _, out var implementationType);

		Assert.IsTrue(implementationType.IsAssignableTo(type));

		Activator.CreateInstance(implementationType);
	}

	[TestMethod]
	public void TestBaseProperties()
	{
		var typeRegistry = new PeachTypeRegistry(PeachTypeRegistry.Stage.Ready);

		typeRegistry.EnsureCanonicalImplementation(typeof(IDerived), out _, out var implementationType);

		Assert.IsTrue(typeRegistry.Resolve(typeof(IBase).GetProperty(nameof(IBase.Foo))!).PropNo.no > 0);
	}
}
