using System.Diagnostics;

namespace Moldinium.Baking;

[TestClass]
public class UnificationTests
{
	public interface ITest;

	public interface IExpectFailure;

	public interface ITest<SourceT, TargetT, ExpectedT> : ITest
		where SourceT : allows ref struct
		where TargetT : allows ref struct
		;

	public class TestSimple<T>
		: ITest<T, String, String>;

	public class TestNullable<T> : ITest<T?, Int32?, Int32>
		where T : struct
		;

	public class TestPairsSimple<T> : ITest<(T, Int32), (Int32, Int32), Int32>;

	public class TestPairsMatching<T> : ITest<(T, T), (Int32, Int32), Int32>;

	public class TestPairsMismatch<T> : ITest<(T, T), (Int16, Int32), Int32>, IExpectFailure;

	public class TestList<T>
		: ITest<List<T>, List<Int32>, Int32>;

	public class TestSpan<T>
		: ITest<Span<T>, Span<Int32>, Int32>;

	// Not yet supported or needed
	//public class TestArray<T>
	//	: ITest<T[], Int32[], Int32>;

	public record class Testcase(String Name, Type Test)
	{
		public override String ToString()
		{
			return Name;
		}
	}

	public static String TrimGenericTypeArgumentNumberEncoding(String typeName)
	{
		var i = typeName.LastIndexOf('`');

		return i < 0 ? typeName : typeName.Substring(0, i);
	}

	public static IEnumerable<Object?[]> GetTestCases()
	{
		var testClasses = typeof(UnificationTests).GetNestedTypes();

		foreach (var type in testClasses)
		{
			if (type.IsAssignableTo(typeof(ITest)) && !type.IsInterface)
			{
				var name = TrimGenericTypeArgumentNumberEncoding(type.Name.StripPrefix("Test"));

				yield return [new Testcase(name, type)];
			}
		}
	}

	[DataTestMethod]
	[DynamicData(nameof(GetTestCases), DynamicDataSourceType.Method)]
	public void TestUnify(Testcase testCase)
	{
		var testType = testCase.Test;

		var arg = testType.GetGenericArguments()[0];

		var pairType = testType.GetInterfaces().Single(i => i != typeof(ITest) && i != typeof(IExpectFailure));

		Trace.Assert(pairType.GetGenericTypeDefinition() == typeof(ITest<,,>));

		var args = pairType.GetGenericArguments();

		Trace.Assert(args.Length == 3);

		var expectFailure = testType.IsAssignableTo(typeof(IExpectFailure));

		var source = args[0];
		var target = args[1];
		var expected = args[2];

		if (expectFailure)
		{
			var result = Unification.Unify(source, target, out _);

			Assert.IsFalse(result);
		}
		else
		{
			var result = Unification.UnifyForSpecificArgument(source, target, arg);

			Assert.AreEqual(expected, result);
		}
	}
}
