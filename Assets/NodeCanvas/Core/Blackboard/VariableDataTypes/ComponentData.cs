using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Variables{

	[System.Obsolete]
	[AddComponentMenu("")]
	public class ComponentData : VariableData<Component>{

		[SerializeField]
		private string _typeName = typeof(Component).AssemblyQualifiedName;

		public override System.Type varType{
			get {return System.Type.GetType(_typeName);}
			set
			{
				_typeName = value.AssemblyQualifiedName;
				if (this.objectValue != null && !value.NCIsAssignableFrom(this.objectValue.GetType()) )
					this.objectValue = null;
			}
		}

		public override object GetSerialized(){
			return null;
		}

		public override void SetSerialized(object obj){

		}
	}
}