using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("✫ Blackboard")]
	public class GetToString : ActionTask {

		public BBVar variable;
		[BlackboardOnly]
		public BBString toString;

		protected override void OnExecute(){

			toString.value = !variable.isNull? variable.value.ToString() : "NULL";
			EndAction();
		}
	}
}