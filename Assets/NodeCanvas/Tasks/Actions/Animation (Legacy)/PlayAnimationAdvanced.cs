using UnityEngine;
using NodeCanvas.Variables;

namespace NodeCanvas.Actions{

	[Category("Animation")]
	[AgentType(typeof(Animation))]
	public class PlayAnimationAdvanced : ActionTask{

		[RequiredField]
		public AnimationClip animationClip;
		public WrapMode animationWrap;
		public AnimationBlendMode blendMode;
		[SliderField(0,2)]
		public float playbackSpeed = 1;
		[SliderField(0,1)]
		public float crossFadeTime= 0.25f;
		public enum PlayDirections{Forward, Backward, Toggle}
		public PlayDirections playDirection = PlayDirections.Forward;
		public BBString mixTransformName;
		public BBInt animationLayer;
		public bool queueAnimation;
		public bool waitUntilFinish = true;

		private string animationToPlay = string.Empty;
		private int dir = -1;
		private Transform mixTransform;

		private Animation anim{
			get {return agent as Animation;}
		}

		protected override string info{
			get {return "Anim '" + ( animationClip? animationClip.name : "NULL" )  + "'";}
		}

		protected override void OnExecute(){

			if (playDirection == PlayDirections.Toggle)
				dir = -dir;

			if (playDirection == PlayDirections.Backward)
				dir = -1;

			if (playDirection == PlayDirections.Forward)
				dir = 1;
			
			anim.AddClip(animationClip, animationClip.name);
			animationToPlay = animationClip.name;

			if (!string.IsNullOrEmpty(mixTransformName.value)){
				mixTransform = FindTransform(agent.transform, mixTransformName.value);
				if (!mixTransform)
					Debug.LogWarning("Cant find transform with name '" + mixTransformName.value + "' for PlayAnimation Action", gameObject);
			
			} else {
				mixTransform = null;
			}

			animationToPlay = animationClip.name;

			if (mixTransform)
				anim[animationToPlay].AddMixingTransform(mixTransform, true);
			
			anim[animationToPlay].layer = animationLayer.value;
			anim[animationToPlay].speed = dir * playbackSpeed;
			anim[animationToPlay].normalizedTime = Mathf.Clamp01(-dir);
			anim[animationToPlay].wrapMode = animationWrap;
			anim[animationToPlay].blendMode = blendMode;
			
			if (queueAnimation){
				anim.CrossFadeQueued(animationToPlay, crossFadeTime);
			} else {
				anim.CrossFade(animationToPlay, crossFadeTime);
			}

			if (!waitUntilFinish)
				EndAction(true);
		}

		protected override void OnUpdate(){

			if (elapsedTime >= (anim[animationToPlay].length / playbackSpeed) - crossFadeTime)
				EndAction(true);
		}

		Transform FindTransform(Transform parent, string name){

			if (parent.name == name)
				return parent;

			Transform[] transforms= parent.GetComponentsInChildren<Transform>();

			foreach (Transform t in transforms){
				if (t.name == name)
					return t;
			}

			return null;
		}
	}
}