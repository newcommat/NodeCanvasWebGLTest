using UnityEngine;

namespace NodeCanvas.BehaviourTrees{

	[AddComponentMenu("")]
	abstract public class BTComposite : BTNodeBase {

		public override int maxOutConnections{
			get {return -1;}
		}

		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnContextMenu(UnityEditor.GenericMenu menu){

			//menu.AddItem (new GUIContent ("Convert to SubTree"), false, ContextMakeNested);
            if (outConnections.Count > 0){
				menu.AddItem (new GUIContent ("Delete Branch"), false, delegate{ DeleteBranch(); } );
				menu.AddItem(new GUIContent("Duplicate Branch"), false, delegate { DuplicateHierarchy(); });
			}
		}		

		private void DeleteBranch(){
			foreach(Node node in GetAllChildNodesRecursively(true))
				graph.RemoveNode(node);
		}

/*		//TODO: RE-Implement.Warning: This doesnt work!! Do not enable
		private void ContextMakeNested(){

			if (!UnityEditor.EditorUtility.DisplayDialog("Convert to SubTree", "This will create a new SubTree out of this branch.\nThe SubTree can NOT be unpacked later on.\nAre you sure?", "Yes", "No!"))
				return;

			UnityEditor.Undo.RecordObject(graph, "Make Nested");
			var newNestedNode = (BTSubTree)graph.AddNode(typeof(BTSubTree));
			var newBT = (BehaviourTree)Graph.CreateNested(newNestedNode, typeof(BehaviourTree), "Nested BT");

			newNestedNode.nodeRect.center = this.nodeRect.center;
			
			if (this.graph.primeNode == this)
				this.graph.primeNode = newNestedNode;

			UnityEditor.Undo.RecordObject(this, "Make Nested");

			foreach (Connection connection in inConnections.ToArray())
				connection.RelinkTarget(newNestedNode);

			this.inConnections.Clear();

			DuplicateHierarchy(newBT);
			UnityEditor.Undo.RecordObject(graph, "Make Nested");
			DeleteBranch();
		}
*/

		#endif
	}
}