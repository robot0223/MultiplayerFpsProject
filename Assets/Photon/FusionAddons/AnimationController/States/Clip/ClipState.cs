namespace Fusion.Addons.AnimationController
{
	using UnityEngine;
	using UnityEngine.Playables;

	/// <summary>
	/// Animation state that plays single clips.
	/// </summary>
	public class ClipState : AnimationState, IAnimationTimeProvider
	{
		// PUBLIC MEMBERS

		public float AnimationTime             => _animationTime;
		public float InterpolatedAnimationTime => _interpolatedAnimationTime;

		// PROTECTED MEMBERS

		protected ClipNode Node => _node;

		// PRIVATE MEMBERS

		[SerializeField]
		private ClipNode _node;

		private float _animationTime;
		private float _interpolatedAnimationTime;

		// PUBLIC METHODS

		public void SetAnimationTime(float animationTime)
		{
			_animationTime = animationTime;
		}

		public bool IsFinished(float time = 1.0f, bool isNormalized = true)
		{
			if (isNormalized == false)
			{
				if (time < 0.0f)
				{
					time += _node.Length;
				}

				time /= _node.Length;
			}

			if (_animationTime < time)
				return false;
			if (_node.IsLooping == true)
				return false;

			return IsActive();
		}

		// ClipState INTERFACE

		protected virtual void OnClipRestarted() {}
		protected virtual void OnClipFinished()  {}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_node.CreatePlayable(Controller.Graph);
			AddPlayable(_node.PlayableClip, 0);
		}

		protected override void OnDespawned()
		{
			_node.DestroyPlayable();
		}

		protected override void OnFixedUpdate()
		{
			float oldAnimationTime = _animationTime;
			float newAnimationTime = oldAnimationTime + Controller.DeltaTime * _node.Speed / _node.Length;
			bool  clipRestarted    = false;

			if (newAnimationTime >= 1.0f)
			{
				if (_node.IsLooping == true)
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

			_node.PlayableClip.SetTime(newAnimationTime * _node.Length);

			if (clipRestarted == true)
			{
				if (_node.IsLooping == true)
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
			_node.PlayableClip.SetTime(_interpolatedAnimationTime * _node.Length);
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
