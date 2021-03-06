﻿using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Conditions{

	[Category("✫ Blackboard")]
	public class CheckBoolean : ConditionTask{

		[BlackboardOnly]
		public BBBool valueA;
		public BBBool valueB;

		protected override string info{
			get {return valueA + " == " + valueB;}
		}

		protected override bool OnCheck(){

			return valueA.value == valueB.value;
		}
	}
}