namespace AlphaBee.Collections;

//public interface BeeList<T> : IPeach
//	where T : unmanaged
//{
//	IValueBeeArray<T> items { get; set; }

//	public static Int32 DefaultCapacity => 64 / Unsafe.SizeOf<T>() + 1;

//	public T this[Int32 i]
//	{
//		get => items[i];
//		set => items[i] = value;
//	}

//	void EnsureIndex(Int32 i)
//	{
//		Debug.Assert(items is not null);

//		if (i >= items.Length)
//		{
//			var newArray = Context.NewValueArray<T>((i + 1).CeilToPowerOfTwo());

//			var oldItems = items.AsSpan();
//			var newItems = newArray.AsSpan();

//			oldItems[..Count].CopyTo(newItems[..Count]);
//			items = newArray;
//		}
//	}
//}
