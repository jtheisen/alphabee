using System.Diagnostics;
using System.Net.Mime;

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
