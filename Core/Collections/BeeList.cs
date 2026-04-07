using System.Collections;

namespace AlphaBee.Collections;

//public interface IBeeList<T> : IReadOnlyCollection<T>
//{
//	T this[Int32 i] { get;set; }

//	void Clear();

//	void Add(T value);
//}

public interface BeeList<T> : IPeach //: IBeeList<T>, IPeach
{
	IBeeArray<T> items { get; set; }

	public Int32 count { get; set; }

	//static Int32 DefaultCapacity => 64 / Unsafe.SizeOf<T>() + 1;

	//Int32 IReadOnlyCollection<T>.Count => count;

	//T IBeeList<T>.this[Int32 i]
	//{
	//	get => items[i];
	//	set => items[i] = value;
	//}

	//void IBeeList<T>.Clear() => count = 0;

	//void IBeeList<T>.Add(T value)
	//{
	//	EnsureIndex(count);
	//	items[count] = value;
	//}

	//void EnsureIndex(Int32 i)
	//{
	//	Debug.Assert(items is not null);

	//	if (i >= items.Length)
	//	{
	//		var newItems = Context.NewArray<T>((i + 1).CeilToPowerOfTwo());

	//		items.CopyTo(newItems);

	//		items = newItems;
	//	}
	//}

	//public record struct Enumerator(BeeList<T> list) : IEnumerator<T>
	//{
	//	Int32 i;

	//	Object IEnumerator.Current => list[i]!;

	//	public T Current => list[i];

	//	public void Dispose() { }

	//	public Boolean MoveNext() => ++i == list.Count;

	//	public void Reset() => i = 0;
	//}

	//public new Enumerator GetEnumerator() => new(this);

	//IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

	//IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
