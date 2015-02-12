using UnityEngine;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class SystemObjectData : VariableData<object>{

		public override System.Type varType{
			get {return value != null? value.GetType() : typeof(object);}
		}
	}
}