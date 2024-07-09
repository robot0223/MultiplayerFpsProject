namespace Fusion.Addons.AnimationController
{
	using System;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	[Serializable]
	public sealed class ClipNode
	{
		// PUBLIC MEMBERS

		public AnimationClip         Clip;
		[NonSerialized]
		public AnimationClipPlayable PlayableClip;
		public float                 Speed = 1.0f;
		public bool                  IsLooping;

		public float                 Length => Clip.length;

		// PUBLIC METHODS

		public void CreatePlayable(PlayableGraph graph)
		{
			PlayableClip = AnimationClipPlayable.Create(graph, Clip);
		}

		public void DestroyPlayable()
		{
			if (PlayableClip.IsValid() == true)
			{
				PlayableClip.Destroy();
			}

			PlayableClip = default;
		}
	}
}
