using UnityEngine;
using System.Collections;

namespace NodeCanvas.Actions{

	[Category("✫ Utility")]
	[Description("Start, Resume, Pause, Stop a GraphOwner's behaviour")]
	[AgentType(typeof(GraphOwner))]
	public class GraphOwnerControl : ActionTask {

		public enum Control
		{
			StartBehaviour,
			StopBehaviour,
			PauseBehaviour
		}

		public Control control = Control.StartBehaviour;
		public bool waitUntilFinish = true;
		private bool isFinished = false;

		protected override string info{
			get {return agentInfo + "." + control.ToString();}
		}

		protected override void OnExecute(){
			if (control == Control.StartBehaviour){
				isFinished = false;
				if (waitUntilFinish){
					(agent as GraphOwner).StartBehaviour( delegate {isFinished = true;} );
				} else {
					(agent as GraphOwner).StartBehaviour();
					EndAction();
				}
			}
		}

		//These should take place here to be called 1 frame later, in case target is the same agent.
		protected override void OnUpdate(){

			if (control == Control.StartBehaviour){
				if (isFinished)
					EndAction();
				return;
			}

			if (control == Control.StopBehaviour){

				(agent as GraphOwner).StopBehaviour();
				EndAction();

			} else if (control == Control.PauseBehaviour){

				(agent as GraphOwner).PauseBehaviour();
				EndAction();
			}
		}
	}
}