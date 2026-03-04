namespace AlphaBee;

[TestClass]
public class PeachTypeRegistryTests
{
	PeachTypeRegistry registry = null!;

	public interface IFoo
	{
		public Int32 Number { get; set; }
	}

	public interface IBar
	{
		public String String { get; set; }

		public IFoo Foo { get; set; }
	}

	[TestInitialize]
	public void Setup()
	{
		registry = new PeachTypeRegistry();
	}

	[TestMethod]
	public void TestFundamentals()
	{
		registry.LookupClrType(typeof(Byte[]), out var byteArrayRef, out var byteArrayType);

		Assert.AreEqual(TypeCode.Byte, byteArrayRef.typeByte.Code);
		Assert.AreEqual(typeof(Byte[]), byteArrayType);
	}

	[TestMethod]
	public void Test2()
	{
		var initialCount = registry.Count;

		registry.EnsureCanonicalImplementation(typeof(IFoo), out var fooRef, out var fooType);
		registry.EnsureCanonicalImplementation(typeof(IBar), out var barRef, out var barType);

		Assert.AreEqual(initialCount, fooRef.no);
		Assert.AreEqual(initialCount + 1, barRef.no);
	}
}
