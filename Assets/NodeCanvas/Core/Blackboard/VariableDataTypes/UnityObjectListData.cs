using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class UnityObjectListData : VariableData<List<Object>> {

		public override void OnCreate(){
			value = new List<Object>();
		}

		public override object GetSerialized(){
			return null;
		}
	}
}