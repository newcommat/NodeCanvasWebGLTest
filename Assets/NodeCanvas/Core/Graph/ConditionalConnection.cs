using UnityEngine;
using System.Collections;

namespace NodeCanvas{

	[AddComponentMenu("")]
	///A connection that holds a Condition Task
	public class ConditionalConnection : Connection, ITaskAssignable<ConditionTask>{

		[SerializeField]
		private Object _condition;

		public Task task{
			get {return condition;}
			set {condition = (ConditionTask)value;}
		}

		public Object serializedTask{
			get {return _condition;}
		}

		public ConditionTask condition{
			get {return _condition as ConditionTask;}
			set
			{
				_condition = value;
				if (value != null)
					value.SetOwnerSystem(graph);
			}
		}

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (!condition || condition.CheckCondition(agent, blackboard))
				return targetNode.Execute(agent, blackboard);

			targetNode.ResetNode();
			return Status.Failure;
		}

		public bool CheckCondition(){
			return CheckCondition(graphAgent, graphBlackboard);
		}

		public bool CheckCondition(Component agent){
			return CheckCondition(agent, graphBlackboard);
		}

		///To be used if and when want to just check the connection without execution, since OnExecute this is called as well to determine return status.
		virtual public bool CheckCondition(Component agent, Blackboard blackboard){

			if ( isActive && (!condition || condition.CheckCondition(agent, blackboard) ) )
				return true;

			connectionStatus = Status.Failure;
			return false;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override string GetConnectionInfo(bool isExpanded, ref float alpha){
			alpha = condition != null? 0.8f : alpha;
			if (isExpanded){
				return condition? condition.summaryInfo : "No Condition";
			} else {
				return condition? "-||-" : "---";
			}
		}

		protected override void OnConnectionInspectorGUI(){

			if (!condition){
				EditorUtils.TaskSelectionButton(gameObject, typeof(ConditionTask), delegate(Task c){condition = (ConditionTask)c;});
			} else {
				condition.ShowInspectorGUI();
			}
		}

		#endif
	}
}