using UnityEngine;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class UnityObjectData : VariableData<Object> {

		[SerializeField]
		private string _typeName = typeof(Object).AssemblyQualifiedName;

		public override System.Type varType{
			get {return System.Type.GetType(_typeName); }
			set
			{
				_typeName = value.AssemblyQualifiedName;
				if (this.objectValue != null && !value.NCIsAssignableFrom(this.objectValue.GetType()))
					this.objectValue = null;
			}
		}

		//////////////////////////
		///////EDITOR/////////////
		//////////////////////////
		#if UNITY_EDITOR

		public override void OnVariableGUI(){
			if (!isBinded && GUILayout.Button("T", GUILayout.Width(10), GUILayout.Height(10)))
				EditorUtils.ShowConfiguredTypeSelectionMenu(typeof(Object), delegate(System.Type t){varType = t;} );
		}

		#endif
	}
}