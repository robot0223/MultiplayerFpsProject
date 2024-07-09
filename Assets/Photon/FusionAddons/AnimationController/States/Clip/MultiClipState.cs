namespace Fusion.Addons.AnimationController
{
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	/// <summary>
	/// Animation state that plays multiple clips. Only one clip can be played at any given moment.
	/// This is useful if you have clips with similar meaning, for example shooting with pistol/rifle/...
	/// </summary>
	public abstract partial class MultiClipState : AnimationState, IAnimationTimeProvider
	{
		// PUBLIC MEMBERS

		public float AnimationTime             => _animationTime;
		public float InterpolatedAnimationTime => _interpolatedAnimationTime;

		// PROTECTED MEMBERS

		protected ClipNode[]             Nodes => _nodes;
		protected AnimationMixerPlayable Mixer => _mixer;

		// PRIVATE MEMBERS

		[SerializeField]
		private ClipNode[] _nodes;

		private AnimationMixerPlayable _mixer;
		private float                  _animationTime;
		private float                  _interpolatedAnimationTime;

		// PUBLIC METHODS

		public void SetAnimationTime(float animationTime)
		{
			_animationTime = animationTime;
		}

		public bool IsFinished(float normalizedTime = 1.0f)
		{
			if (_animationTime < normalizedTime)
				return false;

			return IsActive();
		}

		// MultiClipState INTERFACE

		protected abstract int GetClipID();

		protected virtual void OnClipRestarted() {}
		protected virtual void OnClipFinished()  {}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_mixer = AnimationMixerPlayable.Create(Controller.Graph, _nodes.Length);

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				ClipNode node = _nodes[i];

				node.CreatePlayable(Controller.Graph);

				_mixer.ConnectInput(i, node.PlayableClip, 0);
			}

			AddPlayable(_mixer, 0);
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_nodes[i].DestroyPlayable();
			}
		}

		protected override void OnFixedUpdate()
		{
			int clipID = GetClipID();

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			_mixer.SetInputWeight(clipID, 1.0f);

			ClipNode node = _nodes[clipID];

			float oldAnimationTime = _animationTime;
			float newAnimationTime = oldAnimationTime + Controller.DeltaTime * node.Speed / node.Length;
			bool  clipRestarted    = false;

			if (newAnimationTime >= 1.0f)
			{
				if (node.IsLooping == true)
				{
					newAnimationTime %= 1.0f;
				}
				else
				{
					newAnimationTime = 1.0f;
				}

				if (oldAnimationTime < 1.0f)
				{
					clipRestarted = true;
				}
			}

			_animationTime = newAnimationTime;

			node.PlayableClip.SetTime(newAnimationTime * node.Length);

			if (clipRestarted == true)
			{
				if (node.IsLooping == true)
				{
					OnClipRestarted();
				}
				else
				{
					OnClipFinished();
				}
			}
		}

		protected override void OnInterpolate()
		{
			int clipID = GetClipID();

			for (int i = 0, count = _nodes.Length; i < count; ++i)
			{
				_mixer.SetInputWeight(i, 0.0f);
			}

			_mixer.SetInputWeight(clipID, 1.0f);

			ClipNode node = _nodes[clipID];
			node.PlayableClip.SetTime(_interpolatedAnimationTime * node.Length);
		}

		protected override void OnSetDefaults()
		{
			_animationTime = 0.0f;
		}

		// IAnimationTimeProvider INTERFACE

		float IAnimationTimeProvider.AnimationTime             { get { return _animationTime;             } set { _animationTime             = value; } }
		float IAnimationTimeProvider.InterpolatedAnimationTime { get { return _interpolatedAnimationTime; } set { _interpolatedAnimationTime = value; } }
	}
}
