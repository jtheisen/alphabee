using AlphaBee.Utilities;
using System.Net;

namespace AlphaBee;

public abstract class AbstractPeachContext
{
	public abstract T GetValue<T>(Int64 offset) where T : unmanaged;

	public abstract void SetValue<T>(Int64 offset, T value) where T : unmanaged;

	public abstract Object? GetObject(Int64 offset);

	public abstract void SetObject(Int64 offset, Object? value);

	public T CreateObject<T>(Action<T>? init = null)
		where T : class
	{
		var target = (T)CreateObject(typeof(T));

		init?.Invoke(target);

		return target;
	}

	public abstract Object CreateObject(Type interfaceType);

	public abstract Object CreateObject(TypeNo typeNo);

}

public class PeachContext : AbstractPeachContext
{
	private readonly AbstractTestStorage storage;
	private readonly PeachTypeRegistry typeRegistry;

	public PeachContext(AbstractTestStorage storage, PeachTypeRegistry typeRegistry)
	{
		this.storage = storage;
		this.typeRegistry = typeRegistry;
	}

	public override T GetValue<T>(Int64 offset) => storage.GetValue<T>(offset);

	public override void SetValue<T>(Int64 address, T value) => storage.SetValue(address, value);

	public override Object? GetObject(Int64 referenceAddress)
	{
		var address = storage.GetValue<Int64>(referenceAddress);

		if (address == 0) return null;

		ref var header = ref storage.GetObject(address, out var content);

		if (header.type.IsFundamental)
		{
			var handler = ObjectTypeKinds.GetHandler(in header);

			return handler.Get(storage, this, address);
		}
		else
		{
			var peach = CreatePeach(header.type);

			peach.Init(this, address);

			return peach;
		}
	}

	public override void SetObject(Int64 referenceAddress, Object? value)
	{
		if (value is null)
		{
			storage.SetValue<Int64>(referenceAddress, 0);
		}
		else
		{
			if (value is IPeachMixin peach)
			{
				storage.SetValue(referenceAddress, peach.Address);
			}
			else
			{
				var handler = ObjectTypeKinds.GetHandler(value.GetType());

				handler.Set(storage, this, value, out var address);

				storage.SetValue(referenceAddress, address);
			}
		}
	}

	public override Object CreateObject(Type clrType)
	{
		typeRegistry.LookupClrType(clrType, out var typeNo, out _);

		return CreateObject(typeNo);
	}

	public override Object CreateObject(TypeNo typeNo)
	{
		var entry = typeRegistry.GetEntry(typeNo);

		var header = new ObjectHeader(typeNo, entry.Layout.Size);

		storage.AllocateObject(header, out var address, out var content);

		var peach = entry.ImplementationType.CreateInstance<IPeachMixin>();

		peach.Init(this, address);

		return peach;
	}

	IPeachMixin CreatePeach(TypeNo type)
	{
		var entry = typeRegistry.GetEntry(type);

		return entry.ImplementationType.CreateInstance<IPeachMixin>();
	}
}
