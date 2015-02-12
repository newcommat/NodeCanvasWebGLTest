using UnityEngine;
using System.Collections;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("Condition")]
	[Description("Check a condition and return Success or Failure")]
	[Icon("Condition")]
	public class BTCondition : BTNodeBase, ITaskAssignable<ConditionTask>{

		[SerializeField]
		private Object _condition;

		public Task task{
			get {return condition;}
			set {condition = (ConditionTask)value;}
		}

		public Object serializedTask{
			get {return _condition;}
		}

		private ConditionTask condition{
			get{return _condition as ConditionTask;}
			set
			{
				_condition = value;
				if (value != null)
					value.SetOwnerSystem(graph);
			}
		}

		public override string name{
			get{return base.name.ToUpper();}
		}

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (condition)
				return condition.CheckCondition(agent, blackboard)? Status.Success: Status.Failure;
			return Status.Failure;
		}
	}
}