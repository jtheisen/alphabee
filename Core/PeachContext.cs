using AlphaBee.Collections;

namespace AlphaBee;

public class PeachContext
{
	private readonly AbstractTestStorage storage;
	private readonly PeachTypeRegistry typeRegistry;

	public PeachContext(AbstractTestStorage storage, PeachTypeRegistry typeRegistry)
	{
		this.storage = storage;
		this.typeRegistry = typeRegistry;
	}

	public T New<T>() where T : class => New<T>(null);

	public T New<T>(Action<T>? init)
		where T : class
	{
		var target = (T)New(typeof(T));

		init?.Invoke(target);

		return target;
	}

	public Span<T> GetSpan<T>(Int64 address, Int32 length) where T : unmanaged
	{
		return storage.GetSpan<T>(address, length);
	}

	public T GetValue<T>(Int64 offset) where T : unmanaged => storage.GetValue<T>(offset);

	public void SetValue<T>(Int64 address, T value) where T : unmanaged => storage.SetValue(address, value);

	public TPeach? GetPeach<TPeach>(Int64 address) where TPeach : class, IPeach, IPeachMixin, new()
	{
		if (address == 0) return null;

		var peach = new TPeach();

		peach.Init(this, address);

		return peach;
	}

	public Object? GetObject(Int64 address)
	{
		if (address == 0) return null;

		Debug.Assert(address % ObjectHeader.Size == 0);

		var header = storage.GetHeader(address);

		if (header.TypeNo.IsFundamental)
		{
			var handler = FundamentalTypes.GetHandler(header.TypeNo.TypeByte);

			return handler.Get(storage, this, address);
		}
		else
		{
			var peach = CreatePeach(header.TypeNo);

			peach.Init(this, address);

			return peach;
		}
	}

	public Object? GetObjectFromReferenceAddress(Int64 referenceAddress)
	{
		var address = storage.GetValue<Int64>(referenceAddress);

		return GetObject(address);
	}

	public void SetObjectToReferenceAddress(Int64 referenceAddress, Object? value)
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
				var handler = FundamentalTypes.GetHandler(value.GetType());

				handler.Set(storage, this, value, out var address);

				storage.SetValue(referenceAddress, address);
			}
		}
	}

	public Span<T> GetValueArrayObject<T>(Int64 address, out ObjectHeader header) where T : unmanaged
	{
		return storage.GetValueArrayObject<T>(address, out header);
	}

	public Object New(Type interfaceType)
	{
		typeRegistry.EnsureCanonicalImplementation(interfaceType, out var typeNo, out _);

		return New(typeNo);
	}

	public Object New(TypeNo typeNo)
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

	//public IValueBeeArray<T> NewValueArray<T>(Int32 length)
	//{
	//	typeRegistry.EnsureCanonicalImplementation(typeof(T), out var typeNo, out _);

	//	storage.AllocateArrayObject<T>(new(typeNo, new(length, Unsafe.SizeOf<T>())), out var address, out _);

	//	var array = new ValueBeeArrayImplementation<T>();

	//	array.Length = length;

	//	var boxed = array as IValueBeeArray<T>;

	//	boxed.Init(this, address);

	//	return boxed;
	//}
}
