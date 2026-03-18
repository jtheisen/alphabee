namespace AlphaBee.Collections;

public interface IPeachyBtreePageConfig
{
	Int32 N { get; }
}

public interface IPeachyBtreePage<TKey, TValue, TConfig>
	where TKey : unmanaged, IComparable<TKey>
	where TValue : unmanaged
	where TConfig : struct, IPeachyBtreePageConfig
{
	static TConfig C => default;

	Int32 N { get; set; }

	Span<TKey> Keys { get; }

	Span<TValue> Values { get; }

	Boolean Insert(TKey key, TValue value)
	{
		if (N == C.N)
		{
			return false;
		}
		else
		{
			var i = Keys.BinarySearch(key);

			var p = i >= 0 ? i : ~i;

			Keys[p..^1].CopyTo(Keys[(p + 1)..]);

			Keys[p] = key;

			// FIXME

			return true;
		}
	}


}
