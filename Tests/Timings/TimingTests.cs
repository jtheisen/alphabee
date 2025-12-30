using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace AlphaBee.Timings;

public interface IAction
{
	void Run();
}

public struct Increment : IAction
{
	UInt64 value;

	public void Run() => ++value;
}

public struct IndexOf : IAction
{
	UInt64 value;

	public IndexOf()
	{
		value = 1ul << 32;
	}

	public void Run() => BitOperations.LeadingZeroCount(value);
}

public struct Vector512ExtractMostSignificantBits : IAction
{
	Vector512<UInt64> value;

	public void Run() => value.ExtractMostSignificantBits();
}

[InlineArray(8)]
public struct UInt64ByteArray
{
	private byte _element0;
}

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct U64Variant
{
	[FieldOffset(0)] public ulong U64;

	[FieldOffset(0)] public byte B0;
	[FieldOffset(1)] public byte B1;
	[FieldOffset(2)] public byte B2;
	[FieldOffset(3)] public byte B3;
	[FieldOffset(4)] public byte B4;
	[FieldOffset(5)] public byte B5;
	[FieldOffset(6)] public byte B6;
	[FieldOffset(7)] public byte B7;
}

public struct Vector512LeadingZeroCountManualLoop : IAction
{
	Vector512<UInt64> value;
	Int32 result;

	Int32 Calculate()
	{
		for (var i = 0; i < 8; ++i)
		{
			var j = BitOperations.LeadingZeroCount(value[i]);

			if (j < 64)
			{
				return (1 << i * 3) + j;
			}
		}

		return -1;
	}

	public void Run()
	{
		result = Calculate();
	}
}

public struct Vector512LeadingZeroCountManual0 : IAction
{
	Vector512<UInt64> value;

	public void Run()
	{
		UInt64ByteArray word = default;

		var w0 = BitOperations.LeadingZeroCount(value[0]);
		var w1 = BitOperations.LeadingZeroCount(value[1]);
		var w2 = BitOperations.LeadingZeroCount(value[2]);
		var w3 = BitOperations.LeadingZeroCount(value[3]);
		var w4 = BitOperations.LeadingZeroCount(value[4]);
		var w5 = BitOperations.LeadingZeroCount(value[5]);
		var w6 = BitOperations.LeadingZeroCount(value[6]);
		var w7 = BitOperations.LeadingZeroCount(value[7]);
	}
}

public struct Vector512LeadingZeroCountManual1 : IAction
{
	Vector512<UInt64> value;

	const Int32 mask = 0b1100_0000;

	public void Run()
	{
		UInt64ByteArray word = default;

		word[0] = (Byte)(mask | BitOperations.LeadingZeroCount(value[0]));
		word[1] = (Byte)(mask | BitOperations.LeadingZeroCount(value[1]));
		word[2] = (Byte)(mask | BitOperations.LeadingZeroCount(value[2]));
		word[3] = (Byte)(mask | BitOperations.LeadingZeroCount(value[3]));
		word[4] = (Byte)(mask | BitOperations.LeadingZeroCount(value[4]));
		word[5] = (Byte)(mask | BitOperations.LeadingZeroCount(value[5]));
		word[6] = (Byte)(mask | BitOperations.LeadingZeroCount(value[6]));
		word[7] = (Byte)(mask | BitOperations.LeadingZeroCount(value[7]));
	}
}

public struct Vector512LeadingZeroCountManual2 : IAction
{
	Vector512<UInt64> value;
	Int32 result;

	const Int32 mask = 0b1100_0000;

	public void Run()
	{
		UInt64ByteArray word = default;

		word[0] = (Byte)(mask | BitOperations.LeadingZeroCount(value[0]));
		word[1] = (Byte)(mask | BitOperations.LeadingZeroCount(value[1]));
		word[2] = (Byte)(mask | BitOperations.LeadingZeroCount(value[2]));
		word[3] = (Byte)(mask | BitOperations.LeadingZeroCount(value[3]));
		word[4] = (Byte)(mask | BitOperations.LeadingZeroCount(value[4]));
		word[5] = (Byte)(mask | BitOperations.LeadingZeroCount(value[5]));
		word[6] = (Byte)(mask | BitOperations.LeadingZeroCount(value[6]));
		word[7] = (Byte)(mask | BitOperations.LeadingZeroCount(value[7]));

		var byteIndex = BitOperations.LeadingZeroCount(word.AsUInt64s()[0]);

		result = (word[byteIndex] & ~mask) << 64 * byteIndex;
	}
}

public struct Vector512LeadingZeroCount : IAction
{
	Vector512<UInt64> value;

	public void Run() => Avx10v1.V512.LeadingZeroCount(value);
}

public struct TimingHelper<ActionT>
	where ActionT : IAction
{
}

[TestClass]
public class TimingTests
{
	[TestMethod]
	public void TimeIncrement()
	{
		RunTimed<Increment>(8);
		RunTimed<IndexOf>(8);
		RunTimed<Vector512ExtractMostSignificantBits>(8);
		RunTimed<Vector512LeadingZeroCountManualLoop>(8);
		RunTimed<Vector512LeadingZeroCountManual0>(8);
		RunTimed<Vector512LeadingZeroCountManual1>(8);
		RunTimed<Vector512LeadingZeroCountManual2>(8);
	}

	void RunTimed<ActionT>(Int32 countLog2)
		where ActionT : IAction, new()
	{
		RunTimed(new ActionT(), countLog2);
	}

	void RunTimed<ActionT>(ActionT action, Int32 countLog2)
		where ActionT : IAction
	{
		var stopwatch = new Stopwatch();

		stopwatch.Start();

		var count = 1ul << countLog2;

		for (var i = count; i > 0; i--)
		{
			action.Run();
		}

		stopwatch.Stop();

		var cost = 1.0 * stopwatch.Elapsed.TotalSeconds / count;

		Console.WriteLine($"{typeof(ActionT).Name}:\n  {cost.FormatAsBinaryLog()}");
	}


}
