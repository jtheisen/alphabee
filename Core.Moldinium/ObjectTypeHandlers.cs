using AlphaBee.Utilities;
using System.Collections;
using System.Collections.Immutable;
using System.Net;

namespace AlphaBee;

public interface IObjectTypeHandler
{
	Type? Type { get; }

	TypeRef TypeRef { get; }

	Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address);

	void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address);
}

public struct UnimplementedTypeHandler : IObjectTypeHandler
{
	public Type? Type => null;

	public TypeRef TypeRef => throw new NotImplementedException();

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 offset)
	{
		throw new NotImplementedException();
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		throw new NotImplementedException();
	}
}

public struct Ucs2StringTypeHandler : IObjectTypeHandler
{
	public Type? Type => typeof(String);

	public TypeRef TypeRef => new TypeRef(new TypeByte(TypeCode.String));

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		ref var header = ref storage.GetObject(address, out var content);

		var chars = storage.GetSpan<Char>(address + ObjectHeader.Size, header.size / 2);

		return new String(chars);
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		var value = (String)untyped;

		var header = new ObjectHeader(TypeRef, value.Length * 2);

		storage.AllocateObject(header, out address, out var target);

		var chars = target.InterpretAs<Char>();

		value.CopyTo(chars);
	}
}

public struct StructArrayTypeHandler<T> : IObjectTypeHandler
	where T : unmanaged
{
	public Type? Type => typeof(T).MakeArrayType();

	public TypeRef TypeRef => new TypeRef(new TypeByte(Type.GetTypeCode(typeof(T)), isSpan: true));

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		ref var header = ref storage.GetObject(address, out var content);

		var bytes = storage.GetSpan<Byte>(address + ObjectHeader.Size, header.size);

		return bytes.ToArray();
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		var value = (Byte[])untyped;

		var header = new ObjectHeader(TypeRef, value.Length);

		storage.AllocateObject(header, out address, out var target);

		value.CopyTo(target);
	}
}

//public static class LazyObjectList
//{
//	public static Object Create(Type type, Int64[] addresses, AbstractPeachContext context)
//	{
//		return typeof(LazyObjectList<>).MakeGenericType(type).CreateInstance<Object>(addresses, context);
//	}
//}

//public class LazyObjectList<T> : IReadOnlyList<T?>
//	where T : class
//{
//	private readonly Int64[] addresses;
//	private readonly AbstractPeachContext context;

//	public LazyObjectList(Int64[] addresses, AbstractPeachContext context)
//	{
//		this.addresses = addresses;
//		this.context = context;
//	}

//	public T? this[Int32 index] => context.GetObject(addresses[index]) as T;

//	public Int32 Count => addresses.Length;

//	public IEnumerator<T?> GetEnumerator()
//	{
//		for (var i = 0; i < Count; ++i)
//		{
//			yield return this[i];
//		}
//	}

//	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//}

public struct ObjectArrayTypeHandler : IObjectTypeHandler
{
	public Type? Type => typeof(Object?[]);

	public TypeRef TypeRef => new TypeRef(new TypeByte(TypeCode.Object, isSpan: true, isNullable: true));

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		ref var header = ref storage.GetObject(address, out var content);

		var n = header.size / 8;

		var addresses = storage.GetSpan<Int64>(address + ObjectHeader.Size, n);

		var array = new Object?[n];

		for (var i = 0; i < n; ++i)
		{
			array[i] = context.GetObject(address + ObjectHeader.Size + i * 8);
		}

		return array;
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		var array = (Object[])untyped;

		var n = array.Length;

		var header = new ObjectHeader(TypeRef, n * 8);

		storage.AllocateObject(header, out address, out _);

		for (var i = 0; i < n; ++i)
		{
			var item = array[i];

			context.SetObject(address + ObjectHeader.Size + i * 8, item);
		}
	}
}
