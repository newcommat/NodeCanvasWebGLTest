using UnityEngine;
using System.Collections;
namespace NodeCanvas.DialogueTrees{

	[AddComponentMenu("")]
	[Name("Action")]
	[Description("Execute an Action Task for the Dialogue Actor selected. The Blackboard will be taken from the selected Actor.")]
	public class DLGActionNode : DLGNodeBase, ITaskAssignable<ActionTask>{

		[SerializeField]
		private Object _action;

		[HideInInspector]
		public Task task{
			get {return action;}
			set {action = (ActionTask)value;}
		}

		public Object serializedTask{
			get {return _action;}
		}

		private ActionTask action{
			get {return _action as ActionTask;}
			set
			{
				_action = value;
				if (value != null)
					value.SetOwnerSystem(this);
			}
		}

		public override string name{
			get{return base.name + " " + finalActorName;}
		}

		protected override Status OnExecute(){

			if (!finalActor){
				DLGTree.StopGraph();
				return Error("Actor not found");
			}

			if (!action){
				OnActionEnd(true);
				return Status.Success;
			}

			DLGTree.currentNode = this;

			status = Status.Running;
			StartCoroutine(DoUpdate());
			return status;
		}

		IEnumerator DoUpdate(){

			while(status == Status.Running){
				var s = action.ExecuteAction(finalActor, finalBlackboard);
				if (s != Status.Running){
					OnActionEnd(s == Status.Success? true : false);
					yield break;
				}
				
				yield return null;
			}

		}

		void OnActionEnd(bool success){

			if (success){
				Continue();
				return;
			}

			status = Status.Failure;
			DLGTree.StopGraph();
		}

		protected override void OnReset(){
			if (action)
				action.EndAction(null);
		}

		public override void OnGraphPaused(){
			if (action)
				action.PauseAction();
		}
	}
}