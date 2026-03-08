using System.Diagnostics;
using System.Net.Mime;

namespace AlphaBee;

public class Hive
{
	private readonly AbstractTestStorage storage;

	private readonly PeachTypeRegistry typeRegistry = new();

	private readonly PeachContext context;

	private readonly IHiveRoot root;

	private Int32 storedTypesCount;

	public AbstractTestStorage Storage => storage;

	public PeachTypeRegistry TypeRegistry => typeRegistry;

	public Hive(AbstractTestStorage storage)
	{
		this.storage = storage;

		context = new PeachContext(storage, typeRegistry);

		BootstrapTypes();

		if (storage.IsEmpty)
		{
			typeRegistry.ReadyEmpty();

			root = context.New<IHiveRoot>();

			Trace.Assert((root as IPeach)?.Address == AbstractTestStorage.FundamentalAlignment);
		}
		else
		{
			var firstObject = context.GetObject(AbstractTestStorage.FundamentalAlignment);

			root = (firstObject as IHiveRoot)!;

			Trace.Assert(root is not null, "Failed to get hive root");

			LoadTypes();
		}

		storedTypesCount = typeRegistry.Count;
	}

	public T FindRoot<T>()
		where T : class
	{
		var (typeNo, _) = typeRegistry.LookupCanonical(typeof(T));

		var description = GetDescription(typeNo);

		var rootInstance = description.RootInstance;

		if (rootInstance is not T result)
		{
			if (rootInstance is null)
			{
				throw new Exception($"Root object for type {typeof(T).FullName} was not found");
			}
			else
			{
				throw new Exception($"Root object for type {typeof(T).FullName} has incompatible type {rootInstance.GetType().FullName}");
			}
		}

		return result;
	}

	public void SetRoot<T>(T rootInstance)
	{
		EnsureTypesStored();

		var (typeNo, _) = typeRegistry.LookupCanonical(typeof(T));

		var description = GetDescription(typeNo);

		description.RootInstance = rootInstance;
	}

	ITypeDescription GetDescription(TypeNo typeNo)
	{
		var description = root.TypeDescriptions?[typeNo.no] as ITypeDescription;

		Trace.Assert(description is not null);

		return description;
	}

	void BootstrapTypes()
	{
		typeRegistry.BootstrapImplementation<IHiveRoot>();
	}

	void LoadTypes()
	{
		var descriptions = root.TypeDescriptions;

		if (descriptions is not null)
		{
			typeRegistry.ImportAllTypeDescriptions(descriptions);
		}
	}

	void EnsureTypesStored()
	{
		if (storedTypesCount > typeRegistry.Count)
		{
			StoreTypes();
		}
	}

	void StoreTypes()
	{
		var descriptions = new Object?[typeRegistry.Count];

		root.TypeDescriptions?.CopyTo(descriptions, 0);

		var didWrite = false;
 
		typeRegistry.ExportAllTypeDescriptions(descriptions, ref didWrite);

		if (didWrite)
		{
			root.TypeDescriptions = descriptions;
		}
	}
}
