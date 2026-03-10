using System.Collections;

namespace AlphaBee;

[TestClass]
public class PeachTypeBaking
{
	[PeachLayout(typeof(SFoo))]
	public interface IFoo
	{
		Int32? NullableInt32 { get; set; }

		String? String { get; set; }

		IFoo? Foo { get; set; }

		Object? Untyped { get; set; }

		Object?[]? Items { get; set; }
	}

	public struct SFoo
	{
		public NullableStruct<Int32> NullableInt32;

		public Int64 String;

		public Int64 Foo;

		public Int64 Untyped;

		public Int64 Items;
	}

	public static IEnumerable<Object?[]> GetTestCases()
	{
		for (var i = 0; i < 2; ++i)
		{
			foreach (var property in typeof(IFoo).GetProperties())
			{
				yield return [ property.Name, i ];
			}
		}
	}

	PeachContext context = null!;

	[TestInitialize]
	public void Setup()
	{
		var typeRegistry = new PeachTypeRegistry(PeachTypeRegistry.Stage.Ready);

		typeRegistry.EnsureCanonicalImplementation(typeof(ITypeDescription));
		typeRegistry.EnsureCanonicalImplementation(typeof(IFoo));

		var storage = new TestStorage();

		context = new PeachContext(storage, typeRegistry);
	}

	[TestMethod]
	public void TestUntyped()
	{
		var foo = context.New<IFoo>();

		Assert.IsNull(foo.Untyped);

		var foo2 = context.New<IFoo>();

		foo2.String = "foo";

		foo.Untyped = foo2;

		var foo2b = foo.Untyped;

		Assert.AreEqual("foo", (foo2b as IFoo)?.String);
	}

	[TestMethod]
	public void TestScalar()
	{
		var foo = context.New<IFoo>();

		Assert.IsNull(foo.String);

		foo.String = "foo";

		Assert.AreEqual("foo", foo.String);

		var nested = foo.Foo = context.New<IFoo>();

		Assert.IsNull(foo.Foo.Foo);

		foo.Foo.String = "bar";

		Assert.AreEqual("bar", foo.Foo.String);
		Assert.AreEqual("foo", foo.String);

		var other = context.New<IFoo>();
		other.String = "baz";

		foo.Foo = other;
		Assert.AreEqual("baz", foo.Foo.String);
		Assert.AreEqual("bar", nested.String);
		Assert.AreEqual("foo", foo.String);
	}

	[TestMethod]
	public void TestCollection()
	{
		var foo = context.New<IFoo>();

		var foos = Enumerable.Range(1, 3)
			.Select(i => context.New<IFoo>(f => f.String = $"{i}"))
			.Cast<Object?>()
			.ToArray();

		Assert.AreEqual("2", (foos[1] as IFoo)?.String);

		foo.Items = foos;

		AssertEqual(foos, foo.Items);
	}

	static void AssertEqual(Object? expected, Object? actual)
	{
		Assert.AreEqual(expected?.GetType(), actual?.GetType());

		if (expected is ICollection e && actual is ICollection a)
		{
			Assert.AreEqual(e.Count, a.Count);

			foreach (var p in e.Cast<Object>().Zip(a.Cast<Object>()))
			{
				AssertEqual(p.First, p.Second);
			}
		}
		else
		{
			Assert.AreEqual(expected, actual);
		}
	}
}
