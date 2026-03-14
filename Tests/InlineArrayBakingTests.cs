namespace AlphaBee;

[TestClass]
public class InlineArrayBakingTests
{
	public interface IFoo
	{
		Int32 IntBefore { get; set; }

		[InlineSpan(3)]
		public Span<Int16> Int16s { get; }

		Int32 IntAfter { get; set; }
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

		foo.IntBefore = 1;
		foo.IntAfter = 2;

		Assert.AreEqual(1, foo.IntBefore);
		Assert.AreEqual(2, foo.IntAfter);

		Assert.AreEqual(3, foo.Int16s.Length);

		foo.Int16s[1] = 42;

		Assert.AreEqual(0, foo.Int16s[0]);
		Assert.AreEqual(42, foo.Int16s[1]);
		Assert.AreEqual(0, foo.Int16s[2]);

		Assert.AreEqual(1, foo.IntBefore);
		Assert.AreEqual(2, foo.IntAfter);

		Assert.ThrowsException<IndexOutOfRangeException>(() => foo.Int16s[3]);
	}
}
