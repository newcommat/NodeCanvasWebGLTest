using UnityEngine;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class ColorData : VariableData<Color> {

		public override System.Object GetSerialized(){
			return new float[] {GetValue().r, GetValue().g, GetValue().b, GetValue().a};
		}

		public override void SetSerialized(System.Object obj){
			var floatArr = (float[])obj;
			SetValue( new Color(floatArr[0], floatArr[1], floatArr[2], floatArr[3]) );
		}
	}
}