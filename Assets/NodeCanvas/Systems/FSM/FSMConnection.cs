using UnityEngine;

namespace NodeCanvas.StateMachines{

	///The connection object for FSM nodes. Transitions
	[AddComponentMenu("")]
	public class FSMConnection : ConditionalConnection {

		///Perform the transition disregarding whether or not the condition (if any) is valid
		public void PerformTransition(){
			(graph as FSM).EnterState(targetNode as FSMState);
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override TipConnectionStyle tipConnectionStyle{
			get {return TipConnectionStyle.Arrow;}
		}
		
		#endif
	}
}