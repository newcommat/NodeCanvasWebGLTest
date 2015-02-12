using UnityEngine;
using System.Collections.Generic;
using NodeCanvas.Variables;
using System.Linq;

namespace NodeCanvas.BehaviourTrees{

	[Name("Priority Selector")]
	[Category("Composites")]
	[Description("Very similar to the normal Selector, but executes it's child nodes in an order sorted by their Priority")]
	public class BTPrioritySelector : BTComposite {

		[SerializeField]
		private List<BBFloat> priorities = new List<BBFloat>();

		private List<Connection> orderedConnections = new List<Connection>();
		private int current = 0;

		public override void OnChildConnected(int index){
			priorities.Insert(index, new BBFloat{value = 1, bb = graphBlackboard});
			UpdateNodeBBFields(graphBlackboard);
		}

		public override void OnChildDisconnected(int index){
			priorities.RemoveAt(index);
		}

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (status == Status.Resting)
				orderedConnections = outConnections.OrderBy(c => priorities[outConnections.IndexOf(c)].value).Reverse().ToList();

			for (int i = current; i < orderedConnections.Count; i++){
				status = orderedConnections[i].Execute(agent, blackboard);
				if (status == Status.Success){
					return Status.Success;
				}

				if (status == Status.Running){
					current = i;
					return Status.Running;
				}
			}

			return Status.Failure;
		}

		protected override void OnReset(){
			current = 0;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		public override string GetConnectionInfo(Connection connection){
			var i = outConnections.IndexOf(connection);
			return priorities[i].ToString();
		}

		protected override void OnNodeInspectorGUI(){
			for (int i = 0; i < priorities.Count; i++)
				priorities[i] = (BBFloat)EditorUtils.BBVariableField("Priority Weight", priorities[i]);
		}

		#endif
	}
}