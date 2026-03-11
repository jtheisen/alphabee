namespace AlphaBee;

public interface IArrayPeach<T> : IPeach
{
	T this[Int32 i] { get; set; }

	Span<T> AsSpan();
}

public struct ArrayPeach<T> : IArrayPeach<T>
{
	Int64 address;
	AbstractPeachContext context;

	T IArrayPeach<T>.this[Int32 i] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	TypeNo IPeach.ImplementationTypeNo => throw new NotImplementedException();

	AbstractPeachContext IPeachMixin.Context => context;

	Int64 IPeachMixin.Address => address;

	Span<T> IArrayPeach<T>.AsSpan()
	{
		

		throw new NotImplementedException();
	}

	void IPeachMixin.Init(AbstractPeachContext context, Int64 address)
	{
		this.context = context;
		this.address = address;
	}
}