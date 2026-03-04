namespace AlphaBee;

[TestClass]
public class AutoLayoutBakingTests
{
	public interface IFoo
	{
		public Int32 No { get; set; }

		public String String { get; set; }
	}

	[TestMethod]
	public void TestBaking()
	{
		var typeRegistry = new PeachTypeRegistry();

		typeRegistry.EnsureCanonicalImplementation(typeof(IFoo), out _, out var implementationType);
	}
	
}
