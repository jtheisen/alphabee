using AlphaBee.Utilities;
using Moldinium.Baking;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AlphaBee;

public abstract class AbstractPeachyContext
{
	public abstract T GetValue<T>(Int64 offset) where T : unmanaged;

	public abstract void SetValue<T>(Int64 offset, T value) where T : unmanaged;

	public abstract T? GetObject<T>(Int64 offset) where T : class;

	public abstract void SetObject<T>(Int64 offset, T? value) where T : class;
}

public abstract class AbstractTestStorage
{
	public abstract Span<T> GetSpan<T>(Int64 offset, Int32 length) where T : unmanaged;

	public abstract Span<T> AllocateSpan<T>(out Int64 address, Int32 length) where T : unmanaged;

	public T GetValue<T>(Int64 offset) where T : unmanaged => GetSpan<T>(offset, 1)[0];

	public void SetValue<T>(Int64 offset, T value) where T : unmanaged => GetSpan<T>(offset, 1)[0] = value;

	public ref T AllocateValue<T>(out Int64 address) where T : unmanaged => ref AllocateSpan<T>(out address, 1)[0];

	public ref ObjectHeader GetObject(Int64 address, out Span<Byte> content)
	{
		var headerSize = Unsafe.SizeOf<ObjectHeader>();

		ref var header = ref GetSpan<ObjectHeader>(address, 1)[0];

		content = GetSpan<Byte>(address + headerSize, header.size);

		return ref header;
	}

	public void AllocateObject(ObjectHeader header, out Int64 address, out Span<Byte> content)
	{
		var headerSize = Unsafe.SizeOf<ObjectHeader>();

		var span = AllocateSpan<Byte>(out address, headerSize + header.size);

		span.InterpretAs<ObjectHeader>()[0] = header;

		content = span[headerSize..];
	}
}

public class TestStorage : AbstractTestStorage
{
	private Int64 position = 0;

	private Byte[] data;

	public Byte[] Data => data;

	public TestStorage(Int32 reserved = 0)
	{
		position = reserved;
		data = new Byte[Math.Max(reserved, 4)];
	}

	Span<T> GetSpanCore<T>(Int64 offset, Int32 length, Boolean extend = false) where T : unmanaged
	{
		Trace.Assert(offset < Int32.MaxValue);

		var offset32 = (Int32)offset;

		var max = offset + Unsafe.SizeOf<T>() * length;

		if (!extend && max > position)
		{
			throw new Exception("Accessing invalid address space");
		}

		while (max > data.Length)
		{
			Double();
		}

		return data.AsSpan()[offset32..].InterpretAs<T>()[..length];
	}

	void Double()
	{
		var copy = new Byte[data.Length * 2];
		data.CopyTo(copy, 0);
		data = copy;
	}

	public override Span<T> AllocateSpan<T>(out Int64 referenceAddress, Int32 length)
	{
		referenceAddress = position;

		position += Unsafe.SizeOf<T>() * length;

		return GetSpanCore<T>(referenceAddress, length, extend: true);
	}

	public override Span<T> GetSpan<T>(Int64 referenceAddress, Int32 length)
	{
		return GetSpanCore<T>(referenceAddress, length);
	}
}

public abstract class AbstractFoo
{
	public abstract T? Get<T>() where T : class;
}

public class Foo : AbstractFoo
{
	public override T? Get<T>() where T : class
	{
		return (T)new Object();
	}
}

public class PeachyContext : AbstractPeachyContext
{
	private readonly AbstractTestStorage storage;

	public PeachyContext(AbstractTestStorage storage)
	{
		this.storage = storage;
	}

	public override T GetValue<T>(Int64 offset) => storage.GetValue<T>(offset);

	public override void SetValue<T>(Int64 address, T value) => storage.SetValue(address, value);

	public override T? GetObject<T>(Int64 referenceAddress) where T : class
	{
		var address = storage.GetValue<Int64>(referenceAddress);

		if (address == 0) return null;

		ref var header = ref storage.GetObject(address, out var content);

		var handler = ObjectTypeKinds.GetHandler(in header);

		return (T)handler.Get(storage, address);
	}

	public override void SetObject<T>(Int64 referenceAddress, T? value) where T : class
	{
		var handler = ObjectTypeKinds.GetHandler(typeof(T));

		if (value is null)
		{
			storage.SetValue<Int64>(referenceAddress, 0);
		}
		else
		{
			handler.Set(storage, value, out var address);

			storage.SetValue(referenceAddress, address);
		}
	}
}

public static class ObjectTypeKinds
{
	static readonly IObjectTypeHandler[] handlersByByte;

	static readonly Dictionary<Type, IObjectTypeHandler> handlersByType = new();

	static ObjectTypeKinds()
	{
		var typeCodes = Enum.GetValues<TypeCode>();

		handlersByByte = new IObjectTypeHandler[128];

		var unimplementedHandler = new UnimplementedTypeHandler();

		for (Byte i = 0; i < 128; ++i)
		{
			var typeByte = new TypeByte(i);

			var handler = handlersByByte[i] = GetHandlerType(typeByte)?.CreateInstance<IObjectTypeHandler>()
				?? unimplementedHandler;

			if (handler.Type is Type type)
			{
				Trace.Assert(!handlersByType.ContainsKey(type), $"There's already a handler registered for type '{type}'");

				handlersByType[type] = handler;
			}
		}
	}

