using UnityEngine;
using System.Collections;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("✫ Blackboard")]
	public class SetEnum : ActionTask {

		[BlackboardOnly] [RequiredField]
		public BBEnum valueA;
		public BBEnum valueB;

		protected override string info{
			get {return valueA + " = " + valueB;}
		}

		protected override void OnExecute(){
			valueA.value = valueB.value;
			EndAction();
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR
		
		protected override void OnTaskInspectorGUI(){

			EditorUtils.BBVariableField("Value A", valueA);
			
			if (GUI.changed)
				(valueB as IMultiCastable).type = valueA.isNull? typeof(System.Enum) : valueA.value.GetType();

			EditorUtils.BBVariableField("Value B", valueB);
		}
		
		#endif
	}
}