using AlphaBee.Utilities;
using System.Diagnostics;
using System.Net;

namespace AlphaBee;

public abstract class AbstractPeachContext
{
	public abstract T GetValue<T>(Int64 address) where T : unmanaged;

	public abstract void SetValue<T>(Int64 address, T value) where T : unmanaged;

	public abstract Object? GetObject(Int64 address);

	public abstract Object? GetObjectFromReferenceAddress(Int64 referenceAddress);

	public abstract void SetObjectToReferenceAddress(Int64 referenceAddress, Object? value);

	public T New<T>(Action<T>? init = null)
		where T : class
	{
		var target = (T)New(typeof(T));

		init?.Invoke(target);

		return target;
	}

	public abstract Object New(Type interfaceType);

	public abstract Object New(TypeNo typeNo);

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

	public override Object? GetObject(Int64 address)
	{
		if (address == 0) return null;

		Debug.Assert(address % ObjectHeader.Size == 0);

		var header = storage.GetHeader(address);

		if (header.typeNo.IsFundamental)
		{
			var handler = ObjectTypeKinds.GetHandler(header.typeNo.typeByte);

			return handler.Get(storage, this, address);
		}
		else
		{
			var peach = CreatePeach(header.typeNo);

			peach.Init(this, address);

			return peach;
		}
	}

	public override Object? GetObjectFromReferenceAddress(Int64 referenceAddress)
	{
		var address = storage.GetValue<Int64>(referenceAddress);

		return GetObject(address);
	}

	public override void SetObjectToReferenceAddress(Int64 referenceAddress, Object? value)
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

	public override Object New(Type interfaceType)
	{
		typeRegistry.EnsureCanonicalImplementation(interfaceType, out var typeNo, out _);

		return New(typeNo);
	}

	public override Object New(TypeNo typeNo)
	{
		typeRegistry.GetImplementation(typeNo, out var implementationType, out var size);

		var header = ObjectHeader.CreateWithSize(typeNo, size);

		storage.AllocateObject(header, out var address);

		var peach = implementationType.CreateInstance<IPeachMixin>();

		peach.Init(this, address);

		return peach;
	}

	IPeachMixin CreatePeach(TypeNo typeNo)
	{
		typeRegistry.GetImplementation(typeNo, out var implementationType, out _);

		return implementationType.CreateInstance<IPeachMixin>();
	}
}
