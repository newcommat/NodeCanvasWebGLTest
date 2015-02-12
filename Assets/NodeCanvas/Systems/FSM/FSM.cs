using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace NodeCanvas.StateMachines{

	[AddComponentMenu("")]
	///The actual State Machine
	public class FSM : Graph{

		private FSMState currentState;
		private FSMState lastState;
		private List<FSMAnyState> anyStates;
		private List<FSMConcurrentState> concurrentStates;
		System.Action<IState> CallbackEnter;
		System.Action<IState> CallbackStay;
		System.Action<IState> CallbackExit;

		///The current state name. Null if none
		public string currentStateName{
			get {return currentState != null? currentState.name : null; }
		}

		///The last state name. Not the current! Null if none
		public string lastStateName{
			get	{return lastState != null? lastState.name : null; }
		}

		public override System.Type baseNodeType{
			get {return typeof(FSMState);}
		}

		protected override void OnGraphStarted(){

			GatherMethodInfo();
			anyStates = GetAllNodesOfType<FSMAnyState>();
			concurrentStates = GetAllNodesOfType<FSMConcurrentState>();
			foreach (FSMConcurrentState concurrentState in concurrentStates)
				concurrentState.Execute(agent, blackboard);

			//if it's not null it means we were paused, so it will resume
			if (currentState == null)
				EnterState(lastState == null? primeNode as FSMState : lastState);
		}

		protected override void OnGraphUpdate(){

			//do this first
			if (currentState.status != Status.Running && currentState.outConnections.Count == 0){
				StopGraph();
				return;
			}

			//update Concurent States
			for (int i = 0; i < concurrentStates.Count; i++){
				if (concurrentStates[i].status == Status.Running)
					concurrentStates[i].OnUpdate();
			}

			//update Any States
			for (int i = 0; i < anyStates.Count; i++)
				anyStates[i].UpdateAnyState();

			//Update current state
			currentState.OnUpdate();
			
			if (currentState.status == Status.Running && CallbackStay != null)
				CallbackStay(currentState);
		}

		protected override void OnGraphStoped(){
			lastState = null;
			currentState = null;
		}

		protected override void OnGraphPaused(){
			lastState = currentState;
		}

		///Enter a state providing the state itself
		public bool EnterState(FSMState newState){

			if (!isRunning){
				Debug.LogWarning("Tried to EnterState on an FSM that was not running", gameObject);
				return false;
			}

			if (newState == null){
				Debug.LogWarning("Tried to Enter Null State");
				return false;
			}

			if (currentState == newState){
				Debug.Log("Trying entering the same state");
				return false;
			}

			if (currentState != null){	
				currentState.Finish();
				currentState.ResetNode();
				if (CallbackExit != null)
					CallbackExit(currentState);
				
				//for editor..
				foreach (Connection inConnection in currentState.inConnections)
					inConnection.connectionStatus = Status.Resting;
				///
			}

			lastState = currentState;
			currentState = newState;
			currentState.Execute(agent, blackboard);
			if (CallbackEnter != null)
				CallbackEnter(currentState);
			return true;
		}

		///Trigger a state to enter by it's name. Returns the state found and entered if any
		public FSMState TriggerState(string stateName){

			var state = GetStateWithName(stateName);
			if (state != null){
				EnterState(state);
				return state;
			}

			Debug.LogWarning("No State with name '" + stateName + "' found on FSM '" + name + "'");
			return null;
		}

		///Get all State Names
		public List<string> GetStateNames(){

			var names = new List<string>();
			foreach(FSMState node in allNodes){
				if (node.allowAsPrime)
					names.Add(node.name);
			}
			return names;
		}

		///Get a state by it's name
		public FSMState GetStateWithName(string name){

			foreach (FSMState node in allNodes){
				if (node.allowAsPrime && node.name == name)
					return node;
			}
			return null;
		}

		void GatherMethodInfo(){

			foreach (MonoBehaviour mono in agent.gameObject.GetComponents<MonoBehaviour>()){
                
				var enterMethod = mono.GetType().NCGetMethod("OnStateEnter");
				var stayMethod = mono.GetType().NCGetMethod("OnStateUpdate");
				var exitMethod = mono.GetType().NCGetMethod("OnStateExit");

				if (enterMethod != null)
					CallbackEnter += enterMethod.NCCreateDelegate<System.Action<IState>>(mono);
				if (stayMethod != null)
					CallbackStay += stayMethod.NCCreateDelegate<System.Action<IState>>(mono);
				if (exitMethod != null)
					CallbackExit += exitMethod.NCCreateDelegate<System.Action<IState>>(mono);
			}
		}

		////////////////////////////////////////
		#if UNITY_EDITOR
		
		[UnityEditor.MenuItem("Window/NodeCanvas/Create FSM")]
		public static void CreateFSM(){
			FSM newFSM = new GameObject("FSM").AddComponent(typeof(FSM)) as FSM;
			UnityEditor.Selection.activeObject = newFSM;
		}		
		
		#endif
	}
}