using AlphaBee.StructLayouts;
using System.Runtime.InteropServices;

namespace AlphaBee.Information;

public struct Foo<T>
	where T : unmanaged
{
	public Boolean bool1;
	public Boolean bool2;
	public T value;
	public Bar<T> bar1;
	public Bar<int> bar2;
	public int int32;
	public long int64;
	private long intPrivate;
}

public struct Bar<T>
{
	public Boolean bool1;
	public T value;
	public int int32;
}

[TestClass]
public class StructOffsetsTests
{
	[TestMethod]
	public void TestOffsets()
	{
		Console.WriteLine(Marshal.OffsetOf<Foo<int>>(nameof(Foo<int>.bool2)));

		Foo<int> foo = default;

		Console.WriteLine(BitsAndBytes.Offset(ref foo, ref foo.bool1));
		Console.WriteLine(BitsAndBytes.Offset(ref foo, ref foo.bool2));

		Console.WriteLine(BitsAndBytes.GetFieldOffset(typeof(Foo<int>).GetField("bool1")!));
		Console.WriteLine(BitsAndBytes.GetFieldOffset(typeof(Foo<int>).GetField("bool2")!));
	}

	[TestMethod]
	public void TestLayouts()
	{
		Console.WriteLine(typeof(Foo<Int32>).GetLayoutFields().Stringify());
	}

}
