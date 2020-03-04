using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Util.UnityExtension
{
	public static class TextExtension
	{
		public static float CalculateHeight(this Text text, string str)
		{
			text.verticalOverflow = VerticalWrapMode.Overflow;
			var generator = text.cachedTextGenerator;
			return generator.GetPreferredHeight(str, text.GetGenerationSettings(text.rectTransform.rect.size)) / text.pixelsPerUnit;
		}
		public static float CalculateWidth(this Text text, string str)
		{
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			var generator = text.cachedTextGenerator;
			return generator.GetPreferredWidth(str, text.GetGenerationSettings(text.rectTransform.rect.size)) / text.pixelsPerUnit;
		}
	}
}