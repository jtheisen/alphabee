namespace AlphaBee.Collections;

//public interface PersistedList<T> : IPeach
//{
//	Int32 length { get; }

//	IArrayPeach<T> items { get; set; }

//	public static Int32 DefaultCapacity => 64 / Unsafe.SizeOf<T>() + 1;

//	void EnsureIndex(Int32 i)
//	{
//		Debug.Assert(items is not null);

//		if (i >= items.Length)
//		{
//			var copy = new T[(i + 1).CeilToPowerOfTwo()];
//			items.AsSpan()[..length].CopyTo(copy.AsSpan()[..length]);
//			items = Context.New<IArrayPeach<T>>();
//		}
//	}



//}
