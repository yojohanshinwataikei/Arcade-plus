
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Util.UI{
	public class DropdownHelper<TData>{

		private Dropdown dropdown;

		private List<TData> options=new List<TData>();

		// Since Dictionary do not support null as key, we use ValueTuple as a workaround
		private Dictionary<ValueTuple<TData>,int> optionIds=new Dictionary<ValueTuple<TData>,int>();
		public DropdownHelper(Dropdown dropdown){
			this.dropdown=dropdown;
		}
		public delegate string GetString(TData data,int id);
		public void UpdateOptions(List<TData> options,GetString getString){
			dropdown.ClearOptions();
			List<string> optionStrings = new List<string>();
			optionIds = new Dictionary<ValueTuple<TData>, int>();
			for (int i = 0; i < options.Count; i++)
			{
				optionStrings.Add(getString(options[i], i));
				optionIds.Add(ValueTuple.Create(options[i]), i);
			}
			dropdown.AddOptions(optionStrings);
			this.options = options;
		}

		public TData QueryDataById(int id){
			return options[id];
		}

		public void SetValueWithoutNotify(TData data){
			dropdown.SetValueWithoutNotify(optionIds[ValueTuple.Create(data)]);
		}
	}
}