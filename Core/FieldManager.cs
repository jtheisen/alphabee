namespace AlphaBee;

//public ref struct FieldManager<T, I> : IPageAllocator
//	where T : unmanaged
//	where I : unmanaged, IBitArray
//{
//	ref HeaderCore header;

//	Storage storage;

//	public FieldManager(Storage storage)
//	{
//		this.storage = storage;

//		this.header = ref storage.HeaderPage.HeaderCore;
//	}

//	public void Init()
//	{
//	}

//	public void Deallocate(UInt64 offset)
//	{
//		var field = CreateField();

//		field.Deallocate(offset);
//	}

//	Field<T, I, Vector512BitArray, FieldManager<T, I>> CreateField()
//	{
//		return new Field<T, I, Vector512BitArray, FieldManager<T, I>>(this);
//	}

//	public ref T Allocate(out UInt64 offset)
//	{
//		var field = CreateField();

//		return ref field.Allocate(out offset);
//	}

//	public Span<Byte> GetPageSpanAtOffset(UInt64 offset)
//	{
//		return storage.GetPageSpanAtOffset(offset);
//	}

//	public UInt64 AllocatePageOffset()
//	{
//		return storage.AllocatePageOffset();
//	}

//	public void AssertAllocatedPageIndex(UInt64 index)
//	{
//		throw new NotImplementedException();
//	}

//	public ref TreeRoot Root => throw new NotImplementedException();

//	public Boolean IsPageManagerBitField => false;

//	public Span<Byte> AllocatePageSpan() => GetPageSpanAtOffset(AllocatePageOffset());
//}
