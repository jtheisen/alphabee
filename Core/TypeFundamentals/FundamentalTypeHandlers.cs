namespace AlphaBee;

public interface ITypeHandler
{
	Type? Type { get; }

	TypeNo TypeNo { get; }

	Object Get(AbstractTestStorage storage, PeachContext context, Int64 address);

	void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address);

	static virtual ITypeHandler GetHandler(TypeByte typeByte) => throw new NotImplementedException();

	String HandlerName => GetType().Name.StripSuffix("`1").StripSuffix(nameof(ITypeHandler).StripPrefix("I"));

	String HandlerMessage => HandlerName;
}

public interface IHandlerGetter
{
	ITypeHandler GetHandler(TypeByte typeByte);

	static ITypeHandler Get(Type type, TypeByte typeByte)
	{
		return typeof(HandlerProvider<>)
			.MakeGenericType(type)
			.CreateInstance<IHandlerGetter>()
			.GetHandler(typeByte);
	}
}

public struct HandlerProvider<HandlerT> : IHandlerGetter
	where HandlerT : ITypeHandler
{
	public ITypeHandler GetHandler(TypeByte typeByte)
	{
		return HandlerT.GetHandler(typeByte);
	}
}

public interface ISingletonObjectTypeHandler<T> : ITypeHandler
	where T : ITypeHandler, new()
{
	private static ITypeHandler? instance;

	static ITypeHandler ITypeHandler.GetHandler(TypeByte _)
	{
		return instance ?? (instance = new T());
	}
}

public struct UnimplementedMiscTypeHandler : ITypeHandler
{
	private readonly String? message;

	public Type? Type => null;

	public TypeNo TypeNo { get; }

	public String HandlerMessage => message ?? (this as ITypeHandler).HandlerName;

	public UnimplementedMiscTypeHandler(TypeByte typeByte, String? message = null)
	{
		TypeNo = new TypeNo(typeByte);
		this.message = message;
	}

	public Object Get(AbstractTestStorage storage, PeachContext context, Int64 address)
	{
		throw new NotImplementedException();
	}

	public void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address)
	{
		throw new NotImplementedException();
	}
}

public struct UnimplementedSupportedStructTypeHandler : ITypeHandler
{
	public Type? Type { get; }

	public TypeNo TypeNo { get; }

	static ITypeHandler ITypeHandler.GetHandler(TypeByte typeByte)
	{
		return new UnimplementedSupportedStructTypeHandler(typeByte);
	}

	public UnimplementedSupportedStructTypeHandler(TypeByte typeByte)
	{
		TypeNo = new TypeNo(typeByte);
		var type = typeByte.Code.FindType();
		if (typeByte.IsNullable)
		{
			type = type.MakeNullableType();
		}
		if (typeByte.IsSpan)
		{
			type = type.MakeArrayType();
		}
		Type = type;
	}

	public Object Get(AbstractTestStorage storage, PeachContext context, Int64 offset)
	{
		throw new NotImplementedException();
	}

	public void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address)
	{
		throw new NotImplementedException();
	}
}

public struct Ucs2StringTypeHandler : ISingletonObjectTypeHandler<Ucs2StringTypeHandler>
{
	public Type? Type => typeof(String);

	public TypeNo TypeNo => new TypeNo(new TypeByte(TypeCode.String, isSpan: true, isNullable: true));

	public Object Get(AbstractTestStorage storage, PeachContext context, Int64 address)
	{
		var chars = storage.GetValueArrayObjectOld<Char>(address, out _);

		return new String(chars);
	}

	public void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address)
	{
		var value = (String)untyped;

		var header = ObjectHeader.CreateForStruct<Char>(TypeNo, value.Length);

		storage.AllocateArrayObjectOld<Char>(header, out address, out var chars);

		value.CopyTo(chars);
	}
}

public struct StructArrayTypeHandler<T> : ISingletonObjectTypeHandler<StructArrayTypeHandler<T>>
	where T : unmanaged
{
	public Type? Type => typeof(T).MakeArrayType();

	public TypeNo TypeNo => new TypeNo(new TypeByte(typeof(T).GetTypeCode(), isSpan: true));

	public Object Get(AbstractTestStorage storage, PeachContext context, Int64 address)
	{
		var items = storage.GetValueArrayObjectOld<T>(address, out _);

		return items.ToArray();
	}

	public void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address)
	{
		var value = (T[])untyped;

		var header = ObjectHeader.CreateForStruct<T>(TypeNo, value.Length);

		storage.AllocateArrayObjectOld<T>(header, out address, out var items);

		value.CopyTo(items);
	}
}

public struct NullableStructArrayTypeHandler<T> : ISingletonObjectTypeHandler<NullableStructArrayTypeHandler<T>>
	where T : unmanaged
{
	public Type? Type => typeof(T?).MakeArrayType();

	public TypeNo TypeNo => new TypeNo(new TypeByte(typeof(T).GetTypeCode(), isSpan: true, isNullable: true));

	public Object Get(AbstractTestStorage storage, PeachContext context, Int64 address)
	{
		var items = storage.GetValueArrayObjectOld<NullableStruct<T>>(address, out _);

		var result = new T?[items.Length];

		items.CopyTo(result);

		return result;
	}

	public void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address)
	{
		var value = (T?[])untyped;

		var header = ObjectHeader.CreateForStruct<NullableStruct<T>>(TypeNo, value.Length);

		storage.AllocateArrayObjectOld<NullableStruct<T>>(header, out address, out var items);

		items.CopyFrom(value.AsSpan());
	}
}

public struct ObjectArrayTypeHandler : ISingletonObjectTypeHandler<ObjectArrayTypeHandler>
{
	public Type? Type => typeof(Object?[]);

	public TypeNo TypeNo => new TypeNo(new TypeByte(TypeCode.Object, isSpan: true, isNullable: true));

	public Object Get(AbstractTestStorage storage, PeachContext context, Int64 address)
	{
		var addresses = storage.GetValueArrayObjectOld<Int64>(address, out _);

		var n = addresses.Length;

		var array = new Object?[n];

		for (var i = 0; i < n; ++i)
		{
			var itemAddress = addresses[i];

			array[i] = context.GetObject(itemAddress);
		}

		return array;
	}

	public void Set(AbstractTestStorage storage, PeachContext context, Object untyped, out Int64 address)
	{
		var items = (Object[])untyped;

		var n = items.Length;

		var header = ObjectHeader.CreateForStruct<Int64>(TypeNo, n);

		storage.AllocateArrayObjectOld<Int64>(header, out address, out var addresses);

		for (var i = 0; i < n; ++i)
		{
			var item = items[i];

			context.SetObjectToReferenceAddress(address + ObjectHeader.Size + i * 8, item);
		}

		var regot = context.GetObject(address);


	}
}
