using UnityEngine;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class Vector2Data : VariableData<Vector2>{

		public override object GetSerialized(){
			return new float[] {GetValue().x, GetValue().y};
		}

		public override void SetSerialized(object obj){
			var floatArr = (float[])obj;
			SetValue( new Vector2(floatArr[0], floatArr[1]) );
		}
	}
}