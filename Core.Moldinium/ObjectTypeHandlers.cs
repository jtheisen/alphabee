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

	static virtual IObjectTypeHandler GetHandler(TypeByte typeByte) => throw new NotImplementedException();
}

public interface IHandlerGetter
{
	IObjectTypeHandler GetHandler(TypeByte typeByte);

	static IObjectTypeHandler Get(Type type, TypeByte typeByte)
	{
		return typeof(HandlerProvider<>)
			.MakeGenericType(type)
			.CreateInstance<IHandlerGetter>()
			.GetHandler(typeByte);
	}
}

public struct HandlerProvider<HandlerT> : IHandlerGetter
	where HandlerT : IObjectTypeHandler
{
	public IObjectTypeHandler GetHandler(TypeByte typeByte)
	{
		return HandlerT.GetHandler(typeByte);
	}
}

public interface ISingletonObjectTypeHandler<T> : IObjectTypeHandler
	where T : IObjectTypeHandler, new()
{
	private static IObjectTypeHandler? instance;

	static IObjectTypeHandler IObjectTypeHandler.GetHandler(TypeByte _)
	{
		return instance ?? (instance = new T());
	}
}

public struct UnimplementedMiscTypeHandler : IObjectTypeHandler
{
	public Type? Type { get; }

	public TypeNo TypeNo { get; }

	public UnimplementedMiscTypeHandler(TypeByte typeByte, Type type)
	{
		TypeNo = new TypeNo(typeByte);
		Type = type;
	}

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 address)
	{
		throw new NotImplementedException();
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		throw new NotImplementedException();
	}
}

public struct UnimplementedSupportedStructTypeHandler : IObjectTypeHandler
{
	public Type? Type { get; }

	public TypeNo TypeNo { get; }

	static IObjectTypeHandler IObjectTypeHandler.GetHandler(TypeByte typeByte)
	{
		return new UnimplementedSupportedStructTypeHandler(typeByte);
	}

	public UnimplementedSupportedStructTypeHandler(TypeByte typeByte)
	{
		TypeNo = new TypeNo(typeByte);
		Type = typeByte.Code.FindType();
	}

	public Object Get(AbstractTestStorage storage, AbstractPeachContext context, Int64 offset)
	{
		throw new NotImplementedException();
	}

	public void Set(AbstractTestStorage storage, AbstractPeachContext context, Object untyped, out Int64 address)
	{
		throw new NotImplementedException();
	}
}

public struct Ucs2StringTypeHandler : ISingletonObjectTypeHandler<Ucs2StringTypeHandler>
{
	public Type? Type => typeof(String);

	public TypeNo TypeNo => new TypeNo(new TypeByte(TypeCode.String, isSpan: true));

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

public struct StructArrayTypeHandler<T> : ISingletonObjectTypeHandler<StructArrayTypeHandler<T>>
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

public struct ObjectArrayTypeHandler : ISingletonObjectTypeHandler<ObjectArrayTypeHandler>
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

		var header = ObjectHeader.CreateForStruct<Int64>(TypeNo, n);

		storage.AllocateArrayObject<Int64>(header, out address, out var addresses);

		for (var i = 0; i < n; ++i)
		{
			var item = items[i];

			context.SetObjectToReferenceAddress(address + ObjectHeader.Size + i * 8, item);
		}

		var regot = context.GetObject(address);


	}
}
