namespace AlphaBee;

[TestClass]
public class InlineArrayBakingTests
{
	public interface IFoo
	{
		[InlineSpan(3)]
		public Span<Int16> Int16s { get; }
	}

	PeachContext context = null!;

	[TestInitialize]
	public void Setup()
	{
		var typeRegistry = new PeachTypeRegistry(PeachTypeRegistry.Stage.Ready);

		typeRegistry.EnsureCanonicalImplementation(typeof(IFoo));

		var storage = new TestStorage();

		context = new PeachContext(storage, typeRegistry);
	}

	[TestMethod]
	public void Test()
	{
		var foo = context.New<IFoo>();

		Assert.AreEqual(3, foo.Int16s.Length);

		foo.Int16s[1] = 42;

		Assert.AreEqual(42, foo.Int16s[1]);
	}
}
