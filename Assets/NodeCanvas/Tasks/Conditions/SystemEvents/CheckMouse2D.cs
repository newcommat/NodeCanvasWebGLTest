﻿using UnityEngine;
using System.Collections;

namespace NodeCanvas.Conditions{

	[Category("System Events")]
	[Name("Check Mouse 2D")]
	[EventListener("OnMouseEnter", "OnMouseExit", "OnMouseOver")]
	[AgentType(typeof(Collider2D))]
	public class CheckMouse2D : ConditionTask {

		public enum CheckTypes
		{
			MouseEnter = 0,
			MouseExit  = 1,
			MouseOver  = 2
		}
		
		public CheckTypes checkType = CheckTypes.MouseEnter;

		protected override string info{
			get {return checkType.ToString();}
		}

		protected override bool OnCheck(){
			return false;
		}

		public void OnMouseEnter(){
			if (checkType == CheckTypes.MouseEnter)
				YieldReturn(true);
		}

		public void OnMouseExit(){
			if (checkType == CheckTypes.MouseExit)
				YieldReturn(true);
		}

		public void OnMouseOver(){
			if (checkType == CheckTypes.MouseOver)
				YieldReturn(true);
		}
	}
}