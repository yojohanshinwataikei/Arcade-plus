using System.Collections;

namespace Arcade.Util.UnityExtension
{
	public static class CollectionExtension
	{
		public static bool OutOfRange(this ICollection collection, int index)
		{
			return index < 0 || index > collection.Count - 1;
		}
	}
}