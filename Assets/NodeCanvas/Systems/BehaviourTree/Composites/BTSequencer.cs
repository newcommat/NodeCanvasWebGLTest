using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("Sequencer")]
	[Category("Composites")]
	[Description("Execute the child nodes in order or randonly and return Success if all children return Success, else return Failure\nIf is Dynamic, higher priority child status is revaluated. If a child returns Failure the Sequencer will bail out immediately in Failure too.")]
	[Icon("Sequencer")]
	public class BTSequencer : BTComposite {

		public bool isDynamic;
		public bool isRandom;

		private int lastRunningNodeIndex= 0;

		public override string name{
			get{return string.Format("<color=#bf7fff>{0}</color>", base.name.ToUpper());}
		}

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			for ( int i= isDynamic? 0 : lastRunningNodeIndex; i < outConnections.Count; i++){

				status = outConnections[i].Execute(agent, blackboard);

				if (status == Status.Running){

					if (isDynamic && i < lastRunningNodeIndex)
						outConnections[lastRunningNodeIndex].ResetConnection();
						
					lastRunningNodeIndex = i;
					return Status.Running;
				}

				if (status == Status.Failure){

					if (isDynamic && i < lastRunningNodeIndex)
						outConnections[lastRunningNodeIndex].ResetConnection();

					return Status.Failure;
				}
			}

			return Status.Success;



/*
			if (isDynamic && status != Status.Failure){
				for (int i = 0; i < lastRunningNodeIndex; i++){
					if (outConnections[i].Execute(agent, blackboard) == Status.Failure){
						outConnections[lastRunningNodeIndex].ResetConnection();
						return Status.Failure;
					}
				}
			}

			if (lastRunningNodeIndex >= outConnections.Count)
				return Status.Success;

			status = outConnections[lastRunningNodeIndex].Execute(agent, blackboard);

			if (status != Status.Success)
				return status;

			lastRunningNodeIndex++;
			return OnExecute(agent, blackboard);
*/
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