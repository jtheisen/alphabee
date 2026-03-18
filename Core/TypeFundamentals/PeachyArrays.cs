
namespace AlphaBee;

public interface IBeeArray<T> : IPeach
{
	T this[Int32 i] { get; set; }

	Int32 Length { get; }
}

public interface IValueBeeArray<T> : IBeeArray<T>
	where T : unmanaged
{
	Span<T> AsSpan();
}

public interface ValueBeeArray<T> : IValueBeeArray<T>
	where T : unmanaged
{
	T IBeeArray<T>.this[Int32 i] { get => AsSpan()[i]; set => AsSpan()[i] = value; }

	Span<T> IValueBeeArray<T>.AsSpan() => Context.GetValueArrayObject<T>(Address, out _);
}

public interface IValueBeeArrayImplementation<T> : IValueBeeArray<T>, IPeachMixin
	where T : unmanaged
{
	T IBeeArray<T>.this[Int32 i] { get => AsSpan()[i]; set => AsSpan()[i] = value; }

	TypeNo IPeach.ImplementationTypeNo => throw new NotImplementedException();

	Span<T> IValueBeeArray<T>.AsSpan() => Context.GetValueArrayObject<T>(Address, out _);
}

public struct ValueBeeArrayImplementation<T> : IValueBeeArrayImplementation<T>
		where T : unmanaged
{
	public PeachConnection Connection { get; set; }

	public Int32 Length { get; set; }
}

//public struct BeeArray<T>
//{
//	private readonly IBeeArray<T> implementation;
//	private readonly Int32 start;
//	private readonly Int32 length;

//	public BeeArray(IBeeArray<T> implementation, Int32 start, Int32 length)
//	{
//		this.implementation = implementation;
//		this.start = start;
//		this.length = length;
//	}

//	public Int32 Length => length;

//	void CopyTo(IBeeArray<T> target);

//}

