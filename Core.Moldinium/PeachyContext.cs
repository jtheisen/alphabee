using System.Net;

namespace AlphaBee;

public abstract class AbstractPeachyContext
{
	public abstract T GetValue<T>(Int64 offset) where T : unmanaged;

	public abstract void SetValue<T>(Int64 offset, T value) where T : unmanaged;

	public abstract Object? GetObject(Int64 offset);

	public abstract void SetObject(Int64 offset, Object? value);
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

	public override Object? GetObject(Int64 referenceAddress)
	{
		var address = storage.GetValue<Int64>(referenceAddress);

		if (address == 0) return null;

		ref var header = ref storage.GetObject(address, out var content);

		if (header.type.IsFundamental)
		{
			var handler = ObjectTypeKinds.GetHandler(in header);

			return handler.Get(storage, address);
		}
		else
		{
			var peach = storage.CreatePeach(header.type);

			peach.Init(this);
			peach.Address = address;

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
			if (value is IPeach peach)
			{
				storage.SetValue(referenceAddress, peach.Address);
			}
			else
			{
				var handler = ObjectTypeKinds.GetHandler(value.GetType());

				handler.Set(storage, value, out var address);

				storage.SetValue(referenceAddress, address);
			}
		}
	}
}
