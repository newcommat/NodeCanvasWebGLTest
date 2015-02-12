using UnityEngine;
using System.Collections;
using NodeCanvas.Variables;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	[Name("SubTree")]
	[Category("Nested")]
	[Description("SubTree Node can be assigned an entire Sub BehaviorTree. The root node of that behaviour will be considered child node of this node and will return whatever it returns")]
	[Icon("BT")]
	public class BTSubTree : BTNodeBase, INestedNode{

		[SerializeField]
		private BehaviourTree _nestedTree;
		private bool instantiated;

		private BehaviourTree subTree{
			get {return _nestedTree;}
			set
			{
				_nestedTree = value;
				if (_nestedTree != null){
					_nestedTree.agent = graphAgent;
					_nestedTree.blackboard = graphBlackboard;
					instantiated = false;
				}
			}
		}

		public Graph nestedGraph{
			get {return subTree;}
			set {subTree = (BehaviourTree)value;}
		}

		public override string name{
			get {return base.name.ToUpper();}
		}

		/////////
		/////////

		protected override Status OnExecute(Component agent, Blackboard blackboard){

			CheckInstance();

			if (subTree && subTree.primeNode)
				return subTree.Tick(agent, blackboard);

			return Status.Success;
		}

		protected override void OnReset(){

			if (subTree && subTree.primeNode)
				subTree.primeNode.ResetNode();
		}

		public override void OnGraphStarted(){
			if (subTree){
				CheckInstance();
				foreach(Node node in subTree.allNodes)
					node.OnGraphStarted();				
			}
		}

		public override void OnGraphStoped(){
			if (subTree){
				foreach(Node node in subTree.allNodes)
					node.OnGraphStoped();				
			}			
		}

		public override void OnGraphPaused(){
			if (subTree){
				foreach(Node node in subTree.allNodes)
					node.OnGraphPaused();
			}
		}

		void CheckInstance(){

			if (!instantiated && subTree != null && subTree.transform.parent != graph.transform){
				subTree = (BehaviourTree)Instantiate(subTree, transform.position, transform.rotation);
				subTree.transform.parent = graph.transform;
				instantiated = true;	
			}
		}

		////////////////////////////
		//////EDITOR AND GUI////////
		////////////////////////////
		#if UNITY_EDITOR

		protected override void OnNodeGUI(){
		    
		    if (subTree){

		    	GUILayout.Label("'" + subTree.name + "'");

			} else {
				
				if (GUILayout.Button("CREATE NEW"))
					subTree = (BehaviourTree)Graph.CreateNested(this, typeof(BehaviourTree), "SubTree");
			}
		}

		protected override void OnNodeInspectorGUI(){

		    subTree = UnityEditor.EditorGUILayout.ObjectField("Behaviour SubTree", subTree, typeof(BehaviourTree), true) as BehaviourTree;
	    	if (subTree == this.graph){
		    	Debug.LogWarning("You can't have a Graph nested to iteself! Please select another");
		    	subTree = null;
		    }

		    if (subTree != null){
		    	subTree.name = UnityEditor.EditorGUILayout.TextField("Name", subTree.name);
		    	subTree.ShowDefinedBBVariablesGUI();
		    }
		}

		#endif
	}
}