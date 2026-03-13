namespace Moldinium.Baking;

public struct Unification
{
	Dictionary<Type, Type> map;

	public static Type UnifyForSpecificArgument(Type source, Type target, Type arg)
	{
		if (Unify(source, target, out var map))
		{
			if (map.TryGetValue(arg, out var type))
			{
				return type;
			}
			else
			{
				throw new ArgumentException($"Type argument {arg} was not mapped after unification of {source} onto {target}");
			}
		}
		else
		{
			throw new ArgumentException($"Type {source} could not be unified onto {target}");
		}
	}

	public static Boolean Unify(Type source, Type target, out IDictionary<Type, Type> map)
	{
		var unification = new Unification();

		map = unification.map = new();

		var result = unification.UnifyCore(source, target);

		return result;
	}

	Boolean UnifyCore(Type source, Type target)
	{
		if (source == target)
		{
			return true;
		}

		if (source.IsGenericTypeParameter)
		{
			if (map.TryGetValue(source, out var other))
			{
				if (other != target)
				{
					return false;
				}
			}
			else
			{
				map[source] = target;
			}

			return true;
		}

		if (source.IsArray)
		{
			if (target.IsArray)
			{


				var sourceElementType = source.GetElementType();
				var targetElementType = target.GetElementType();
			}
		}

		if (!source.IsGenericType || !target.IsGenericType)
		{
			return false;
		}

		var sourceTypeDefintion = source.GetGenericTypeDefinition();
		var targetTypeDefinition = target.GetGenericTypeDefinition();

		if (sourceTypeDefintion != targetTypeDefinition)
		{
			return false;
		}

		var sourceTypeArguments = source.GetGenericArguments();
		var targetTypeArguments = target.GetGenericArguments();

		var n = sourceTypeArguments.Length;

		Trace.Assert(targetTypeArguments.Length == n);

		for (var i = 0; i < n; ++i)
		{
			var sourceTypeArgument = sourceTypeArguments[i];
			var targetTypeArgument = targetTypeArguments[i];

			if (!UnifyCore(sourceTypeArgument, targetTypeArgument))
			{
				return false;
			}
		}

		return true;
	}

	public record struct SourceAndTarget<T>(T source, T target)
	{

	}
}
