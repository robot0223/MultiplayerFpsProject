namespace Fusion.Addons.AnimationController
{
	using System;
	using System.Collections.Generic;
	using Unity.Profiling;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	public interface IAnimationStateOwner
	{
		AnimationMixerPlayable Mixer { get; }

		bool IsActive(bool self);
		bool IsPlaying(bool self);
		bool IsFadingIn(bool self);
		bool IsFadingOut(bool self);
		void Activate(AnimationState source, float fadeDuration);
		void Deactivate(AnimationState source, float fadeDuration);
	}

	/// <summary>
	/// Animation state component.
	/// </summary>
	public abstract unsafe partial class AnimationState : MonoBehaviour, IAnimationWeightProvider, IAnimationFadingProvider
	{
		// PUBLIC MEMBERS

		/// <summary>
		/// Reference to animation controller.
		/// </summary>
		public AnimationController Controller => _controller;

		/// <summary>
		/// List of animation states (direct children).
		/// </summary>
		public IList<AnimationState> States => _states;

		/// <summary>
		/// Input port used to identify connection to owner (layer/state) mixer.
		/// </summary>
		public int Port => _port;

		/// <summary>
		/// Weight of the state.
		/// </summary>
		public float Weight { get { return _weight; } protected set { _weight = value; } }

		/// <summary>
		/// Fading speed of the state. Represents transition and controls calculation of <c>Weight</c> over time.
		/// </summary>
		public float FadingSpeed { get { return _fadingSpeed; } protected set { _fadingSpeed = value; } }

		/// <summary>
		/// Interpolated weight used in render update to get smooth animations.
		/// </summary>
		public float InterpolatedWeight { get { return _interpolatedWeight; } protected set { _interpolatedWeight = value; } }

		// PROTECTED MEMBERS

		/// <summary>
		/// Reference to state owner (layer/state).
		/// </summary>
		protected IAnimationStateOwner Owner => _owner;

		// PRIVATE MEMBERS

		private AnimationController  _controller;
		private AnimationState[]     _states;
		private IAnimationStateOwner _owner;
		private int                  _port;
		private float                _weight;
		private float                _fadingSpeed;
		private float                _cachedWeight;
		private float                _playableWeight;
		private float                _interpolatedWeight;
		private ProfilerMarker       _profilerMarker;

		// PUBLIC METHODS

		/// <summary>
		/// Explicitly set weight of the state.
		/// <param name="weight">Valid range is 0.0f - 1.0f.</param>
		/// </summary>
		public void SetWeight(float weight)
		{
			_weight = Mathf.Clamp01(weight);
		}

		/// <summary>
		/// Returns <c>true</c> if the state is fading in or if the <c>Weight</c> is greater than zero and the state is not fading out.
		/// There is only one state active at the same time.
		/// <param name="self">If <c>true</c> only this state is considered, otherwise whole owner hierarchy must be active.</param>
		/// </summary>
		public bool IsActive(bool self = false)
		{
			return ((_fadingSpeed == 0.0f && _weight > 0.0f) || _fadingSpeed > 0.0f) && (self == true || _owner.IsActive(false) == true);
		}

		/// <summary>
		/// Returns <c>true</c> if the state is fading in or if the <c>Weight</c> is greater than zero.
		/// There might be more states playing at the same time (while transitioning from one state to another).
		/// <param name="self">If <c>true</c> only this state is considered, otherwise whole owner hierarchy must be playing.</param>
		/// </summary>
		public bool IsPlaying(bool self = false)
		{
			return (_fadingSpeed > 0.0f || _weight > 0.0f) && (self == true || _owner.IsPlaying(false) == true);
		}

		/// <summary>
		/// Returns <c>true</c> if the state is fading in (fading speed greater than zero).
		/// <param name="self">If <c>true</c> only this state is considered, otherwise whole owner hierarchy must be playing and not fading out.</param>
		/// </summary>
		public bool IsFadingIn(bool self = false)
		{
			return _fadingSpeed > 0.0f && (self == true || (_owner.IsPlaying(false) == true && _owner.IsFadingOut(false) == false));
		}

		/// <summary>
		/// Returns <c>true</c> if the state is fading out (fading speed lower than zero).
		/// <param name="self">If <c>true</c> only this state is considered, otherwise whole owner hierarchy must be playing and not fading in.</param>
		/// </summary>
		public bool IsFadingOut(bool self = false)
		{
			return _fadingSpeed < 0.0f && (self == true || (_owner.IsPlaying(false) == true && _owner.IsFadingIn(false) == false));
		}

		/// <summary>
		/// Activate the state.
		/// <param name="fadeDuration">How long it takes to reach full <c>Weight</c> from zero.</param>
		/// <param name="self">If <c>true</c> only this state is activated, otherwise also whole owner hierarchy (except layer) is activated and other states on same level deactivated.</param>
		/// </summary>
		public void Activate(float fadeDuration, bool self = false)
		{
			if ((_fadingSpeed == 0.0f && _weight >= 1.0f) || _fadingSpeed > 0.0f)
				return;

			if (fadeDuration <= 0.0f)
			{
				_weight      = 1.0f;
				_fadingSpeed = 0.0f;
			}
			else
			{
				_fadingSpeed = 1.0f / fadeDuration;
			}

			AnimationProfiler.Log(Controller, $"{nameof(AnimationState)}.{nameof(Activate)} ({name}), Fade Duration: {fadeDuration:F3}, Self: {self}", gameObject);

			OnActivate();

			if (self == false)
			{
				_owner.Activate(this, fadeDuration);
			}
		}

		/// <summary>
		/// Deactivate the state.
		/// <param name="fadeDuration">How long it takes to reach zero <c>Weight</c> from full.</param>
		/// <param name="self">If <c>true</c> only this state is deactivated, otherwise also whole owner hierarchy (except layer) is deactivated.</param>
		/// </summary>
		public void Deactivate(float fadeDuration, bool self = false)
		{
			if ((_fadingSpeed == 0.0f && _weight <= 0.0f) || _fadingSpeed < 0.0f)
				return;

			if (fadeDuration <= 0.0f)
			{
				_weight      = 0.0f;
				_fadingSpeed = 0.0f;
			}
			else
			{
				_fadingSpeed = 1.0f / -fadeDuration;
			}

			AnimationProfiler.Log(Controller, $"{nameof(AnimationState)}.{nameof(Deactivate)} ({name}), Fade Duration: {fadeDuration:F3}, Self: {self}", gameObject);

			OnDeactivate();

			if (self == false)
			{
				_owner.Deactivate(this, fadeDuration);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if the state has an active sub-state. This call is non-recursive.
		/// </summary>
		public bool HasActiveState()
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.IsActive(true) == true)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns <c>true</c> if the state has an active sub-state of type <c>T</c>. This call is non-recursive.
		/// </summary>
		public bool HasActiveState<T>() where T : class
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.IsActive(true) == true && state is T stateAsT)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns active sub-state. This call is non-recursive.
		/// </summary>
		public AnimationState GetActiveState()
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.IsActive(true) == true)
					return state;
			}

			return null;
		}

		/// <summary>
		/// Returns active sub-state of type T. This call is non-recursive.
		/// </summary>
		public bool GetActiveState<T>(out T activeState) where T : class
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.IsActive(true) == true && state is T stateAsT)
				{
					activeState = stateAsT;
					return true;
				}
			}

			activeState = default;
			return false;
		}

		/// <summary>
		/// Deactivate all states within this state.
		/// <param name="fadeDuration">How long it takes to reach zero <c>Weight</c> from full.</param>
		/// <param name="recursive">If true all sub-states will be deactivated as well.</param>
		/// </summary>
		public void DeactivateAllStates(float fadeDuration, bool recursive)
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];

				if (recursive == true)
				{
					state.DeactivateAllStates(fadeDuration, true);
				}

				state.Deactivate(fadeDuration, true);
			}
		}

		/// <summary>
		/// Reset to default state. Calls SetDefaults() on sub-states.
		/// </summary>
		public void SetDefaults()
		{
			_weight      = 0.0f;
			_fadingSpeed = 0.0f;

			for (int i = 0, count = _states.Length; i < count; ++i)
			{
				_states[i].SetDefaults();
			}

			OnSetDefaults();
		}

		/// <summary>
		/// Called when Animation Controller is initialized.
		/// </summary>
		public void Initialize(AnimationController controller, IAnimationStateOwner owner)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			_controller = controller;
			_owner      = owner;

			#if ANIMATIONS_PROFILE
			_profilerMarker = new ProfilerMarker(GetType().Name);
			#endif

			InitializeStates();

			OnInitialize();
		}

		/// <summary>
		/// Called when Animation Controller is deinitialized.
		/// </summary>
		public void Deinitialize()
		{
			OnDeinitialize();

			DeinitializeStates();

			_owner      = default;
			_controller = default;
		}

		/// <summary>
		/// Called when Animation Controller is spawned.
		/// </summary>
		public void Spawned()
		{
			_port               = -1;
			_weight             = 0.0f;
			_fadingSpeed        = 0.0f;
			_cachedWeight       = 0.0f;
			_playableWeight     = 0.0f;
			_interpolatedWeight = 0.0f;

			CreatePlayable();

			if (_port < 0)
				throw new NotSupportedException();

			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Spawned();
			}

			OnSpawned();
		}

		/// <summary>
		/// Called when Animation Controller is despawned.
		/// </summary>
		public void Despawned()
		{
			OnDespawned();

			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state != null)
				{
					state.Despawned();
				}
			}
		}

		/// <summary>
		/// Manual fixed update execution, called internally by Animation Controller.
		/// </summary>
		public void ManualFixedUpdate()
		{
			if (_fadingSpeed <= 0.0f && _weight <= 0.0f)
			{
				SetDefaults();
				return;
			}

			#if ANIMATIONS_PROFILE
			_profilerMarker.Begin();
			#endif

			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.ManualFixedUpdate();
			}

			if (_fadingSpeed != 0.0f)
			{
				_weight += _fadingSpeed * _controller.DeltaTime;

				if (_weight <= 0.0f)
				{
					_weight      = 0.0f;
					_fadingSpeed = 0.0f;
				}
				else if (_weight >= 1.0f)
				{
					_weight      = 1.0f;
					_fadingSpeed = 0.0f;
				}
			}

			OnFixedUpdate();

			#if ANIMATIONS_PROFILE
			_profilerMarker.End();
			#endif
		}

		/// <summary>
		/// Manual interpolation, called internally by Animation Controller.
		/// </summary>
		public void Interpolate()
		{
			if (_interpolatedWeight <= 0.0f)
				return;

			#if ANIMATIONS_PROFILE
			_profilerMarker.Begin();
			#endif

			AnimationState[] states = _states;
			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Interpolate();
			}

			OnInterpolate();

			#if ANIMATIONS_PROFILE
			_profilerMarker.End();
			#endif
		}

		/// <summary>
		/// Returns input weight used in playable graph. Used internally by owner layers and states.
		/// </summary>
		public float GetPlayableInputWeight()
		{
			return _owner.Mixer.GetInputWeight(_port);
		}

		/// <summary>
		/// Explicitly set weight to playable graph. Used to set specific values.
		/// <param name="weight">Playable weight (absolute value).</param>
		/// </summary>
		public void SetPlayableInputWeight(float weight)
		{
			_playableWeight = weight;

			if (weight == _cachedWeight)
				return;

			_cachedWeight = weight;
			_owner.Mixer.SetInputWeight(_port, weight);
		}

		/// <summary>
		/// Applies pre-calculated playable weight to playable graph.
		/// </summary>
		public void SetPlayableInputWeight()
		{
			float weight = _playableWeight;
			if (weight == _cachedWeight)
				return;

			_cachedWeight = weight;
			_owner.Mixer.SetInputWeight(_port, weight);
		}

		/// <summary>
		/// Calculates weight for playable graph. Output requires normalization, final graph input weights can be different than weights stores in layers/states.
		/// <param name="interpolated">If <c>true</c> interpolated properties will be used.</param>
		/// <param name="maxWeight">Maximum children weight, used to calculate maximum playable weight of the parent.</param>
		/// </summary>
		public float CalculatePlayableWeights(bool interpolated, out float maxWeight)
		{
			float stateWeight = interpolated == true ? _interpolatedWeight : _weight;
			if (stateWeight <= 0.0f)
			{
				_playableWeight = 0.0f;
				maxWeight = 0.0f;
				return 0.0f;
			}

			AnimationState[] states     = _states;
			int              stateCount = states.Length;

			if (stateCount == 0)
			{
				_playableWeight = stateWeight;
				maxWeight = stateWeight;
				return stateWeight;
			}

			if (stateCount == 1)
			{
				AnimationState state = states[0];
				_playableWeight = state.CalculatePlayableWeights(interpolated, out float maxChildWeight);
				state.SetPlayableInputWeight(_playableWeight > 0.0f ? 1.0f : 0.0f);
				maxWeight = maxChildWeight;
				return stateWeight;
			}

			maxWeight = 0.0f;

			float childrenWeight = 0.0f;

			for (int i = 0; i < stateCount; ++i)
			{
				childrenWeight += states[i].CalculatePlayableWeights(interpolated, out float maxChildWeight);

				if (maxChildWeight > maxWeight)
				{
					maxWeight = maxChildWeight;
				}
			}

			if (childrenWeight < 0.001f)
			{
				childrenWeight = 0.0f;

				for (int i = 0; i < stateCount; ++i)
				{
					states[i].SetPlayableInputWeight(0.0f);
				}
			}
			else
			{
				float weightMultiplier    = 1.0f / childrenWeight;
				float multipliedWeightSum = 0.0f;

				for (int i = 0; i < stateCount; ++i)
				{
					multipliedWeightSum += states[i].RecalculatePlayableWeight(weightMultiplier);
				}

				if (multipliedWeightSum < 0.001f)
				{
					weightMultiplier    = 0.0f;
					multipliedWeightSum = 0.0f;
				}
				else
				{
					weightMultiplier = 1.0f / multipliedWeightSum;
				}

				for (int i = 0; i < stateCount; ++i)
				{
					AnimationState state = states[i];
					state.RecalculatePlayableWeight(weightMultiplier);
					state.SetPlayableInputWeight();
				}

				if (childrenWeight > 1.0f)
				{
					childrenWeight = 1.0f;
				}
			}

			if (childrenWeight > maxWeight)
			{
				maxWeight = childrenWeight;
			}

			_playableWeight = childrenWeight;

			return stateWeight;
		}

		/// <summary>
		/// Modifies weight for playable graph. Used for normalization.
		/// <param name="multiplier">Pending playable graph input weight is multiplied by this.</param>
		/// </summary>
		public float RecalculatePlayableWeight(float multiplier)
		{
			_playableWeight *= multiplier;
			return _playableWeight;
		}

		/// <summary>
		/// Returns first animation state with the given name.
		/// </summary>
		/// <param name="stateName">Name of the state.</param>
		/// <param name="recursive">Search through children states, using Breadth-First Search.</param>
		public AnimationState FindState(string stateName, bool recursive = true)
		{
			return FindState(stateName, out AnimationState state, recursive) == true ? state : default;
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c>.
		/// </summary>
		/// <param name="recursive">Search through children states, using Breadth-First Search.</param>
		public T FindState<T>(bool recursive = true) where T : class
		{
			return FindState(default, out T state, recursive) == true ? state : default;
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c> with the given name.
		/// </summary>
		/// <param name="stateName">Name of the state.</param>
		/// <param name="recursive">Search through children states, using Breadth-First Search.</param>
		public T FindState<T>(string stateName, bool recursive = true) where T : class
		{
			return FindState(stateName, out T state, recursive) == true ? state : default;
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c>.
		/// </summary>
		/// <param name="state">Reference to the state.</param>
		/// <param name="recursive">Search through children states, using Breadth-First Search.</param>
		public bool FindState<T>(out T state, bool recursive = true) where T : class
		{
			return FindState(default, out state, recursive);
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c> with the given name.
		/// </summary>
		/// <param name="stateName">Name of the state.</param>
		/// <param name="state">Reference to the state.</param>
		/// <param name="recursive">Search through children states, using Breadth-First Search.</param>
		public bool FindState<T>(string stateName, out T state, bool recursive = true) where T : class
		{
			AnimationState[] states = _states;

			for (int i = 0, count = states.Length; i < count; ++i)
			{
				AnimationState stateState = states[i];
				if (stateState is T stateStateAsT && (stateName == default || string.CompareOrdinal(stateState.name, stateName) == 0))
				{
					state = stateStateAsT;
					return true;
				}
			}

			if (recursive == true)
			{
				for (int i = 0, count = states.Length; i < count; ++i)
				{
					AnimationState stateState = states[i];
					if (stateState.FindState(stateName, out T innerState, true) == true)
					{
						state = innerState;
						return true;
					}
				}
			}

			state = default;
			return false;
		}

		public virtual void OnInspectorGUI()
		{
#if UNITY_EDITOR
			//UnityEditor.EditorGUILayout.LabelField("", .ToString("0.00"));
#endif
		}

		// AnimationState INTERFACE

		protected abstract void CreatePlayable();

		protected virtual void OnInitialize()   {}
		protected virtual void OnDeinitialize() {}
		protected virtual void OnSpawned()      {}
		protected virtual void OnDespawned()    {}
		protected virtual void OnFixedUpdate()  {}
		protected virtual void OnInterpolate()  {}
		protected virtual void OnActivate()     {}
		protected virtual void OnDeactivate()   {}
		protected virtual void OnSetDefaults()  {}

		// IAnimationWeightProvider INTERFACE

		float IAnimationWeightProvider.Weight             { get { return _weight;             } set { _weight             = value; } }
		float IAnimationWeightProvider.InterpolatedWeight { get { return _interpolatedWeight; } set { _interpolatedWeight = value; } }

		// IAnimationFadingProvider INTERFACE

		float IAnimationFadingProvider.FadingSpeed { get { return _fadingSpeed; } set { _fadingSpeed = value; } }

		// PROTECTED METHODS

		protected void AddPlayable<T>(T playable, int sourceOutputIndex) where T : struct, IPlayable
		{
			if (_port >= 0)
				throw new NotSupportedException();

			_port = _owner.Mixer.AddInput(playable, sourceOutputIndex, 0.0f);
		}

		// PRIVATE METHODS

		private void InitializeStates()
		{
			if (_states != null)
				return;

			List<AnimationState> activeStates = new List<AnimationState>(8);

			Transform root = transform;
			for (int i = 0, count = root.childCount; i < count; ++i)
			{
				Transform child = root.GetChild(i);

				AnimationState state = child.GetComponentNoAlloc<AnimationState>();
				if (state != null && state.enabled == true && state.gameObject.activeSelf == true)
				{
					activeStates.Add(state);
				}
			}

			_states = activeStates.ToArray();

			IAnimationStateOwner animationStateOwner = this as IAnimationStateOwner;
			if (_states.Length > 0 && animationStateOwner == null)
			{
				throw new NotImplementedException($"State {name}({GetType().FullName}) doesn't implement {nameof(IAnimationStateOwner)}, sub-states are not supported!");
			}

			AnimationState[] states = _states;
			for (int i = 0, count = states != null ? states.Length : 0; i < count; ++i)
			{
				AnimationState state = states[i];
				state.Initialize(_controller, animationStateOwner);
			}
		}

		private void DeinitializeStates()
		{
			AnimationState[] states = _states;
			for (int i = 0, count = states != null ? states.Length : 0; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state != null)
				{
					state.Deinitialize();
				}
			}

			_states = null;
		}
	}
}
