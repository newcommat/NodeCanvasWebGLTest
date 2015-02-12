using UnityEngine;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class Vector3Data : VariableData<Vector3>{

		public override object GetSerialized(){
			return new float[] {GetValue().x, GetValue().y, GetValue().z};
		}

		public override void SetSerialized(object obj){
			var floatArr = (float[])obj;
			SetValue( new Vector3(floatArr[0], floatArr[1], floatArr[2]) );
		}
	}
}