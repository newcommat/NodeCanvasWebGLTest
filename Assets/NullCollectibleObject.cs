using NodeCanvas;
using NodeCanvas.Variables;
using UnityEngine;


namespace scenes.collection
{
	[Category("WEBGL_TEST")]
	public class NullCollectibleObject : ActionTask {
		
		[BlackboardOnly]
		public BBCollectible CollectedItem;

		protected override void OnExecute()
		{
            CollectedItem.value = null;
            EndAction();
		}
	}
}

