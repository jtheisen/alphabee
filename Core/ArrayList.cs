using System.Collections;

namespace AlphaBee;

public struct ArrayList<T> : IReadOnlyList<T>
{
	Int32 length = 0;
	T[] items;

	public static Int32 DefaultCapacity => 64 / Unsafe.SizeOf<T>() + 1;

	public ArrayList()
		: this(DefaultCapacity)
	{
	}

	public ArrayList(Int32 capacity)
	{
		items = new T[capacity];
	}

	public Int32 Capacity => items.Length;

	public Int32 Count => length;

	public void Clear()
	{
		length = 0;
	}

	public void Add(T item)
	{
		var i = length;
		EnsureIndex(i);
		++length;
		items[i] = item;
	}

	void ThrowOutOfRange(Int32 i)
	{
		throw new ArgumentOutOfRangeException($"{nameof(ArrayList<T>)} has no element #{i}");
	}

	void AssertInRange(Int32 i)
	{
		if (i < 0 || i >= length)
		{
			ThrowOutOfRange(i);
		}
	}

	public ref T At(Int32 i)
	{
		AssertInRange(i);

		Debug.Assert(items is not null);

		return ref items[i];
	}

	public T this[Int32 i]
	{
		get
		{
			AssertInRange(i);

			Debug.Assert(items is not null);

			return items[i];
		}
		set
		{
			EnsureIndex(i);

			items[i] = value;
		}
	}

	void EnsureIndex(Int32 i)
	{
		Debug.Assert(items is not null);

		if (i >= items.Length)
		{
			var copy = new T[(i + 1).CeilToPowerOfTwo()];
			items.AsSpan()[..length].CopyTo(copy.AsSpan()[..length]);
			items = copy;
		}
	}

	public IEnumerator<T> GetEnumerator() => items.Take(length).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