	static Type? GetHandlerType(TypeByte typeByte)
	{
		var code = typeByte.Code;

		var isSpan = typeByte.IsSpan;

		var isNullable = typeByte.IsNullable;

		if (code == TypeCode.String)
		{
			// A TypeCode.String means a Char span that is treated as a String
			return isSpan || isNullable ? null : typeof(Ucs2StringTypeHandler);
		}
		else if (code == TypeCode.Object)
		{
			// A TypeCode.Object means a reference to something else
			// FIXME: implement this
			return null;
		}
		else if (code.IsSupportedStruct())
		{
			// FIXME: this could be supported
			if (!isSpan) return null;

			var type = code.FindType();

			if (isNullable)
			{
				// FIXME: we can implement this
				return null;

				// FIXME: unsafe, because the layout of Nullable<> could change
				//type = typeof(Nullable<>).MakeGenericType(type);
			}

			return isSpan ? typeof(StructArrayTypeHandler<>).MakeGenericType(type) : null;
		}
		else
		{
			return null;
		}
	}

	public static String ReportTypes()
	{
		var writer = new StringWriter();

		for (Byte i = 0; i < 128; ++i)
		{
			writer.WriteLine($"{new TypeByte(i)} - {handlersByByte[i].Type?.Name ?? "n/a"}");
		}

		return writer.ToString();
	}

	public static IObjectTypeHandler GetHandler(in ObjectHeader header)
	{
		var typeByte = header.type.typeByte;

		Trace.Assert(!typeByte.IsZero);

		return handlersByByte[typeByte.value];
	}

	static IObjectTypeHandler? GetHandlerOrNull(Type type)
	{
		return handlersByType.GetValueOrDefault(type);
	}

	public static IObjectTypeHandler GetHandler(Type type)
	{
		return GetHandlerOrNull(type) ?? throw new Exception($"No handler exists for type '{type.Name}'");
	}
}

public interface IObjectTypeHandler
{
	Type? Type { get; }

	Object Get(AbstractTestStorage storage, Int64 offset);

	void Set(AbstractTestStorage storage, Object untyped, out Int64 address);
}

public class UnimplementedTypeHandler : IObjectTypeHandler
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

public class Ucs2StringTypeHandler : IObjectTypeHandler
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

public class StructArrayTypeHandler<T> : IObjectTypeHandler
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

public interface IPeachyMixin
{
	Int64 Address { get; set; }

	void Init(AbstractPeachyContext context);
}

[IgnoreForBaking]
public interface IPeachyInternalMixin : IPeachyMixin
{
	T GetValue<T>(Int32 offset) where T : unmanaged;

	void SetValue<T>(Int32 offset, T value) where T : unmanaged;

	T? GetObject<T>(Int32 offset) where T : class;

	void SetObject<T>(Int32 offset, T? value) where T : class;
}

public struct ExamplePeachyMixin : IPeachyInternalMixin
{
	AbstractPeachyContext context;

	public void Init(AbstractPeachyContext context)
	{
		this.context = context;
	}

	public Int64 Address { get; set; }

	Int64 GetFieldAddress(Int32 offset) => Address + (Int64)offset;

	public T GetValue<T>(Int32 offset) where T : unmanaged
	{
		var address = GetFieldAddress(offset);

		return context.GetValue<T>(address);
	}

	public void SetValue<T>(Int32 offset, T value) where T : unmanaged
	{
		context.SetValue(GetFieldAddress(offset), value);
	}

	public T? GetObject<T>(Int32 offset) where T : class
	{
		var address = GetFieldAddress(offset);

		return context.GetObject<T>(address);
	}

	public void SetObject<T>(Int32 offset, T? value) where T : class
	{
		context.SetObject(GetFieldAddress(offset), value);
	}
}

//public struct PeachyMixin<AccessorT> : IPeachyMixing
//	where AccessorT : IPeachyAccessor
//{
//	public UInt64 Address { get; set; }

//	public AccessorT Accessor { get; set; }

//	ref T GetCore<T>(Int32 offset) where T : unmanaged => ref Accessor.Get<T>(Address + (UInt32)offset);

//	public T Get<T>(Int32 offset) where T : unmanaged => GetCore<T>(offset);

//	public void Set<T>(Int32 offset, T value) where T : unmanaged => GetCore<T>(offset) = value;
//}

public interface IPeachyStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Value : unmanaged
	where Mixin : IPeachyMixin
{
	Value Get(ref Mixin mixin, Int32 offset);

	void Set(ref Mixin mixin, Int32 offset, Value value);
}

public struct PeachyStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPeachyStructPropertyImplementation<Value, ExamplePeachyMixin>
	where Value : unmanaged
{
	public Value Get(ref ExamplePeachyMixin mixin, Int32 offset) => mixin.GetValue<Value>(offset);

	public void Set(ref ExamplePeachyMixin mixin, Int32 offset, Value value) => mixin.SetValue(offset, value);
}

public interface IPeachyClassPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Value : class
	where Mixin : IPeachyMixin
{
	Value? Get(ref Mixin mixin, Int32 offset);

	void Set(ref Mixin mixin, Int32 offset, Value? value);
}

public struct PeachyClassPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPeachyClassPropertyImplementation<Value, ExamplePeachyMixin>
	where Value : class
{
	public Value? Get(ref ExamplePeachyMixin mixin, Int32 offset) => mixin.GetObject<Value>(offset);

	public void Set(ref ExamplePeachyMixin mixin, Int32 offset, Value? value) => mixin.SetObject(offset, value);
}

public class PeachyPropertyImplementationProvider : PropertyImplementationProvider
{
	public override Type Get(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return typeof(PeachyClassPropertyImplementation<>);
		}
		else
		{
			return typeof(PeachyStructPropertyImplementation<>);
		}
	}

	public override IEnumerable<Type> GetAll()
	{
		yield return typeof(PeachyClassPropertyImplementation<>);
		yield return typeof(PeachyStructPropertyImplementation<>);
	}
}
