namespace AlphaBee.Collections;

public class HiveTestBase
{
	protected Hive hive;
	protected PeachContext context;

	public HiveTestBase()
	{
		var clrTypeResolver = new ClrTypeResolver(typeof(PeachTypeRegistryStorageTests).Assembly);

		var storage = new TestStorage();

		hive = new Hive(storage, clrTypeResolver);
		context = new PeachContext(hive.Storage, hive.TypeRegistry);
	}
}
