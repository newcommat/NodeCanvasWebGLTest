using NodeCanvas;
using NodeCanvas.Variables;
using UnityEngine;


namespace scenes.collection
{
	[Category("WEBGL_TEST")]
	public class NullGameObject : ActionTask {
		
		[BlackboardOnly]
		public BBObject unityObject;

		protected override void OnExecute()
		{
            unityObject.value = null;
            EndAction();
		}
	}
}

