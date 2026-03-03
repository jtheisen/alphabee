using System.Diagnostics;

namespace AlphaBee;

public class Hive
{
	private readonly AbstractTestStorage storage;

	private readonly PeachTypeRegistry typeRegistry = new();

	private readonly IHiveRoot root;

	public AbstractTestStorage Storage => storage;

	public PeachTypeRegistry TypeRegistry => typeRegistry;

	public Hive(AbstractTestStorage storage)
	{
		this.storage = storage;

		root = EnsureHiveRoot();

		LoadTypes();
	}

	IHiveRoot EnsureHiveRoot()
	{
		var context = new PeachContext(storage, typeRegistry);

		if (storage.IsEmpty)
		{
			return context.CreateObject<IHiveRoot>();
		}
		else
		{

			var hiveRoot = context.GetObject(0) as IHiveRoot;

			Trace.Assert(hiveRoot is not null, "Failed to get hive root");

			return hiveRoot;
		}
	}

	void LoadTypes()
	{
		var types = root.TypeDescriptions?.Cast<ITypeDescription>();

		Trace.Assert(types is not null);

		foreach (var type in types)
		{
			
		}
	}
}
