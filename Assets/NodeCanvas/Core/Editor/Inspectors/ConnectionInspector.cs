using UnityEngine;
using UnityEditor;
using NodeCanvas;

namespace NodeCanvasEditor{
	//On purpose hide inspection
	[CustomEditor(typeof(Connection), true)]
	public class ConnectionInspector : Editor {
		public override void OnInspectorGUI(){}
	}
}
