namespace Fusion.Addons.AnimationController
{
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public abstract class MultiBlendTreeState : AnimationState, IAnimationTimeProvider, IAnimationWeightsProvider
	{
		// PUBLIC MEMBERS

		public float   AnimationTime             => _animationTime;
		public float   InterpolatedAnimationTime => _interpolatedAnimationTime;
		public float[] Weights                   => _weights;
		public float[] InterpolatedWeights       => _interpolatedWeights;

		// PROTECTED MEMBERS

		protected BlendTreeSet[]         Sets  => _sets;
		protected AnimationMixerPlayable Mixer => _mixer;

		// PRIVATE MEMBERS

		[SerializeField]
		private BlendTreeSet[] _sets;
		[SerializeField]
		private bool           _isLooping;
		[SerializeField]
		private float          _blendTime;

		private AnimationMixerPlayable _mixer;
		private float                  _animationTime;
		private float                  _interpolatedAnimationTime;
		private float[]                _weights;
		private float[]                _cachedWeights;
		private float[]                _interpolatedWeights;

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

		// BlendTreeState INTERFACE

		public abstract int     GetSetID();
		public abstract float   GetSpeedMultiplier();
		public abstract Vector2 GetBlendPosition(bool interpolated);

		// AnimationState INTERFACE

		protected override void OnInitialize()
		{
			_weights             = new float[_sets.Length];
			_cachedWeights       = new float[_sets.Length];
			_interpolatedWeights = new float[_sets.Length];
		}

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

			for (int i = 0, count = _sets.Length; i < count; ++i)
			{
				_sets[i].DestroyPlayable();
			}
		}

		protected override void OnFixedUpdate()
		{
			int     setID         = GetSetID();
			Vector2 blendPosition = GetBlendPosition(false);

			float targetLength = 0.0f;
			float targetWeight = 0.0f;
			float totalWeight  = 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = _weights[i];

				if (setID == i)
				{
					weight = _blendTime > 0.0f ? Mathf.Min(weight + Controller.DeltaTime / _blendTime, 1.0f) : 1.0f;
				}
				else
				{
					weight = _blendTime > 0.0f ? Mathf.Max(0.0f, weight - Controller.DeltaTime / _blendTime) : 0.0f;
				}

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

				_weights[i] = weight;
			}

			if (targetWeight > 0.0f && targetLength > 0.0f)
			{
				targetLength /= targetWeight;

				float speedMultiplier     = GetSpeedMultiplier();
				float normalizedDeltaTime = Controller.DeltaTime * speedMultiplier / targetLength;

				_animationTime += normalizedDeltaTime;
				if (_animationTime > 1.0f)
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
				float weight = _weights[i] * weightMultiplier;
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
			Vector2 blendPosition = GetBlendPosition(true);

			float totalWeight = 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				totalWeight += _interpolatedWeights[i];
			}

			float weightMultiplier = totalWeight > 0.0f ? 1.0f / totalWeight : 0.0f;

			for (int i = 0; i < _sets.Length; ++i)
			{
				float weight = _interpolatedWeights[i] * weightMultiplier;
				if (weight > 0.0f)
				{
					BlendTreeSet set = _sets[i];
					set.SetPosition(blendPosition);
					set.SetTime(_interpolatedAnimationTime);
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

		// IAnimationWeightsProvider INTERFACE

		float[] IAnimationWeightsProvider.Weights             { get { return _weights;             } }
		float[] IAnimationWeightsProvider.InterpolatedWeights { get { return _interpolatedWeights; } }
	}
}
