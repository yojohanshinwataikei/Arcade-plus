
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Util.UI{
	public class DropdownHelper<TData>{

		private Dropdown dropdown;

		private List<TData> options=new List<TData>();
		private Dictionary<TData,int> optionIds=new Dictionary<TData,int>();
		public DropdownHelper(Dropdown dropdown){
			this.dropdown=dropdown;
		}
		public delegate string GetString(TData data,int id);
		public void UpdateOptions(List<TData> options,GetString getString){
			dropdown.ClearOptions();
			List<string> optionStrings = new List<string>();
			optionIds = new Dictionary<TData, int>();
			for (int i = 0; i < options.Count; i++)
			{
				optionStrings.Add(getString(options[i], i));
				optionIds.Add(options[i], i);
			}
			dropdown.AddOptions(optionStrings);
			this.options = options;
		}

		public TData QueryDataById(int id){
			return options[id];
		}

		public void SetValueWithoutNotify(TData data){
			dropdown.SetValueWithoutNotify(optionIds[data]);
		}
	}
}