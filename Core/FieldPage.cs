namespace AlphaBee;

public struct FieldPageLayout<T, I>
{
	public Int32 PageSize => Constants.PageSize32;

	public Int32 IndexSize => Unsafe.SizeOf<I>();
	public Int32 ItemSize => Unsafe.SizeOf<T>();
	public Int32 PaddedItemSize => 1 << Unsafe.SizeOf<T>().ToUInt64().Log2Ceil();

	public Int32 LeadSize => IndexSize * 4;

	public Int32 ContentSize => PageSize - LeadSize;

	public Int32 ContentBitSize => ContentSize * Constants.BitsPerByte;

	public Int32 FieldLength => ContentSize / PaddedItemSize;

	public UInt64 GetSpaceSizeForDepth(Int32 depth)
	{
		var result = (UInt64)ContentBitSize;

		for (var i = 0; i < depth; i++)
		{
			result *= (UInt64)FieldLength;
		}

		return result;
	}

	public UInt64 GetAddressSpaceSizeForDepth(Int32 depth)
	{
		return GetSpaceSizeForDepth(depth) * PageSize.ToUInt64();
	}
}

[DebuggerDisplay("{ToString()}")]
public ref struct FieldPage<T, I>
	where T : unmanaged
	where I : unmanaged
{
	public static readonly FieldPageLayout<T, I> layout;

	PageHeader header;
	ref I used;
	ref I full;
	Span<T> content;

	public Int32 IndexSize => Unsafe.SizeOf<I>();
	public Int32 PaddedItemSize => 1 << Unsafe.SizeOf<T>().ToUInt64().Log2Ceil();

	public UInt64 PageSize => Constants.PageSize;

	public FieldPageLayout<T, I> Layout => layout;

	public override String ToString()
	{
		return $"[{header.PageCharPair}|{used.ToBrailleString()}|{full.ToBrailleString()}]";
	}

	public FieldPage(Span<Byte> page)
	{
		//Debug.Assert();

		var bitArrays = page.InterpretAs<I>();

		content = page[layout.LeadSize..].InterpretAs<T>();
		header = new PageHeader(ref page.InterpretAs<UInt64>()[0]);
		used = ref bitArrays[1];
		full = ref bitArrays[2];
	}

	public void Init(PageType pageType, Int32 pageDepth)
	{
		header.Init(pageType, pageDepth);
		used = full = default;
	}

	public void Validate()
	{
		header.Validate();

		Debug.Assert(full.BitImplies(ref used));
	}

	public readonly ref T At(UInt64 i)
	{
		return ref content[i.ToInt32()];
	}

	public ref T ModifyAt(UInt64 i)
	{
		return ref content[i.ToInt32()];
	}

	public void SetFullBit(Int32 i, Boolean value)
	{
		Debug.Assert(i < layout.FieldLength);

		full.SetBit(i, value);
	}

	public Boolean GetFullBit(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		return full.GetBit(i);
	}

	public void SetUsedBit(Int32 i, Boolean value)
	{
		Debug.Assert(i < layout.FieldLength);

		used.SetBit(i, value);
	}

	public Boolean GetUsedBit(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		return used.GetBit(i);
	}

	public ref T Use(Int32 i, out Boolean unused)
	{
		Debug.Assert(i < layout.FieldLength);

		ref var item = ref content[i];

		unused = !GetUsedBit(i);

		if (unused)
		{
			SetUsedBit(i, true);

			item = default;
		}

		return ref item;
	}

	ref T Allocate(out Int32 i, out Boolean unused)
	{
		i = full.IndexOfBitZero();

		return ref Use(i, out unused);
	}

	public ref T AllocatePartially(out Int32 i, out Boolean unused)
	{
		return ref Allocate(out i, out unused);
	}

	public ref T AllocateFully(out Int32 i)
	{
		ref var item = ref Allocate(out i, out var unused);

		Debug.Assert(unused);

		SetFullBit(i, true);

		Validate();

		return ref item;
	}

	public Boolean IsFull => full.IndexOfBitZero() >= layout.FieldLength;

	public Boolean IsEmpty => used.IsAllZero();
}
