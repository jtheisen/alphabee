using AlphaBee.TestModels;
using Newtonsoft.Json;
using System.Reflection;

namespace AlphaBee;

[TestClass]
public class PeachTypeRegistryStorageTests
{
	[TestMethod]
	public void TestWithStorage()
	{
		var clrTypeResolver = new ClrTypeResolver(typeof(PeachTypeRegistryStorageTests).Assembly);

		var storage = new TestStorage();

		String serializedTree1, serializedTree2;

		{
			var hive = new Hive(storage, clrTypeResolver);

			var registry = hive.TypeRegistry; 

			var context = new PeachContext(storage, registry);

			var tree = new BinaryTreeSampleBuilder(context).Create();

			serializedTree1 = JsonConvert.SerializeObject(tree);

			hive.SetRoot(tree);
		}

		{
			var hive = new Hive(storage, clrTypeResolver);
			
			var registry = hive.TypeRegistry;

			var context = new PeachContext(storage, registry);

			var tree = hive.FindRoot<BinaryTree>();

			serializedTree2 = JsonConvert.SerializeObject(tree);

			Assert.AreEqual(serializedTree1, serializedTree2);
		}
	}
}
