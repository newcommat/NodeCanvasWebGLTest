#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections;

namespace NodeCanvas{

	///Generic version to define the AgentType instead of using the [AgentType] attribute
	abstract public class ActionTask<T> : ActionTask where T:Component{
		public override System.Type agentType{
			get {return typeof(T);}
		}

		new public T agent{
			get
			{
				try { return (T)base.agent; }
				catch {return null;}
			}
		}		
	}

	///Base class for all actions. Extend this to create your own.
	abstract public class ActionTask : Task{

		private Status status = Status.Resting;
		private float startTime;
		private float pauseTime;
		private bool latch = false;

		///The time in seconds this action is running if at all
		public float elapsedTime{
			get
			{
				if (isPaused)
					return pauseTime - startTime;
				if (isRunning)
					return Time.time - startTime;
				return 0;
			}
		}

		///Is the action currently running?
		public bool isRunning{
			get {return status == Status.Running;}
		}

		///Is the action currently paused?
		public bool isPaused{get; private set;}

		sealed override public string summaryInfo{
			get {return (agentIsOverride? "* " : "") + info;}
		}

		///Override in your own actions to provide the visible editor action info whenever it's shown
		virtual protected string info{
			get {return name;}
		}

	
		////////
		////////

		///Used to call an action providing a callback. Not used in NC
		public void ExecuteAction(Component agent, System.Action<bool> callback){
			ExecuteAction(agent, null, callback);
		}
		///Used to call an action providing a callback. Not used in NC
		public void ExecuteAction(Component agent, Blackboard blackboard, System.Action<bool> callback){
			if (!isRunning)
				MonoManager.current.StartCoroutine(ActionUpdater(agent, blackboard, callback));			
		}

		//The internal updater for when an action has been called with a callback parameter
		IEnumerator ActionUpdater(Component agent, Blackboard blackboard, System.Action<bool> callback){
			while(ExecuteAction(agent, blackboard) == Status.Running)
				yield return null;
			if (callback != null)
				callback(status == Status.Success? true : false);
		}

		///Executes the action for the provided agent and blackboard
		public Status ExecuteAction(Component agent, Blackboard blackboard){

			if (isPaused){ //is resume
				startTime += Time.time - pauseTime;
				isPaused = false;
				enabled = true;
			}

			if (status == Status.Running){
				OnUpdate();
				latch = false;
				return status;
			}

			if (latch){ //to be possible to call EndAction anywhere
				latch = false;
				return status;
			}

			if (!isActive)
				return Status.Failure;

			if (!Set(agent, blackboard)){
				isActive = false;
				return Status.Failure;
			}

			enabled = true;
			startTime = Time.time;
			status = Status.Running;
			OnExecute();
			latch = false;
			return status;
		}

		///Ends the action either in success or failure. Ending with null means that it's a cancel/interrupt. Used by the external system.
		public void EndAction(){ EndAction(true); }
		public void EndAction(bool? success){

			if (status != Status.Running)
				return;
			
			enabled = false;
			isPaused = false;
			status = success == true? Status.Success : Status.Failure;
			latch = success != null? true : false;
			OnStop();
		}

		///Pause the action from updating and calls OnPause
		public void PauseAction(){
			
			if (status != Status.Running)
				return;

			pauseTime = Time.time;
			enabled = false;
			isPaused = true;
			OnPause();
		}

		///Override in your own actions. Called once when the actions is executed.
		virtual protected void OnExecute(){}

		///Override in your own actions. Called every frame, if and while the action is running and until it ends.
		virtual protected void OnUpdate(){}
		
		///Called whenever the action ends due to any reason.
		virtual protected void OnStop(){}

		///Called when the action is paused
		virtual protected void OnPause(){}


		//////////////////////////////////
		/////////GUI & EDITOR STUFF///////
		//////////////////////////////////
		#if UNITY_EDITOR

		///Editor: Draw the action's controls.
		sealed protected override void SealedInspectorGUI(){
			if (Application.isPlaying){
				if (elapsedTime > 0) GUI.color = Color.yellow;
				EditorGUILayout.LabelField("Elapsed Time", elapsedTime.ToString());
				GUI.color = Color.white;
			}
		}

		#endif
	}
}