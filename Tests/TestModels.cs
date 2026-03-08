namespace AlphaBee.TestModels;

interface BinaryTree : IEquatable<BinaryTree>
{
	BinaryTreeNode Root { get; set; }

	Boolean IEquatable<BinaryTree>.Equals(BinaryTree? other)
	{
		return true;
	}
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
		return ctx.New<BinaryTree>(t =>
		{
			t.Root = CreateBranch(depth);
		});
	}

	BinaryTreeNode CreateBranch(Int32 depth)
	{
		if (depth >= 0)
		{
			return ctx.New<BinaryTreeBranch>(b =>
			{
				b.Left = CreateBranch(depth - 1);
				b.Right = CreateBranch(depth - 2);
			});
		}
		else
		{
			return ctx.New<BinaryTreeLeaf>(l => l.Value = "");
		}
	}
}
