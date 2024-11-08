namespace Fusion.Addons.AnimationController
{
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public abstract class MirrorBlendTreeState : AnimationState, IAnimationTimeProvider
	{
		// PUBLIC MEMBERS

		public float AnimationTime             => _animationTime;
		public float InterpolatedAnimationTime => _interpolatedAnimationTime;

		// PROTECTED MEMBERS

		protected BlendTreeSet[]         Sets  => _sets;
		protected AnimationMixerPlayable Mixer => _mixer;

		// PRIVATE MEMBERS

		[SerializeField]
		private BlendTreeSet[] _sets;
		[SerializeField]
		private bool           _isLooping;
		[SerializeField]
		private MultiBlendTreeState _mirrorState;

		private AnimationMixerPlayable _mixer;
		private float                  _animationTime;
		private float                  _interpolatedAnimationTime;
		private float[]                _cachedWeights;

		// PUBLIC METHODS

		public void SetAnimationTime(float animationTime)
		{
			_animationTime = animationTime;
		}

		public bool IsFinished(float normalizedTime = 1.0f)
		{
			if (_animationTime < normalizedTime)
				return false;
			if (_isLooping == true)
				return false;

			return IsActive();
		}

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_mixer = AnimationMixerPlayable.Create(Controller.Graph, _sets.Length);

			for (int i = 0; i < _sets.Length; ++i)
			{
				BlendTreeSet set = _sets[i];
				set.CreatePlayable(Controller);

				_mixer.ConnectInput(i, set.Mixer, 0);
			}

			AddPlayable(_mixer, 0);
		}

		protected override void OnInitialize()
		{
			_cachedWeights = new float[_sets.Length];
		}

		protected override void OnSpawned()
		{
			for (int i = 0; i < _sets.Length; ++i)
			{
				_cachedWeights[i] = 0.0f;

				BlendTreeSet set = _sets[i];
				set.ResetSpeed();
			}
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}

			for (int i = 0; i < _sets.Length; ++i)
			{
				BlendTreeSet set = _sets[i];
				set.DestroyPlayable();
			}
		}

		protected override void OnFixedUpdate()
		{
			int     setID         = _mirrorState.GetSetID();
			Vector2 blendPosition = _mirrorState.GetBlendPosition(false);
			float[] weights       = _mirrorState.Weights;

			float targetLength = 0.0f;
			float targetWeight = 0.0f;
			float totalWeight  = 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = weights[i];
				if (weight > 0.0f)
				{
					float clipLength = _sets[i].SetPosition(blendPosition);
					if (clipLength > 0.0f)
					{
						targetLength += clipLength * weight;
						targetWeight += weight;
					}

					totalWeight += weight;
				}
			}

			if (targetWeight > 0.0f && targetLength > 0.0f)
			{
				targetLength /= targetWeight;

				float normalizedDeltaTime = Controller.DeltaTime / targetLength;

				_animationTime += normalizedDeltaTime;
				if (AnimationTime > 1.0f)
				{
					if (_isLooping == true)
					{
						_animationTime %= 1.0f;
					}
					else
					{
						_animationTime = 1.0f;
					}
				}
			}

			float weightMultiplier = totalWeight > 0.0f ? 1.0f / totalWeight : 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = weights[i] * weightMultiplier;
				if (weight > 0.0f)
				{
					BlendTreeSet set = _sets[i];
					set.SetTime(AnimationTime);
				}

				if (weight != _cachedWeights[i])
				{
					_cachedWeights[i] = weight;
					_mixer.SetInputWeight(i, weight);
				}
			}
		}

		protected override void OnInterpolate()
		{
			Vector2 blendPosition = _mirrorState.GetBlendPosition(true);
			float[] weights       = _mirrorState.InterpolatedWeights;

			float totalWeight = 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				totalWeight += weights[i];
			}

			float weightMultiplier = totalWeight > 0.0f ? 1.0f / totalWeight : 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = weights[i] * weightMultiplier;
				if (weight > 0.0f)
				{
					BlendTreeSet set = _sets[i];
					set.SetPosition(blendPosition);
					set.SetTime(InterpolatedAnimationTime);
				}

				if (weight != _cachedWeights[i])
				{
					_cachedWeights[i] = weight;
					_mixer.SetInputWeight(i, weight);
				}
			}
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
