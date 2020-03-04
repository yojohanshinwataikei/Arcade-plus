using System;
using Arcade.Compose.Dialog;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose
{
	public class AdeSkinBackgroundOption : MonoBehaviour
	{
		public Image Preview;
		public Text Label;
		public string BackgroundName;
		public bool External;
		public Color SelectedColor;
		public Color UnselectedColor;

		public void OnSelected(){
			AdeSkinDialog.Instance.SelectBackground(BackgroundName,External);
		}

		public void Initialize(string bg,bool external,Sprite image)
		{
			BackgroundName=bg;
			Label.text=bg;
			Preview.sprite=image;
			External=external;
		}

		public void SetSelected(bool selected){
			Preview.color=selected?SelectedColor:UnselectedColor;
		}
	}
}