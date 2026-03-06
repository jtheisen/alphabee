using AlphaBee.TestModels;

namespace AlphaBee;

[TestClass]
public class PeachTypeRegistryStorageTests
{
	[TestMethod]
	public void TestWithStorage()
	{
		var storage = new TestStorage();

		{
			var hive = new Hive(storage);

			var registry = hive.TypeRegistry; 

			var context = new PeachContext(storage, registry);

			new BinaryTreeSampleBuilder(context).Create();
		}

		{
			var hive = new Hive(storage);
			
			var registry = hive.TypeRegistry;
		}
	}
}
