using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	[System.Obsolete]
	[AddComponentMenu("")]
	public class ComponentListData : VariableData<List<Component>> {

		public override object GetSerialized(){
			return null;
		}

		public override void SetSerialized(object o){

		}
	}
}