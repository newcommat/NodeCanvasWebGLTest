using UnityEngine;

namespace NodeCanvas.Conditions{

	[Category("✫ Utility")]
	[Description("Simply use to debug return true or false by inverting the condition if needed")]
	public class DebugCondition : ConditionTask{

		protected override string info{
			get {return "TRUE";}
		}

		protected override bool OnCheck(){
			return true;
		}
	}
}