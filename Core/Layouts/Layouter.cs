namespace AlphaBee.Layouts;

public class Layouter
{
	public IEnumerable<LayoutEntry> CreateLayout(IEnumerable<Type> items)
	{


		foreach (var item in items)
		{
			var size = item.GetType().SizeOf();

			yield return new LayoutEntry { Offset = 0, Size = size };
		}
	}
}
