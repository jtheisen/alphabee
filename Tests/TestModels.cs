namespace AlphaBee.TestModels;

interface BinaryTree
{
	BinaryTreeNode Root { get; set; }
}

interface BinaryTreeNode
{
	BinaryTreeNode Parent { get; set; }
}

interface BinaryTreeBranch : BinaryTreeNode
{
	BinaryTreeNode Left { get; set; }

	BinaryTreeNode Right { get; set; }
}

interface BinaryTreeLeaf : BinaryTreeNode
{
	String Value { get; set; }
}

class BinaryTreeSampleBuilder(PeachContext ctx)
{
	public BinaryTree Create(Int32 depth = 5)
	{
		return ctx.CreateObject<BinaryTree>(t =>
		{
			t.Root = CreateBranch(depth);
		});
	}

	BinaryTreeNode CreateBranch(Int32 depth)
	{
		if (depth >= 0)
		{
			return ctx.CreateObject<BinaryTreeBranch>(b =>
			{
				b.Left = CreateBranch(depth - 1);
				b.Right = CreateBranch(depth - 2);
			});
		}
		else
		{
			return ctx.CreateObject<BinaryTreeLeaf>(l => l.Value = "");
		}
	}
}
