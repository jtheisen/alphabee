using System.Diagnostics;

namespace AlphaBee;

public class Hive
{
	private readonly AbstractTestStorage storage;

	private readonly PeachTypeRegistry typeRegistry = new();

	private readonly PeachContext context;

	private readonly IHiveRoot root;

	public AbstractTestStorage Storage => storage;

	public PeachTypeRegistry TypeRegistry => typeRegistry;

	public Hive(AbstractTestStorage storage)
	{
		this.storage = storage;

		context = new PeachContext(storage, typeRegistry);

		BootstrapTypes();

		root = EnsureHiveRoot();
	}

	void BootstrapTypes()
	{
		typeRegistry.BootstrapImplementation<IHiveRoot>();
	}

	IHiveRoot EnsureHiveRoot()
	{
		if (storage.IsEmpty)
		{
			typeRegistry.ReadyEmpty();

			return context.CreateObject<IHiveRoot>();
		}
		else
		{

			var hiveRoot = context.GetObject(0) as IHiveRoot;

			Trace.Assert(hiveRoot is not null, "Failed to get hive root");

			LoadTypes();

			return hiveRoot;
		}
	}

	void LoadTypes()
	{
		var descriptions = root.TypeDescriptions;

		Trace.Assert(descriptions is not null);

		typeRegistry.ImportAllTypeDescriptions(descriptions);
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
