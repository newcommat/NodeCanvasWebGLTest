using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Conditions{

	[Category("✫ Blackboard")]
	public class CheckInt : ConditionTask{

		[BlackboardOnly]
		public BBInt valueA;
		public CompareMethod checkType = CompareMethod.EqualTo;
		public BBInt valueB;

		protected override string info{
			get {return valueA + TaskTools.GetCompareString(checkType) + valueB;}
		}

		protected override bool OnCheck(){
			return TaskTools.Compare((int)valueA.value, (int)valueB.value, checkType);
		}
	}
}