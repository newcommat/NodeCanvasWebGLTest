using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("Movement")]
	[Description("Find the closes Navigation Mesh position to the target position")]
	public class FindClosestEdge : ActionTask {

		public BBVector targetPosition;
		[BlackboardOnly]
		public BBVector saveFoundPosition;

		private NavMeshHit hit;

		protected override void OnExecute(){
			if (NavMesh.FindClosestEdge(targetPosition.value, out hit, -1)){
				saveFoundPosition.value = hit.position;
				EndAction(true);
			}

			EndAction(false);
		}
	}
}