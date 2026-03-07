using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBee.Information;

[TestClass]
public class LineArrayTests
{
	public interface INumber
	{
		static abstract Int32 Value { get; }
	}

	public struct Number42 : INumber
	{
		static Int32 INumber.Value => 42;
	}

	//public ref struct Foo<T>
	//{
	//	public static ref T Pass(ref T value) => ref value;
	//	public ref T Some() => ref value;

	//	[System.Runtime.CompilerServices.InlineArray(42)]
	//	public struct InlineArray
	//	{
	//		private T _element0;
	//	}

	//	InlineArray array;

	//	public ref T this[Int32 i]
	//	{
	//		get => ref Some();
	//	}

	//}
}
