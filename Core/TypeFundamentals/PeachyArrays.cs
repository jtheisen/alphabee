
using System.Reflection;

namespace AlphaBee;

public interface IBeeArray<T> : IPeach
{
	static readonly Int32 ArrayLevel;

	static readonly Type ImplementationType;

	static readonly Type BaseType;

	static IBeeArray()
	{
		var itemType = typeof(T);

		if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(IBeeArray<>))
		{
			var arrayLevelField
				= itemType.GetField(nameof(ArrayLevel), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			var baseTypeField
				= itemType.GetField(nameof(BaseType), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			Trace.Assert(arrayLevelField is not null);
			Trace.Assert(baseTypeField is not null);

			var nestedArrayLevel = arrayLevelField.GetValue(null);
			var baseType = baseTypeField.GetValue(null);

			Trace.Assert(nestedArrayLevel is not null);
			Trace.Assert(baseType is not null);

			ArrayLevel = (Int32)nestedArrayLevel + 1;
			BaseType = (Type)baseType;
		}
		else
		{
			ArrayLevel = 1;
			BaseType = itemType;
		}

		if (itemType.IsValueType)
		{
			ImplementationType = typeof(ValueBeeArrayImplementation<>).MakeGenericType(itemType);
		}
		else
		{
			ImplementationType = typeof(ReferenceBeeArrayImplementation<>).MakeGenericType(itemType);
		}
	}

	T this[Int32 i] { get; set; }

	Int32 Length { get; }
}

public interface IBeeArrayInternal<T> : IBeeArray<T>, IPeachMixin
{
	public (ObjectHeader, ArrayHeader) GetHeaders(TypeNo typeNo, Int32 length)
	{
		return new(new(typeNo, new(length * Unsafe.SizeOf<T>())), new(length, ArrayLevel));
	}
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

	Span<T> IValueBeeArray<T>.AsSpan() => Context.GetValueArrayObjectOld<T>(Address, out _);
}

public interface IValueBeeArrayImplementation<T> : IValueBeeArray<T>, IPeachMixin
	where T : unmanaged
{
	T IBeeArray<T>.this[Int32 i] { get => AsSpan()[i]; set => AsSpan()[i] = value; }

	TypeNo IPeach.ImplementationTypeNo => throw new NotImplementedException();

	Span<T> IValueBeeArray<T>.AsSpan() => Context.GetValueArrayObjectOld<T>(Address, out _);
}



public struct ReferenceBeeArrayImplementation<TValue> : IBeeArray<TValue?>, IBeeArrayInternal<TValue?>
	where TValue : class
{
	public PeachConnection Connection { get; set; }

	public Int64 Address => Connection.Address;
	public PeachContext Context => Connection.Context;

	public void Init(PeachContext context, Int64 address) => Connection = new(context, address);

	Span<Int64> AsSpan() => Context.GetArraySpan<Int64>(Address);

	ref Int64 At(Int32 i) => ref AsSpan()[i];

	public TValue? this[Int32 i] { get => (TValue?)Context.GetObject(At(i)); set => Context.SetObjectToAddress(ref At(i), value); }

	public Int32 Length => Context.GetArrayHeader(Address).Length;

	public TypeNo ImplementationTypeNo => throw new NotImplementedException();
}

public struct ValueBeeArrayImplementation<T> : IValueBeeArrayImplementation<T>, IBeeArrayInternal<T>
	where T : unmanaged
{
	public PeachConnection Connection { get; set; }

	public Int64 Address => Connection.Address;
	public PeachContext Context => Connection.Context;

	public void Init(PeachContext context, Int64 address) => Connection = new(context, address);

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

