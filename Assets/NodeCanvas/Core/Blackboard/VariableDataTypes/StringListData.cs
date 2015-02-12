using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	public class StringListData : VariableData<List<string>> {

		public override void OnCreate(){
			value = new List<string>();
		}
	}
}