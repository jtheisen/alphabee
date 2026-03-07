using AlphaBee.Utilities;
using System.Collections;
using System.Collections.Immutable;
using System.Net;

namespace AlphaBee;

public interface IObjectTypeHandler
{
	Type? Type { get; }

	TypeNo TypeNo { get; }

	Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address);

	void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address);
}

public struct UnimplementedTypeHandler : IObjectTypeHandler
{
	public Type? Type => null;

	public TypeNo TypeNo => throw new NotImplementedException();

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

	public TypeNo TypeNo => new TypeNo(new TypeByte(TypeCode.String));

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		var chars = storage.GetArrayObject<Char>(address);

		return new String(chars);
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		var value = (String)untyped;

		var header = ObjectHeader.CreateForStruct<Char>(TypeNo, value.Length);

		storage.AllocateArrayObject<Char>(header, out address, out var chars);

		value.CopyTo(chars);
	}
}

public struct StructArrayTypeHandler<T> : IObjectTypeHandler
	where T : unmanaged
{
	public Type? Type => typeof(T).MakeArrayType();

	public TypeNo TypeNo => new TypeNo(new TypeByte(typeof(T).GetTypeCode(), isSpan: true));

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		var items = storage.GetArrayObject<T>(address);

		return items.ToArray();
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		var value = (T[])untyped;

		var header = ObjectHeader.CreateForStruct<T>(TypeNo, value.Length);

		storage.AllocateArrayObject<T>(header, out address, out var items);

		value.CopyTo(items);
	}
}

public struct ObjectArrayTypeHandler : IObjectTypeHandler
{
	public Type? Type => typeof(Object?[]);

	public TypeNo TypeNo => new TypeNo(new TypeByte(TypeCode.Object, isSpan: true, isNullable: true));

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		var addresses = storage.GetArrayObject<Int64>(address);

		var n = addresses.Length;

		var array = new Object?[n];

		for (var i = 0; i < n; ++i)
		{
			var itemAddress = addresses[i];

			array[i] = context.GetObject(itemAddress);
		}

		return array;
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		var items = (Object[])untyped;

		var n = items.Length;

		var header = new ObjectHeader(TypeNo, n * 8);

		storage.AllocateObject(header, out address, out _);

		for (var i = 0; i < n; ++i)
		{
			var item = items[i];

			context.SetObject(address + ObjectHeader.Size + i * 8, item);
		}
	}
}
