using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Util.I18n
{
	[RequireComponent(typeof(Text))]
	public class I18nText : MonoBehaviour
	{
		[SerializeField] private string id;
		[SerializeField] private bool idFromContent = false;

		private Text text;
		public Text GetText() => text;

		public virtual void ApplyLocale()
		{
			text.text = I18n.Translate(id);
		}

		public void LoadComponent()
		{
			text = GetComponent<Text>();
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
		}

		private void Awake()
		{
			LoadComponent();
		}

		private void Start()
		{
			I18n.OnLocaleChanged += ApplyLocale;

			if (idFromContent) id = text.text.TrimStart('$');
			ApplyLocale();
		}

		private void OnDestroy()
		{
			I18n.OnLocaleChanged -= ApplyLocale;
		}
	}
}
