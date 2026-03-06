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

	public interface IDerived
	{
		public Int32 Bar { get; }
	}

	[DataTestMethod]
	[DataRow(typeof(IFoo))]
	[DataRow(typeof(IDerived))]
	public void TestBaking(Type type)
	{
		var typeRegistry = new PeachTypeRegistry();

		typeRegistry.EnsureCanonicalImplementation(type, out _, out var implementationType);

		Assert.IsTrue(implementationType.IsAssignableTo(type));

		Activator.CreateInstance(implementationType);
	}
	
}
