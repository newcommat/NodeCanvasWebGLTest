using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	public class FloatListData : VariableData<List<float>> {

		public override void OnCreate(){
			value = new List<float>();
		}
	}
}