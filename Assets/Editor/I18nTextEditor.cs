using System;
using Arcade.Util.I18n;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(I18nText), true)]
public class I18nTextEditor : Editor
{
	private bool duringTween = false;
	private const int DelayBetweenSingleFlash = 200;
	private const int FlashCount = 2;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		var text = (target as I18nText)!;

		using (new EditorGUI.DisabledScope(duringTween))
		{
			if (GUILayout.Button("Apply Locale"))
			{
				text.LoadComponent();
				var textComponent = text.GetText();

				Color flashColor;
				Color originalColor = textComponent.color;

				try
				{
					I18n.SetLocale(I18n.CurrentLocale);
					text.ApplyLocale();

					flashColor = Color.green;
				}
				catch (Exception)
				{
					text.GetText().text = I18n.UnknownValue;

					flashColor = Color.red;
				}

				DoFlash(textComponent, flashColor, originalColor).Forget();
			}
		}
		return;

		async UniTask DoFlash(Text textComponent, Color flashColor, Color originalColor)
		{
			duringTween = true;
			Repaint();

			for (int i = 0; i < FlashCount; i++)
			{
				textComponent.color = flashColor;
				await UniTask.Delay(DelayBetweenSingleFlash);
				textComponent.color = originalColor;
				await UniTask.Delay(DelayBetweenSingleFlash);
			}

			duringTween = false;
			Repaint();
		}
	}
}
