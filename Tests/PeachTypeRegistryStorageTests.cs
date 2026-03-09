using AlphaBee.TestModels;
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

		{
			var hive = new Hive(storage, clrTypeResolver);

			var registry = hive.TypeRegistry; 

			var context = new PeachContext(storage, registry);

			var tree = new BinaryTreeSampleBuilder(context).Create();

			hive.SetRoot(tree);
		}

		{
			var hive = new Hive(storage, clrTypeResolver);
			
			var registry = hive.TypeRegistry;

			var context = new PeachContext(storage, registry);

			var oldTree = hive.FindRoot<BinaryTree>();

			var newTree = new BinaryTreeSampleBuilder(context).Create();

			Assert.IsTrue(newTree.Equals(oldTree));
		}
	}
}
