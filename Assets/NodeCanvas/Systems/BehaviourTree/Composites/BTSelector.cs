using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("Selector")]
	[Category("Composites")]
	[Description("Execute the child nodes in order or randonly until the first that returns Success and return Success as well. If none returns Success, then returns Failure.\nIf is Dynamic, then higher priority children Status are revaluated and if one returns Success the Selector will select that one and bail out immediately in Success too")]
	[Icon("Selector")]
	public class BTSelector : BTComposite{

		public bool isDynamic;
		public bool isRandom;

		private int lastRunningNodeIndex= 0;

		public override string name{
			get {return string.Format("<color=#b3ff7f>{0}</color>", base.name.ToUpper());}
		}

		protected override Status OnExecute(Component agent, Blackboard blackboard){
/*

			if (isDynamic && status != Status.Success){
				for (int i = 0; i < lastRunningNodeIndex; i++){
					if (outConnections[i].Execute(agent, blackboard) == Status.Success){
						outConnections[lastRunningNodeIndex].ResetConnection();
						return Status.Success;
					}
				}
			}

			if (lastRunningNodeIndex >= outConnections.Count)
				return Status.Failure;

			status = outConnections[lastRunningNodeIndex].Execute(agent, blackboard);

			if (status != Status.Failure)
				return status;

			lastRunningNodeIndex++;
			return OnExecute(agent, blackboard);
*/


			for ( int i= isDynamic? 0 : lastRunningNodeIndex; i < outConnections.Count; i++){

				status = outConnections[i].Execute(agent, blackboard);
				
				if (status == Status.Running){

					if (isDynamic && i < lastRunningNodeIndex)
						outConnections[lastRunningNodeIndex].ResetConnection();

					lastRunningNodeIndex = i;
					return Status.Running;
				}

				if (status == Status.Success){
					
					if (isDynamic && i < lastRunningNodeIndex)
						outConnections[lastRunningNodeIndex].ResetConnection();

					return Status.Success;
				}
			}

			return Status.Failure;

		}

		protected override void OnReset(){
			lastRunningNodeIndex = 0;
			if (isRandom)
				outConnections = Shuffle(outConnections);
		}

		public override void OnChildDisconnected(int index){
			if (index != 0 && index == lastRunningNodeIndex)
				lastRunningNodeIndex--;
		}

		public override void OnGraphStarted(){
			OnReset();
		}

		//Fisher-Yates shuffle algorithm
		private List<Connection> Shuffle(List<Connection> list){
			for ( int i= list.Count -1; i > 0; i--){
				var j = (int)Mathf.Floor(Random.value * (i + 1));
				var temp = list[i];
				list[i] = list[j];
				list[j] = temp;
			}

			return list;
		}

		
		/////////////////////////////////////////
		/////////GUI AND EDITOR STUFF////////////
		/////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnNodeGUI(){

			if (isDynamic)
				GUILayout.Label("<b>DYNAMIC</b>");
			if (isRandom)
				GUILayout.Label("<b>RANDOM</b>");
		}

		#endif
	}
}