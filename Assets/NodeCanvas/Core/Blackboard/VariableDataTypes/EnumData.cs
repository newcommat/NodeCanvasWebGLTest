using UnityEngine;
using System;
using System.Linq;

namespace NodeCanvas.Variables{

	[AddComponentMenu("")]
	public class EnumData : VariableData<Enum> {

		enum DefaultEnum{
			Zero,
			One,
			Two,
			Three,
			Four,
			Five
		}

		[SerializeField]
		private string stringValue;
		[SerializeField]
		private string _typeName = typeof(DefaultEnum).AssemblyQualifiedName;

		public override System.Type varType{
			get {return System.Type.GetType(_typeName);}
			set
			{
				if (value.NCIsSubclassOf(typeof(System.Enum))){
					_typeName = value.AssemblyQualifiedName;
					stringValue = Enum.GetNames(value)[0];
				}
			}
		}

		public override void OnCreate(){
			varType = typeof(DefaultEnum);
		}

		public override Enum GetValue(){
			value = (Enum)Enum.Parse(varType, stringValue);
			return base.GetValue();
		}

		public override void SetValue(Enum o){
			if (o is System.Enum && varType != o.GetType() )
				varType = o.GetType();
			stringValue = Enum.GetName(varType, o);
			base.SetValue(value);
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		public override void OnVariableGUI(){
			if (!isBinded && GUILayout.Button("T", GUILayout.Width(10), GUILayout.Height(10))){
				EditorUtils.ShowConfiguredTypeSelectionMenu(typeof(Enum), delegate(System.Type t){
					varType = t;
				}, false);
			}
		}

		#endif
	}
}