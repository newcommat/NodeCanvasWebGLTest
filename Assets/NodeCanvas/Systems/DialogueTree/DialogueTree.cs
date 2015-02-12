using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.DialogueTrees{

	[AddComponentMenu("")]
	///A Dialogue Tree container
	public class DialogueTree : Graph {

		public enum EndState
		{
			Failure = 0,
			Success = 1,
			None    = 3
		}

		public EndState endState = EndState.None;

		[SerializeField]
		private List<string> _dialogueActorNames = new List<string>();
		private Dictionary<string, DialogueActor> actorReferences = new Dictionary<string, DialogueActor>();
		private DLGNodeBase _currentNode;

		///The current executing node
		public DLGNodeBase currentNode{
			get {return _currentNode;}
			set {_currentNode = value;}
		}

		///The actor names that are inputed and available to act
		public List<string> dialogueActorNames{
			get {return _dialogueActorNames;}
			set {dialogueActorNames = value;}
		}

		public override bool autoSort{
			get {return true;}
		}

		public override System.Type baseNodeType{
			get {return typeof(DLGNodeBase);}
		}

		protected override bool allowNullAgent{
			get{return true;}
		}

		public DialogueActor GetActorReference(string actorName){
			if (!actorReferences.ContainsKey(actorName) || actorReferences[actorName] == null)
				actorReferences[actorName] = DialogueActor.Find(actorName);
			return actorReferences[actorName];
		}

/*
		///Assign a named DialogueActor reference to be used by the dialogue tree
		public void AssignActor(string actorName, DialogueActor actor){
			if (!dialogueActorNames.Contains(actorName)){
				Debug.LogWarning("Dialogue Actor with name '" + actorName + "' is not set to be used in Dialogue Tree");
				return;
			}
			actorReferences[actorName] = actor;
		}

		///Assign all named DialogueActor references to be used by the dialogue tree provided in a dictionary
		public void AssignActors(Dictionary<string, DialogueActor> nameActorPairs){
			foreach (var pair in nameActorPairs){
				if ( !dialogueActorNames.Contains(pair.Key) )
					Debug.LogWarning("Dialogue Actor with name '" + pair.Key + "' is not set to be used in Dialogue Tree");
			}
			actorReferences = nameActorPairs;
		}
*/

		protected override void OnGraphStarted(){

			if (agent != null){
				var actor = agent.GetComponent<DialogueActor>();
				if (actor == null){
					Debug.Log("Dialogue Tree Agent has no Dialogue Actor Component. Adding one now...", agent.gameObject);
					actor = agent.gameObject.AddComponent<DialogueActor>();
					actor.blackboard = actor.GetComponent<Blackboard>();
					if (actor.blackboard == null){
						Debug.Log("Dialogue Tree Agent game object has no Blackboard. Adding one now and assigning it to the Dialogue Actor...", agent.gameObject);
						actor.blackboard = actor.gameObject.AddComponent<Blackboard>();
					}
					actor.name = actor.gameObject.name;
					agent = actor;
				}
			
			} else {

				Debug.Log("Dialogue Tree started with no Agent to be used as 'Owner'. A default one will be created now...", this.gameObject);
				var actor = this.gameObject.AddComponent<DialogueActor>();
				actor.blackboard = this.gameObject.AddComponent<Blackboard>();
				actor.name = "Default";
				agent = actor;
			}

			if (blackboard == null)
				blackboard = (agent as DialogueActor).blackboard;

			actorReferences.Clear();

			foreach (string actorName in dialogueActorNames)
				actorReferences[actorName] = DialogueActor.Find(actorName);

			foreach (KeyValuePair<string, DialogueActor> pair in actorReferences){
				if (pair.Value == null)
					Debug.LogWarning(pair.Key + " DialogueActor not found when starting Dialogue Tree " + name + ". Dialogue will play but if a node with that actor is encountered an error will show and the Dialogue will Stop", gameObject);
			}

			//DLGNodes implement ITaskDefaults to provide defaults for the tasks they contain based on the dialogue actor selected for the node
			//This SendTaskOwnerDefaults is send after the graph's SendTaskOwnerDefaults so in essence it overrides it
			foreach (DLGNodeBase node in allNodes)
				node.SendTaskOwnerDefaults();

			EventHandler.Dispatch(DLGEvents.OnDialogueStarted, this);
			
			currentNode = currentNode != null? currentNode : (DLGNodeBase)primeNode;
			currentNode.Execute();
		}

		protected override void OnGraphStoped(){

			endState = currentNode? (EndState)currentNode.status : EndState.Success;

			EventHandler.Dispatch(DLGEvents.OnDialogueFinished, this);
			actorReferences.Clear();
			currentNode = null;
		}

		protected override void OnGraphPaused(){

			EventHandler.Dispatch(DLGEvents.OnDialoguePaused, this);
		}

		////////////////////////////////////////
		#if UNITY_EDITOR
		
		[UnityEditor.MenuItem("Window/NodeCanvas/Create Dialogue Tree")]
		public static void CreateDialogueTree(){
			DialogueTree newDLG = new GameObject("DialogueTree").AddComponent(typeof(DialogueTree)) as DialogueTree;
			UnityEditor.Selection.activeObject = newDLG;
		}		
		
		#endif
	}
}