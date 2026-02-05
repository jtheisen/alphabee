using System.Diagnostics;
using static Tests.Information.RamPerformance;

namespace Tests.Information;

[TestClass]
public class RamPerformance
{
	[TestMethod]
	public void Test()
	{
		IFixture[] cases = [
			new Fixture<Space4K, NullAccessor>(),
			new Fixture<Space4K, RandomAccessor>(),
			new Fixture<Space2M, RandomAccessor>(),
			new Fixture<Space1G, RandomAccessor>(),
			new Fixture<Space4K, LinearAccessor>(),
			new Fixture<Space2M, LinearAccessor>(),
			new Fixture<Space1G, LinearAccessor>()
			];

		foreach (var test in cases)
		{
			test.Test();
		}
	}

	public interface IFixture
	{
		void Test();
	}

	public class Fixture<SpaceT, AccessorT> : IFixture
		where SpaceT : struct, ISpace
		where AccessorT : struct, IAccessor
	{
		const int randomsSize = 4;
		const int N = 1 << 24;

		static SpaceT space = default;
		static AccessorT accessor = default;

		Byte[] mem;

		public Fixture()
		{
			mem = new Byte[space.Size];
		}

		public void Test()
		{
			var span = mem.AsSpan();

			GC.Collect();

			span.Clear();

			Span<int> randoms = stackalloc int[randomsSize];

			var mask = randomsSize - 1;

			randoms[0] = 1;

			for (var i = 1; i < randomsSize; ++i)
			{
				FillRandom(ref randoms[i & mask], randoms[(i - 1 + randomsSize) & mask]);
			}

			var sw = new Stopwatch();
			sw.Start();

			for (var i = 0; i < N; ++i)
			{
				ref var currentRandomValue = ref randoms[i & mask];

				accessor.Access(span, i % space.Size, currentRandomValue);

				//Console.WriteLine(String.Join(" ", randoms.ToArray().Select(v => v)));

				FillRandom(ref currentRandomValue, randoms[(i - 1 + randomsSize) & mask]);
			}

			sw.Stop();

			Console.WriteLine($"{typeof(AccessorT).Name} {typeof(SpaceT).Name} {sw.ElapsedMilliseconds}ms");
		}

		static void FillRandom(ref int target, int source)
		{
			var i = ((uint)source) * space.A + space.C;

			target = (int)(i % space.M);
		}
	}

	public interface ISpace
	{
		int Size { get; }

		uint M => (uint)Size - 1;

		uint A => 1103515245;

		uint C { get; }
	}

	public struct Space4K : ISpace
	{
		public int Size => 1 << 12;

		public uint C => 123;
	}

	public struct Space2M : ISpace
	{
		public int Size => 1 << 21;

		public uint C => 1013;
	}

	public struct Space1G : ISpace
	{
		public int Size => 1 << 30;

		public uint C => 12345;
	}

	public interface IAccessor
	{
		void Access(Span<Byte> mem, int step, int position);
	}

	public struct NullAccessor : IAccessor
	{
		public void Access(Span<Byte> mem, int step, int position)
		{
		}
	}

	public struct RandomAccessor : IAccessor
	{
		public void Access(Span<Byte> mem, int step, int position)
		{
			Trace.Assert(mem[position] == 0);
		}
	}

	public struct LinearAccessor : IAccessor
	{
		public void Access(Span<Byte> mem, int step, int position)
		{
			Trace.Assert(mem[step] == 0);
		}
	}
}
