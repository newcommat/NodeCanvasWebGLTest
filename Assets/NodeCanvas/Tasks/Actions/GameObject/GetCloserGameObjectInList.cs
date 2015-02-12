using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("GameObject")]
	[AgentType(typeof(Transform))]
	[Description("Get the closer game object to the agent from within a list of game objects and save it in the blackboard.")]
	public class GetCloserGameObjectInList : ActionTask {

		[RequiredField]
		public BBGameObjectList list;
		
		[BlackboardOnly]
		public BBGameObject saveAs;

		protected override string info{
			get {return "Get Closer from '" + list + "' as " + saveAs;}
		}

		protected override void OnExecute(){

			if (list.value.Count == 0){
				EndAction(false);
				return;
			}

			float closerDistance = Mathf.Infinity;
			GameObject closerGO = null;
			foreach(GameObject go in list.value){
				var dist = Vector3.Distance(agent.transform.position, go.transform.position);
				if (dist < closerDistance){
					closerDistance = dist;
					closerGO = go;
				}
			}

			saveAs.value = closerGO;
			EndAction(true);
		}
	}
}