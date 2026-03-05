using System.Diagnostics;

namespace AlphaBee;

public struct ArrayList<T>
{
	Int32 length;
	T[] items;

	public ArrayList(Int32 capacity)
	{
		items = new T[capacity];
	}

	public void Add(T item)
	{
		var i = length + 1;
		EnsureIndex(i);
		items[length] = item;
	}

	public ref T At(Int32 i)
	{
		Debug.Assert(i >= 0 && i < length);

		return ref items[i];
	}

	public T this[Int32 i]
	{
		get
		{
			Debug.Assert(i >= 0 && i < length);

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
		while (i >= items.Length)
		{
			var copy = new T[items.Length * 2];
			items.CopyTo(copy, 0);
			items = copy;
		}
	}
}
