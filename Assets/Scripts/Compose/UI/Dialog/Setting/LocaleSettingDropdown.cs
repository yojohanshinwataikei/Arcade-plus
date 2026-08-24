using System.Linq;
using Arcade.Util.I18n;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose.Dialog
{
	[RequireComponent(typeof(Dropdown))]
	public class LocaleSettingDropdown : MonoBehaviour
	{
		private Dropdown dropdown;

		private void Awake()
		{
			dropdown = GetComponent<Dropdown>();
		}

		private void Start()
		{
			I18n.OnLocaleChanged += ApplyLocale;
			dropdown.onValueChanged.AddListener(OnValueChanged);
			
			ApplyLocale();
		}

		private void OnDestroy()
		{
			I18n.OnLocaleChanged -= ApplyLocale;
			dropdown.onValueChanged.RemoveListener(OnValueChanged);
		}

		private void ApplyLocale()
		{
			dropdown.options.Clear();
			dropdown.options.AddRange(I18n.LocaleEntries.Select(x => new Dropdown.OptionData(x.LocalizedName)));

			var idx = I18n.LocaleEntries.FindIndex(x => x.Id == I18n.CurrentLocale);
			dropdown.SetValueWithoutNotify(idx);
			dropdown.captionText.text = I18n.LocaleEntries[idx].LocalizedName;
		}

		private static void OnValueChanged(int idx)
		{
			I18n.SetLocale(I18n.LocaleEntries[idx].Id);
		}
	}
}
