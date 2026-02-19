using AlphaBee;
using System;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

namespace Tests.Information;

[TestClass]
public class RamPerformance
{
	static Type[] Actions = [
		typeof(NoAction),
		typeof(TrivialAction),
		typeof(SqrtAction),
		//typeof(CpuidAction)
	];

	static Type[] Allocators = [
		typeof(SimpleAllocator),
		typeof(LargePageAllocator),
	];

	static Int32[] Sizes = [12, 21, 30];

	IEnumerable<IFixture> GetFixtures()
	{
		foreach (Type action in Actions)
		{
			yield return CreateFixture(typeof(SimpleAllocator), typeof(LinearPreparer), action, 12);
		}

		foreach (Type action in Actions)
		{
			yield return CreateFixture(typeof(SimpleAllocator), typeof(RandomPreparer), action, 12);
		}

		foreach (Type allocator in Allocators)
		{
			foreach (Int32 size in Sizes)
			{
				yield return CreateFixture(allocator, typeof(RandomPreparer), typeof(NoAction), size);
			}
		}
	}

	[TestMethod]
	public void TestLargePages()
	{
		Assert.IsTrue(LargePages.Enable());

		var range = LargePages.AllocLargePages(1 << 26);

		var span = range.AsSpan<Byte>();

		span.Clear();
	}

	[TestMethod]
	public void Test()
	{
		LargePages.Enable();

		foreach (var test in GetFixtures())
		{
			test.Test(out _);
		}
	}

	public IFixture CreateFixture(Type allocator, Type preparer, Type action, Int32 length)
	{
		var type = typeof(Fixture<,,>).MakeGenericType(allocator, preparer, action);

		return Activator.CreateInstance(type, [ length ]) as IFixture ?? throw new Exception("Could not create fixture");
	}

	public interface IFixture
	{
		void Test(out Int32 dummy);

		int Size { get; }

		int LineSize => 64;

		int LineCount => Size / LineSize;

		int WordSize => Unsafe.SizeOf<int>();

		int WordsPerLine => LineSize / WordSize;

		int WordCount => Size / WordSize;

		int MemSize => 1 << 30;

		int MemWordCount => MemSize / WordSize;

		int MemLineCount => MemSize / LineSize;
	}

	public class Fixture<AllocatorT, PreparerT, ActionT> : IFixture
		where AllocatorT : struct, IAllocator
		where PreparerT : struct, IPreparer
		where ActionT : struct, IAction
	{
		static ActionT action = default;
		static PreparerT preparer = default;

		IFixture Self => this;

		public int Size { get; }

		public Fixture(Int32 sizeLog2)
		{
			Size = 1 << sizeLog2;
		}

		public void Test(out Int32 dummy)
		{
			var allocator = default(AllocatorT);

			var bytes = allocator.Allocate(Self.MemSize);

			var span = bytes.InterpretAs<Int32>();

			span.Clear();

			Prepare(span);

			GC.Collect();

			var sw = new Stopwatch();
			sw.Start();

			var runLength = 1 << 23;

			if (action.DontUseAction)
			{
				dummy = Run(span, runLength);
			}
			else
			{
				dummy = RunWithAction(span, runLength);
			}

			sw.Stop();

			Console.WriteLine($"{typeof(AllocatorT).Name} {typeof(PreparerT).Name} {typeof(ActionT).Name} {Size} {sw.ElapsedMilliseconds}ms");
		}

		void Prepare(Span<int> span)
		{
			var i = 0;

			do
			{
				var nextI = i + Self.WordCount;

				if (nextI > Self.MemWordCount - Self.WordCount)
				{
					nextI = 0;
				}

				preparer.Prepare(span[i..(i + Self.WordCount)], i, nextI, this);

				i = nextI;
			}
			while (i != 0);

			var checkCount = Count(span);

			Trace.Assert(checkCount == Self.MemLineCount);
		}

		void DebugPrintSpace(Span<Int32> span)
		{
			Console.WriteLine("-- span");
			for (var i = 0; i < span.Length; ++i)
			{
				if (i % 16 == 0)
				{
					Console.WriteLine();
					Console.Write($"{i / 16}:");

					if (i > 66 * 16)
					{
						Console.WriteLine("...");

						return;
					}
				}
				Console.Write($" {span[i] / 16}");
			}
			Console.WriteLine();
		}

		Int32 Count(Span<Int32> span)
		{
			Int32 i = 0, count = 0;
			do
			{
				i = span[i];
				count++;
			}
			while (i > 0);
			return count;
		}

		Int32 Run(Span<Int32> span, Int32 count)
		{
			Int32 i = 0;
			do
			{
				i = span[i];
			}
			while (--count > 0);

			return i;
		}

		Int32 RunWithAction(Span<Int32> span, Int32 count)
		{
			Int32 a = 0;
			Int32 i = 0;
			do
			{
				a = action.Do(i, a);
				i = span[i];
			}
			while (--count > 0);

			return a;
		}
	}

	public interface IPreparer
	{
		void Prepare(Span<int> span, Int32 offsetI, Int32 nextI, IFixture fixture);
	}

	public struct RandomPreparer : IPreparer
	{
		public void Prepare(Span<Int32> span, Int32 offset, Int32 next, IFixture fixture)
		{
			var lineCount = fixture.LineCount;

			var random = new Random();

			var remainingLines = Enumerable.Range(1, lineCount - 1).ToArray();

			var remainingLinesCount = lineCount - 1;

			var currentI = 0;

			do
			{
				var p = random.Next(remainingLinesCount);
				var i = remainingLines[p];
				remainingLines[p] = remainingLines[remainingLinesCount - 1];
				--remainingLinesCount;

				var nextI = i * fixture.WordsPerLine;

				span[currentI] = offset + nextI;

				currentI = nextI;
			}
			while (remainingLinesCount > 0);

			span[currentI] = next;
		}
	}

	public struct LinearPreparer : IPreparer
	{
		public void Prepare(Span<Int32> span, Int32 offset, Int32 next, IFixture fixture)
		{
			for (var i = 0; i < span.Length; i += fixture.WordsPerLine)
			{
				span[i] = offset + (i + fixture.WordsPerLine) % fixture.WordCount;
			}

			span[^fixture.WordsPerLine] = next;
		}
	}

	public interface IAllocator
	{
		Span<Byte> Allocate(Int32 size);
	}

	public struct LargePageAllocator : IAllocator
	{
		public Span<Byte> Allocate(Int32 size)
		{
			Assert.IsTrue(LargePages.Enable());

			var range = LargePages.AllocLargePages((nuint)size);

			return range.AsSpan<Byte>();
		}
	}

	public struct SimpleAllocator : IAllocator
	{
		public Span<Byte> Allocate(Int32 size)
		{
			return new Byte[size].AsSpan();
		}
	}

	public interface IAction
	{
		Int32 Do(Int32 value, Int32 a);

		Boolean DontUseAction => false;
	}

	public struct NoAction : IAction
	{
		public Boolean DontUseAction => true;

		public Int32 Do(Int32 value, Int32 a) => value;
	}

	public struct TrivialAction : IAction
	{
		public Int32 Do(Int32 value, Int32 a) => value;
	}

	public struct SqrtAction : IAction
	{
		public Int32 Do(Int32 value, Int32 a) => (Int32)Math.Sqrt(value);
	}

	public struct CpuidAction : IAction
	{
		public Int32 Do(Int32 value, Int32 a) => X86Base.CpuId(0, 0).Eax;
	}
}
