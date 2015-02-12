using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class QuaternionData : VariableData<Quaternion>{
		
		public override object GetSerialized(){
			return new float[] {GetValue().x, GetValue().y, GetValue().z, GetValue().w};
		}

		public override void SetSerialized(object obj){
			var floatArr = obj as float[];
			SetValue( new Quaternion(floatArr[0], floatArr[1], floatArr[2], floatArr[3]) );
		}
	}
}