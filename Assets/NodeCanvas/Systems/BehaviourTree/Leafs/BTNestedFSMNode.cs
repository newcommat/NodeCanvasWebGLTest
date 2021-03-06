﻿using UnityEngine;
using System.Collections;
using NodeCanvas.StateMachines;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("FSM")]
	[Category("Nested")]
	[Description("NestedFSM can be assigned an entire FSM. This node will return Running for as long as the FSM is Running. If a Success or Failure State is selected, then it will return Success or Failure as soon as the Nested FSM enters that state at which point the FSM will also be stoped. If the Nested FSM ends otherwise, this node will return Success.")]
	[Icon("FSM")]
	public class BTNestedFSMNode : BTNodeBase, INestedNode{

		[SerializeField]
		private FSM _nestedFSM;
		private bool instantiated;

		public string successState;
		public string failureState;

		private FSM nestedFSM{
			get {return _nestedFSM;}
			set
			{
				_nestedFSM = value;
				if (_nestedFSM != null){
					_nestedFSM.agent = graphAgent;
					_nestedFSM.blackboard = graphBlackboard;
				}
			}
		}

		public Graph nestedGraph{
			get {return nestedFSM;}
			set {nestedFSM = (FSM)value;}
		}

		public override string name{
			get {return base.name.ToUpper();}
		}

		/////////

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			if (nestedFSM == null || nestedFSM.primeNode == null)
				return Status.Failure;

			CheckInstance();

			if (status == Status.Resting || nestedFSM.isPaused){
				status = Status.Running;
				nestedFSM.StartGraph(agent, blackboard, OnFSMFinish);
			}

			if (!string.IsNullOrEmpty(successState) && nestedFSM.currentStateName == successState){
				nestedFSM.StopGraph();
				return Status.Success;
			}

			if (!string.IsNullOrEmpty(failureState) && nestedFSM.currentStateName == failureState){
				nestedFSM.StopGraph();
				return Status.Failure;
			}

			return status;
		}

		void OnFSMFinish(){
			if (status == Status.Running)
				status = Status.Success;
		}

		protected override void OnReset(){
			if (nestedFSM)
				nestedFSM.StopGraph();
		}

		public override void OnGraphStarted(){
			if (nestedFSM)
				CheckInstance();
		}

		public override void OnGraphPaused(){
			if (nestedFSM)
				nestedFSM.PauseGraph();
		}

		public override void OnGraphStoped(){
			if (nestedFSM)
				nestedFSM.StopGraph();
		}

		void CheckInstance(){

			if (!instantiated && nestedFSM != null && nestedFSM.transform.parent != graph.transform){
				nestedFSM = (FSM)Instantiate(nestedFSM, transform.position, transform.rotation);
				nestedFSM.transform.parent = graph.transform;
				instantiated = true;	
			}
		}

		////////////////////////////
		//////EDITOR AND GUI////////
		////////////////////////////
		#if UNITY_EDITOR

		protected override void OnNodeGUI(){

		    if (nestedFSM){

		    	GUILayout.Label("'" + nestedFSM.name + "'");

		    	if (graph.isRunning)
			    	GUILayout.Label("State: " + nestedFSM.currentStateName);
			    	
			} else {
				
				if (GUILayout.Button("CREATE NEW"))
					nestedFSM = Graph.CreateNested(this, typeof(FSM), "Nested FSM") as FSM;
			}
		}

		protected override void OnNodeInspectorGUI(){

		    nestedFSM = UnityEditor.EditorGUILayout.ObjectField("FSM", nestedFSM, typeof(FSM), true) as FSM;

		    if (nestedFSM == null)
		    	return;

	    	nestedFSM.name = UnityEditor.EditorGUILayout.TextField("Name", nestedFSM.name);

		    successState = EditorUtils.StringPopup("Success State", successState, nestedFSM.GetStateNames(), false, true);
		    failureState = EditorUtils.StringPopup("Failure State", failureState, nestedFSM.GetStateNames(), false, true);
		    nestedFSM.ShowDefinedBBVariablesGUI();
		}

		#endif
	}
}