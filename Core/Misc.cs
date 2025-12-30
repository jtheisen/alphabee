using System.Reflection;
using System.Runtime.Intrinsics;

namespace AlphaBee;

public class InternalErrorException : Exception
{
	public InternalErrorException()
	{
	}

	public InternalErrorException(String message)
		: base(message)
	{
	}
}

public interface IBitArray
{
	Boolean IsAllOne { get; }
	Boolean IsAllZero { get; }

	Boolean GetBit(Int32 i);
	void SetBit(Int32 i, Boolean value);

	Int32 IndexOfBitCore(UInt64 pattern);

	Int32 IndexOfBitZero() => IndexOfBitCore(UInt64.MaxValue);
	Int32 IndexOfBitOne() => IndexOfBitCore(0ul);
}

public struct UInt64Array : IBitArray
{
	UInt64 value;

	public Boolean IsAllOne => value == UInt64.MaxValue;
	public Boolean IsAllZero => value == 0ul;

	public Boolean GetBit(Int32 i) => value.GetBit(i);
	public void SetBit(Int32 i, Boolean value) => value.SetBit(i, value);

	public Int32 IndexOfBitCore(UInt64 pattern) => value.IndexOfBitCore(pattern);
}

public struct Vector512BitArray : IBitArray
{
	Vector512<UInt64> value;

	public Boolean IsAllOne => value.IsAllOne();
	public Boolean IsAllZero => value.IsAllZero();

	public Boolean GetBit(Int32 i) => value.GetBit(i);
	public void SetBit(Int32 i, Boolean value) => value.SetBit(i, value);

	public Int32 IndexOfBitCore(UInt64 pattern) => value.IndexOfBitCore(pattern);
}

[InlineArray(8)]
public struct UInt512
{
	private ulong _element0;
}

public static class Extensions
{
	public static UInt64 AllocatePageNo(this Storage storage)
	{
		return storage.AllocatePageOffset() / storage.PageSize;
	}

	public static Span<Byte> GetPageSpanAtNo(this Storage storage, UInt64 pageNo)
	{
		return storage.GetPageSpanAtOffset(pageNo * storage.PageSize);
	}

	public static String GetCharPairAtNo(this Storage storage, UInt64 pageNo)
	{
		return storage.GetCharPairAtOffset(pageNo * storage.PageSize);
	}

	public static String GetCharPairAtOffset(this Storage storage, UInt64 offset)
	{
		var span = storage.GetPageSpanAtOffset(offset);

		var header = new PageHeader(ref span.InterpretAs<UInt64>()[0]);

		return header.PageCharPair;
	}

	public static String FormatAsDecimalLog(this Double value)
		=> $"1E{Math.Log10(value):#.00}";

	public static String FormatAsBinaryLog(this Double value)
		=> $"1B{Math.Log2(value):#.00}";

	public static Boolean IsOptimized(this Assembly assembly)
	{
		var attr = assembly.GetCustomAttribute<DebuggableAttribute>();

		return attr == null || !attr.IsJITOptimizerDisabled;
	}
}


[DebuggerDisplay("{ToString()}")]
public ref struct BitsWord
{
	ref UInt64 word;

	public BitsWord(ref UInt64 word)
	{
		this.word = ref word;
	}

	public override String ToString()
	{
		return word.ToBrailleString();
	}
}
