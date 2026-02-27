using AlphaBee;
using Moldinium.Baking;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Alphabee;

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

	public override Span<T> AllocateSpan<T>(out Int64 address, Int32 length)
	{
		address = position;

		position += Unsafe.SizeOf<T>() * length;

		return GetSpanCore<T>(address, length, extend: true);
	}

	public override Span<T> GetSpan<T>(Int64 offset, Int32 length)
	{
		return GetSpanCore<T>(offset, length);
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

	public override void SetValue<T>(Int64 offset, T value) => storage.SetValue(offset, value);

	public override T? GetObject<T>(Int64 offset) where T : class
	{
		var address = storage.GetValue<Int64>(offset);

		if (address == 0) return null;

		var handler = ObjectTypeKinds.GetHandler(typeof(T));

		return (T)handler.Get(storage, address);
	}

	public override void SetObject<T>(Int64 offset, T? value) where T : class
	{
		var handler = ObjectTypeKinds.GetHandler(typeof(T));

		if (value is null)
		{
			storage.SetValue<Int64>(offset, 0);
		}
		else
		{
			handler.Set(storage, value, out var address);

			storage.SetValue(offset, address);
		}
	}
}

public enum ObjectTypeKind
{
	Regular = 0,
	Bytes = 1,
	Ucs2String = 2,

	End = 4
}

public static class ObjectTypeKinds
{
	static AbstractTypeHandler[] handlers;

	static ObjectTypeKinds()
	{
		handlers = new AbstractTypeHandler[(Int32)ObjectTypeKind.End];

		foreach (var handler in Enumerate())
		{
			var i = (Int32)handler.Kind;

			handlers[i] = handler;
		}
	}

	static IEnumerable<AbstractTypeHandler> Enumerate()
	{
		yield return new Ucs2StringTypeHandler();
		yield return new BytesTypeHandler();
	}

	public static AbstractTypeHandler GetHandler(ObjectTypeKind kind)
	{
		return handlers[(Int32)kind] ?? throw new InvalidEnumArgumentException($"No handler for {kind}");
	}

	public static ObjectTypeKind GetKindForType(Type type)
	{
		if (type == typeof(String))
		{
			return ObjectTypeKind.Ucs2String;
		}
		else if (type == typeof(Byte[]))
		{
			return ObjectTypeKind.Bytes;
		}
		else
		{
			return ObjectTypeKind.Regular;
		}
	}

	public static AbstractTypeHandler GetHandler(Type type)
	{
		var kind = GetKindForType(type);

		return GetHandler(kind);
	}
}

public abstract class AbstractTypeHandler
{
	public abstract ObjectTypeKind Kind { get; }

	public abstract Object Get(AbstractTestStorage storage, Int64 offset);

	public abstract void Set(AbstractTestStorage storage, Object untyped, out Int64 address);
}

public class Ucs2StringTypeHandler : AbstractTypeHandler
{
	public override ObjectTypeKind Kind => ObjectTypeKind.Ucs2String;

	public override Object Get(AbstractTestStorage storage, Int64 offset)
	{
		var length = storage.GetValue<Int32>(offset);

		var chars = storage.GetSpan<Char>(offset + 4, length);

		return new String(chars);
	}

	public override void Set(AbstractTestStorage storage, Object untyped, out Int64 address)
	{
		var value = (String)untyped;

		var bytes = storage.AllocateSpan<Byte>(out address, value.Length * 2 + 4);

		bytes.InterpretAs<Int32>()[0] = value.Length;

		var chars = bytes[4..].InterpretAs<Char>();

		value.CopyTo(chars);
	}
}

public class BytesTypeHandler : AbstractTypeHandler
{
	public override ObjectTypeKind Kind => ObjectTypeKind.Bytes;

	public override Object Get(AbstractTestStorage storage, Int64 offset)
	{
		var length = storage.GetValue<Int32>(offset);

		var bytes = storage.GetSpan<Byte>(offset + 4, length);

		return bytes.ToArray();
	}

	public override void Set(AbstractTestStorage storage, Object untyped, out Int64 address)
	{
		var value = (Byte[])untyped;

		var bytes = storage.AllocateSpan<Byte>(out address, value.Length + 4);

		bytes.InterpretAs<Int32>()[0] = value.Length;

		var target = bytes[4..];

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
