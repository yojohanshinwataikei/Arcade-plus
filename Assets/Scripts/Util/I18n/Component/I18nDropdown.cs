using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Util.I18n
{
	[RequireComponent(typeof(Dropdown))]
	public class I18nDropdown : MonoBehaviour
	{
		private Dropdown dropdown;
		public Dropdown GetDropdown() => dropdown;

		private List<string> options = new List<string>();

		public virtual void ApplyLocale()
		{
			for (int i = 0; i < options.Count; i++)
			{
				string raw = options[i];

				dropdown.options[i].text = raw.StartsWith('$')
					? I18n.Translate(raw[1..])
					: raw;
			}
			
			dropdown.captionText.text = dropdown.options[dropdown.value].text;
		}

		public void LoadComponent()
		{
			dropdown = GetComponent<Dropdown>();
		}

		private void Awake()
		{
			LoadComponent();
		}

		private void Start()
		{
			options = dropdown.options.Select(x => x.text).ToList();
			
			I18n.OnLocaleChanged += ApplyLocale;
			ApplyLocale();
		}

		private void OnDestroy()
		{
			I18n.OnLocaleChanged -= ApplyLocale;
		}
	}
}
