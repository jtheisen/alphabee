using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AlphaBee;

public static class Extensions
{
	public static Span<T> InterpretAs<T>(this Span<Byte> span)
		where T : unmanaged
	{
		return MemoryMarshal.Cast<Byte, T>(span);
	}

	public static Span<Byte> Deinterpret<T>(this Span<T> span)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(span);
	}

	public static ReadOnlySpan<T> InterpretAs<T>(this ReadOnlySpan<Byte> span)
		where T : unmanaged
	{
		return MemoryMarshal.Cast<Byte, T>(span);
	}

	public static ReadOnlySpan<Byte> Deinterpret<T>(this ReadOnlySpan<T> span)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(span);
	}

	public static Boolean TryIndexOfBitCore(this Span<UInt64> words, UInt64 pattern, out Int32 i)
	{
		i = 0;

		foreach (var word in words)
		{
			var count = BitOperations.LeadingZeroCount(word ^ pattern);

			if (count < 64)
			{
				i += count;

				return true;
			}

			i += 64;
		}

		return false;
	}

	public static Boolean TryIndexOfBitZero(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(UInt64.MaxValue, out i);

	public static Boolean TryIndexOfBitOne(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(0, out i);

	public static Int32 IndexOfBitZero(this Span<UInt64> words)
		=> words.TryIndexOfBitZero(out var i) ? i : -1;

	public static Int32 TryIndexOfBitOne(this Span<UInt64> words)
		=> words.TryIndexOfBitOne(out var i) ? i : -1;
}

interface IPageLayout
{
	Int32 Size { get; }
}

interface IFieldPageLayout : IPageLayout
{
	Int32 HeaderSize { get; }
	Int32 FieldLength { get; }
}

struct FieldPageLayout4K<T> : IFieldPageLayout
{
	public Int32 Size => 4096;

	public Int32 HeaderSize => Size / Unsafe.SizeOf<UInt64>() / 8;

	public Int32 FieldLength => (Size - HeaderSize) / Unsafe.SizeOf<T>();
}

ref struct FieldPageHelper<T, L>
	where T : unmanaged
	where L : struct, IFieldPageLayout
{
	L layout;

	Span<T> content;
	Span<UInt64> housed;

	public FieldPageHelper(Span<Byte> page)
	{
		content = page[layout.HeaderSize..].InterpretAs<T>();
		housed = page[..layout.HeaderSize].InterpretAs<UInt64>();
	}

	public Boolean TryIndexOfVacant(out Int32 i)
	{
		return housed.TryIndexOfBitZero(out i);
	}

	public ref T Get(Int32 i)
	{
		return ref content[i];
	}
}
