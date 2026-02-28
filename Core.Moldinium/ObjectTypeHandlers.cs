namespace AlphaBee;

public interface IObjectTypeHandler
{
	Type? Type { get; }

	Object Get(AbstractTestStorage storage, Int64 offset);

	void Set(AbstractTestStorage storage, Object untyped, out Int64 address);
}

public struct UnimplementedTypeHandler : IObjectTypeHandler
{
	public Type? Type => null;

	public Object Get(AbstractTestStorage storage, Int64 offset)
	{
		throw new NotImplementedException();
	}

	public void Set(AbstractTestStorage storage, Object untyped, out Int64 address)
	{
		throw new NotImplementedException();
	}
}

public struct Ucs2StringTypeHandler : IObjectTypeHandler
{
	public Type? Type => typeof(String);

	public Object Get(AbstractTestStorage storage, Int64 address)
	{
		ref var header = ref storage.GetObject(address, out var content);

		var chars = storage.GetSpan<Char>(address + ObjectHeader.Size, header.size / 2);

		return new String(chars);
	}

	public void Set(AbstractTestStorage storage, Object untyped, out Int64 address)
	{
		var value = (String)untyped;

		var header = new ObjectHeader(new TypeRef(new TypeByte(TypeCode.String)), value.Length * 2);

		storage.AllocateObject(header, out address, out var target);

		var chars = target.InterpretAs<Char>();

		value.CopyTo(chars);
	}
}

public struct StructArrayTypeHandler<T> : IObjectTypeHandler
	where T : unmanaged
{
	public Type? Type => typeof(T).MakeArrayType();

	public Object Get(AbstractTestStorage storage, Int64 address)
	{
		ref var header = ref storage.GetObject(address, out var content);

		var bytes = storage.GetSpan<Byte>(address + ObjectHeader.Size, header.size);

		return bytes.ToArray();
	}

	public void Set(AbstractTestStorage storage, Object untyped, out Int64 address)
	{
		var value = (Byte[])untyped;

		var header = new ObjectHeader(new TypeRef(new TypeByte(TypeCode.Byte, isSpan: true)), value.Length);

		storage.AllocateObject(header, out address, out var target);

		value.CopyTo(target);
	}
}

