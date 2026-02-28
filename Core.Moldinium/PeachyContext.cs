namespace AlphaBee;

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
