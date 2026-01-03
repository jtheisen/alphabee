using System.Runtime.Intrinsics;

namespace AlphaBee;

public static class BitArray
{
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
