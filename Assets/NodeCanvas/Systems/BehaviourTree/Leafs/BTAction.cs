using UnityEngine;
using System.Collections;
using System.Reflection;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("Action")]
	[Description("Executes an action and returns Success or Failure. Returns Running until the action is finished")]
	[Icon("Action")]
	///Executes an action and returns Success or Failure. Returns Running until the action is finished
	public class BTAction : BTNodeBase, ITaskAssignable<ActionTask>{

		[SerializeField]
		private Object _action;

		public Object serializedTask{
			get {return _action;}
		}

		public Task task{
			get {return action;}
			set {action = (ActionTask)value;}
		}

		private ActionTask action{
			get { return _action as ActionTask; }
			set
			{
				_action = value;
				if (value != null)
					value.SetOwnerSystem(graph);
			}
		}

		public override string name{
			get {return base.name.ToUpper();}
		}


		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (action == null)
				return Status.Success;

			if (status == Status.Resting || status == Status.Running)
				return action.ExecuteAction(agent, blackboard);

			return status;
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