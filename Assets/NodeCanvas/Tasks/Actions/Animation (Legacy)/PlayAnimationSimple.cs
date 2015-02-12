using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Actions{

	[Category("Animation")]
	[AgentType(typeof(Animation))]
	public class PlayAnimationSimple : ActionTask{

		[RequiredField]
		public AnimationClip animationClip;
		[SliderField(0,1)]
		public float crossFadeTime = 0.25f;
		public WrapMode animationWrap= WrapMode.Loop;
		public bool waitUntilFinish;

		private Animation anim{
			get {return agent as Animation;}
		}

		//holds the last played animationClip for each agent 
		//definetely not the best way to do it, but its a simple example
		private static Dictionary<Animation, AnimationClip> lastPlayedClips = new Dictionary<Animation, AnimationClip>();

		protected override string info{
			get {return "Anim '" + (animationClip? animationClip.name:"NULL") + "'";}
		}

		protected override string OnInit(){
			anim.AddClip(animationClip, animationClip.name);
			return null;
		}

		protected override void OnExecute(){

			if (lastPlayedClips.ContainsKey(anim) && lastPlayedClips[anim] == animationClip){
				EndAction(true);
				return;
			}

			lastPlayedClips[anim] = animationClip;
			anim[animationClip.name].wrapMode = animationWrap;
			anim.CrossFade(animationClip.name, crossFadeTime);
			
			if (!waitUntilFinish)
				EndAction(true);
		}

		protected override void OnUpdate(){

			if (elapsedTime >= animationClip.length - crossFadeTime)
				EndAction(true);
		}
	}
}