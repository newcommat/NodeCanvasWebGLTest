﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NodeCanvas.Variables;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("Iterate")]
	[Category("Decorators")]
	[Description("Iterate a game object list and execute the child node for each object in the list. Keeps iterating until the Termination Condition is met or the whole list is iterated and return the child node status")]
	[Icon("List")]
	///Iterate a GameObject list
	public class BTIterator : BTDecorator{

		[BlackboardOnly]
		public BBGameObjectList list;
		[BlackboardOnly]
		public BBGameObject current;
		public BBInt maxIteration;

		public enum TerminationConditions {FirstSuccess, FirstFailure, None}
		public TerminationConditions terminationCondition = TerminationConditions.None;
		public bool resetIndex = true;

		private int currentIndex;
		
		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (!decoratedConnection)
				return Status.Resting;

			if (list.value == null || list.value.Count == 0)
				return Status.Failure;

			current.value = list.value[currentIndex];
			status = decoratedConnection.Execute(agent, blackboard);

			if (status == Status.Success && terminationCondition == TerminationConditions.FirstSuccess)
				return Status.Success;

			if (status == Status.Failure && terminationCondition == TerminationConditions.FirstFailure)
				return Status.Failure;

			if (status != Status.Running){

				if (currentIndex == list.value.Count - 1 || currentIndex == maxIteration.value - 1)
					return status;

				decoratedConnection.ResetConnection();
				currentIndex ++;
				return Status.Running;
			}

			return status;
		}


		protected override void OnReset(){

			if (resetIndex || currentIndex == list.value.Count - 1 || currentIndex == maxIteration.value - 1)
				currentIndex = 0;
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnNodeGUI(){

			var leftLabelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
			leftLabelStyle.alignment = TextAnchor.UpperLeft;

			GUILayout.Label("For Each \t'<b>$" + current.dataName + "</b>'\nIn \t'<b>$" + list.dataName + "</b>'", leftLabelStyle);
			if (terminationCondition != TerminationConditions.None)
				GUILayout.Label("Exit on " + terminationCondition.ToString());

			if (Application.isPlaying)
				GUILayout.Label("Index: " + currentIndex.ToString() + " / " + (list.value != null && list.value.Count != 0? (list.value.Count -1).ToString() : "?") );
		}

		#endif
	}
}