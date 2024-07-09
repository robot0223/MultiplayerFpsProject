namespace Fusion.Addons.AnimationController
{
	using System;
	using System.Collections.Generic;
	using Unity.Profiling;
	using UnityEngine;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	#pragma warning disable 0109

	/// <summary>
	/// Animation controller component.
	/// </summary>
	[DisallowMultipleComponent]
	public partial class AnimationController : NetworkBehaviour, IAfterSpawned, IAfterClientPredictionReset, IAfterTick
	{
		// CONSTANTS

		public const string LOGGING_SCRIPT_DEFINE   = "ANIMATIONS_LOG";
		public const string PROFILING_SCRIPT_DEFINE = "ANIMATIONS_PROFILE";

		// PUBLIC MEMBERS

		/// <summary>
		/// Evaluated playable graph.
		/// </summary>
		public PlayableGraph Graph => _graph;

		/// <summary>
		/// Main playable mixer which mixes outputs from animation layers.
		/// </summary>
		public AnimationLayerMixerPlayable Mixer => _mixer;

		/// <summary>
		/// List of animation layers.
		/// </summary>
		public IList<AnimationLayer> Layers => _layers;

		/// <summary>
		/// Current animator.
		/// </summary>
		public Animator Animator => _animator;

		/// <summary>
		/// Controls whether update methods are driven by default Fusion methods or called manually using <c>ManualFixedUpdate()</c> and <c>ManualRenderUpdate()</c>.
		/// </summary>
		public bool HasManualUpdate => _hasManualUpdate;

		/// <summary>
		/// Returns <c>Runner.DeltaTime</c> in fixed update and <c>Time.deltaTime</c> in render update.
		/// </summary>
		public float DeltaTime => _deltaTime;

		/// <summary>
		/// <c>True</c> if the animation prediction is enabled in fixed update.
		/// </summary>
		[System.Obsolete("Use Object.IsInSimulation.")]
		public bool IsPredictingInFixedUpdate => Object.IsInSimulation;

		/// <summary>
		/// <c>True</c> if the animation interpolation is enabled in fixed update.
		/// </summary>
		[System.Obsolete("Interpolation in fixed update has been removed.")]
		public bool IsInterpolatingInFixedUpdate => false;

		// PRIVATE MEMBERS

		[SerializeField]
		private Transform                   _root;
		[SerializeField]
		private Animator                    _animator;

		private PlayableGraph               _graph;
		private AnimationLayerMixerPlayable _mixer;
		private AnimationPlayableOutput     _output;
		private AnimationLayer[]            _layers;
		private bool                        _isSpawned;
		private bool                        _hasManualUpdate;
		private bool                        _evaluateOnResimulation;
		private EvaluationSettings          _fixedEvaluationSettings  = new EvaluationSettings();
		private EvaluationSettings          _renderEvaluationSettings = new EvaluationSettings();
		private float                       _deltaTime;

		private static ProfilerMarker _fixedUpdateMarker   = new ProfilerMarker("AnimationController.FixedUpdate");
		private static ProfilerMarker _renderUpdateMarker  = new ProfilerMarker("AnimationController.RenderUpdate");
		private static ProfilerMarker _restoreStateMarker  = new ProfilerMarker("AnimationController.RestoreState");
		private static ProfilerMarker _afterTickMarker     = new ProfilerMarker("AnimationController.AfterTick");
		private static ProfilerMarker _interpolateMarker   = new ProfilerMarker("AnimationController.Interpolate");
		private static ProfilerMarker _evaluateMarker      = new ProfilerMarker("AnimationController.Evaluate");
		private static ProfilerMarker _evaluateGraphMarker = new ProfilerMarker("PlayableGraph.Evaluate");

		// PUBLIC METHODS

		/// <summary>
		/// Returns current evaluation mode for given target.
		/// <param name="target">Evaluation target.</param>
		/// </summary>
		public EEvaluationMode GetEvaluationMode(EEvaluationTarget target)
		{
			switch (target)
			{
				case EEvaluationTarget.None:         { return EEvaluationMode.None;           }
				case EEvaluationTarget.FixedUpdate:  { return _fixedEvaluationSettings.Mode;  }
				case EEvaluationTarget.RenderUpdate: { return _renderEvaluationSettings.Mode; }
				default:
				{
					throw new NotImplementedException($"{target}");
				}
			}
		}

		/// <summary>
		/// <para>Set evaluation mode for given target.</para>
		/// <para>Settings <c>EEvaluationMode.Interlaced</c> requires additional parameters and <c>SetInterlacedEvaluation()</c> must be called.</para>
		/// <para>Settings <c>EEvaluationMode.Periodic</c> requires additional parameters and <c>SetPeriodicEvaluation()</c> must be called.</para>
		/// </summary>
		public void SetEvaluationMode(EEvaluationTarget target, EEvaluationMode mode)
		{
			if (target == EEvaluationTarget.None)
				return;
			if (mode == EEvaluationMode.Interlaced)
				throw new NotSupportedException($"Parameters required, please use {nameof(AnimationController)}.{nameof(SetInterlacedEvaluation)}()");
			if (mode == EEvaluationMode.Periodic)
				throw new NotSupportedException($"Parameters required, please use {nameof(AnimationController)}.{nameof(SetPeriodicEvaluation)}()");

			if (target == EEvaluationTarget.FixedUpdate)
			{
				_fixedEvaluationSettings.Mode = mode;
			}
			else if (target == EEvaluationTarget.RenderUpdate)
			{
				_renderEvaluationSettings.Mode = mode;
			}
			else
			{
				throw new NotImplementedException($"{target}");
			}
		}

		/// <summary>
		/// Set interlaced evaluation for given target.
		/// <param name="target">Evaluation target.</param>
		/// <param name="frames">Evaluation runs once per [frames] fixed/render updates.</param>
		/// <param name="seed">Initial frame offset to spread evaluation over multiple ticks/frames.</param>
		/// </summary>
		public void SetInterlacedEvaluation(EEvaluationTarget target, int frames, int seed)
		{
			if (frames < 0)
				throw new ArgumentException(nameof(frames));

			if (target == EEvaluationTarget.FixedUpdate)
			{
				_fixedEvaluationSettings.Mode   = EEvaluationMode.Interlaced;
				_fixedEvaluationSettings.Frames = frames;
				_fixedEvaluationSettings.Seed   = seed % frames;
			}
			else if (target == EEvaluationTarget.RenderUpdate)
			{
				_renderEvaluationSettings.Mode   = EEvaluationMode.Interlaced;
				_renderEvaluationSettings.Frames = frames;
				_renderEvaluationSettings.Seed   = seed % frames;
			}
			else
			{
				throw new NotImplementedException($"{target}");
			}
		}

		/// <summary>
		/// Set periodic evaluation for given target.
		/// <param name="target">Evaluation target.</param>
		/// <param name="period">Evaluation runs once per [period] seconds.</param>
		/// <param name="setInitialOffset">If true, the current offset will be reset to [initialOffset].</param>
		/// <param name="initialOffset">Initial time offset to start with.</param>
		/// </summary>
		public void SetPeriodicEvaluation(EEvaluationTarget target, float period, bool setInitialOffset = false, float initialOffset = 0.0f)
		{
			if (period <= 0.0f)
				throw new ArgumentException(nameof(period));
			if (setInitialOffset == true && initialOffset < 0.0f)
				throw new ArgumentException(nameof(initialOffset));

			if (target == EEvaluationTarget.FixedUpdate)
			{
				_fixedEvaluationSettings.Mode   = EEvaluationMode.Periodic;
				_fixedEvaluationSettings.Period = period;

				if (setInitialOffset == true)
				{
					_fixedEvaluationSettings.Offset = initialOffset;
				}
			}
			else if (target == EEvaluationTarget.RenderUpdate)
			{
				_renderEvaluationSettings.Mode   = EEvaluationMode.Periodic;
				_renderEvaluationSettings.Period = period;

				if (setInitialOffset == true)
				{
					_renderEvaluationSettings.Offset = initialOffset;
				}
			}
			else
			{
				throw new NotImplementedException($"{target}");
			}
		}

		/// <summary>
		/// Enable/disable evaluation on resimulation. This is important if evaluated bones have direct impact on following logic.
		/// <param name="evaluateOnResimulation">If true, evaluation on resimulation will be enabled.</param>
		/// </summary>
		public void SetEvaluateOnResimulation(bool evaluateOnResimulation)
		{
			_evaluateOnResimulation = evaluateOnResimulation;
		}

		/// <summary>
		/// Set Animator target for evaluation.
		/// </summary>
		public void SetAnimator(Animator animator)
		{
			_animator = animator;

			if (_graph.IsValid() == false)
				return;

			if (_output.IsOutputValid() == true)
			{
				_graph.DestroyOutput(_output);
				_output = default;
			}

			if (_animator != null)
			{
				_output = AnimationPlayableOutput.Create(_graph, name, _animator);
				_output.SetSourcePlayable(_mixer);
			}
		}

		/// <summary>
		/// Controls whether update methods are driven by default Fusion methods or called manually using <c>ManualFixedUpdate()</c> and <c>ManualRenderUpdate()</c>.
		/// </summary>
		public void SetManualUpdate(bool hasManualUpdate)
		{
			_hasManualUpdate = hasManualUpdate;
		}

		/// <summary>
		/// Manual fixed update execution, <c>SetManualUpdate(true)</c> must be called prior usage.
		/// </summary>
		public void ManualFixedUpdate()
		{
			if (_isSpawned == false)
				return;
			if (_hasManualUpdate == false)
				throw new InvalidOperationException("Manual update is not set!");

			_fixedUpdateMarker.Begin();
			OnFixedUpdateInternal();
			_fixedUpdateMarker.End();
		}

		/// <summary>
		/// Manual render update execution, <c>SetManualUpdate(true)</c> must be called prior usage.
		/// </summary>
		public void ManualRenderUpdate()
		{
			if (_isSpawned == false)
				return;
			if (_hasManualUpdate == false)
				throw new InvalidOperationException("Manual update is not set!");

			_renderUpdateMarker.Begin();
			OnRenderUpdateInternal();
			_renderUpdateMarker.End();
		}

		/// <summary>
		/// Returns first animation layer with the given name.
		/// </summary>
		/// <param name="layerName">Name of the layer.</param>
		public AnimationLayer FindLayer(string layerName)
		{
			return FindLayer<AnimationLayer>(layerName);
		}

		/// <summary>
		/// Returns first animation layer of type <c>T</c>.
		/// </summary>
		public T FindLayer<T>() where T : class
		{
			return FindLayer(default, out T layer) == true ? layer : default;
		}

		/// <summary>
		/// Returns first animation layer of type <c>T</c> with the given name.
		/// </summary>
		/// <param name="layerName">Name of the layer.</param>
		public T FindLayer<T>(string layerName) where T : class
		{
			return FindLayer(layerName, out T layer) == true ? layer : default;
		}

		/// <summary>
		/// Returns first animation layer of type <c>T</c>.
		/// </summary>
		/// <param name="layer">Reference to the layer.</param>
		public bool FindLayer<T>(out T layer) where T : class
		{
			return FindLayer(default, out layer);
		}

		/// <summary>
		/// Returns first animation layer of type <c>T</c> with the given name.
		/// </summary>
		/// <param name="layerName">Name of the layer.</param>
		/// <param name="layer">Reference to the layer.</param>
		public bool FindLayer<T>(string layerName, out T layer) where T : class
		{
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer animationLayer = layers[i];
				if (animationLayer is T layerAsT && (layerName == default || string.CompareOrdinal(animationLayer.name, layerName) == 0))
				{
					layer = layerAsT;
					return true;
				}
			}

			layer = default;
			return false;
		}

		/// <summary>
		/// Returns first animation state with the given name, using Breadth-First Search.
		/// </summary>
		/// <param name="stateName">Name of the state.</param>
		public AnimationState FindState(string stateName)
		{
			return FindState(stateName, out AnimationState state) == true ? state : default;
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c>, using Breadth-First Search.
		/// </summary>
		public T FindState<T>() where T : class
		{
			return FindState(default, out T state) == true ? state : default;
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c> with the given name, using Breadth-First Search.
		/// </summary>
		/// <param name="stateName">Name of the state.</param>
		public T FindState<T>(string stateName) where T : class
		{
			return FindState(stateName, out T state) == true ? state : default;
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c>, using Breadth-First Search.
		/// </summary>
		/// <param name="state">Reference to the state.</param>
		public bool FindState<T>(out T state) where T : class
		{
			return FindState(default, out state);
		}

		/// <summary>
		/// Returns first animation state of type <c>T</c> with the given name, using Breadth-First Search.
		/// </summary>
		/// <param name="stateName">Name of the state.</param>
		/// <param name="state">Reference to the state.</param>
		public bool FindState<T>(string stateName, out T state) where T : class
		{
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				if (layer.FindState(stateName, out state, true) == true)
					return true;
			}

			state = default;
			return false;
		}

		/// <summary>
		/// Logs info message into console with frame/tick metadata.
		/// </summary>
		/// <param name="messages">Custom message objects.</param>
		public void Log(params object[] messages)
		{
			AnimationUtility.Log(this, default, EAnimationLogType.Info, messages);
		}

		/// <summary>
		/// Logs warning message into console with frame/tick metadata.
		/// </summary>
		/// <param name="messages">Custom message objects.</param>
		public void LogWarning(params object[] messages)
		{
			AnimationUtility.Log(this, default, EAnimationLogType.Warning, messages);
		}

		/// <summary>
		/// Logs error message into console with frame/tick metadata.
		/// </summary>
		/// <param name="messages">Custom message objects.</param>
		public void LogError(params object[] messages)
		{
			AnimationUtility.Log(this, default, EAnimationLogType.Error, messages);
		}

		// AnimationController INTERFACE

		protected virtual void OnInitialize()   {}
		protected virtual void OnDeinitialize() {}
		protected virtual void OnSpawned()      {}
		protected virtual void OnDespawned()    {}
		protected virtual void OnFixedUpdate()  {}
		protected virtual void OnInterpolate()  {}
		protected virtual void OnEvaluate()     {}

		// NetworkBehaviour INTERFACE

		public override sealed int? DynamicWordCount => GetNetworkDataWordCount();

		public override sealed void Spawned()
		{
			_isSpawned = true;
			_deltaTime = Runner.DeltaTime;

			_graph = PlayableGraph.Create(name);
			_graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

			_mixer = AnimationLayerMixerPlayable.Create(_graph);

			_output = AnimationPlayableOutput.Create(_graph, name, _animator);
			_output.SetSourcePlayable(_mixer);

			if (Object.HasStateAuthority == false)
			{
				ReadNetworkData();
			}

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Spawned();
			}

			OnSpawned();
		}

		public override sealed void Despawned(NetworkRunner runner, bool hasState)
		{
			_isSpawned = false;

			OnDespawned();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers != null ? layers.Length : 0; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				if (layer != null)
				{
					layer.Despawned();
				}
			}

			if (_graph.IsValid() == true)
			{
				_graph.Destroy();
			}

			_hasManualUpdate        = default;
			_evaluateOnResimulation = default;

			_fixedEvaluationSettings.Reset();
			_renderEvaluationSettings.Reset();
		}

		public override sealed void FixedUpdateNetwork()
		{
			if (_hasManualUpdate == true)
				return;

			_fixedUpdateMarker.Begin();
			OnFixedUpdateInternal();
			_fixedUpdateMarker.End();
		}

		public override sealed void Render()
		{
			if (_hasManualUpdate == true)
				return;

			_renderUpdateMarker.Begin();
			OnRenderUpdateInternal();
			_renderUpdateMarker.End();
		}

		// IAfterSpawned INTERFACE

		void IAfterSpawned.AfterSpawned()
		{
			if (Object.IsInSimulation == true)
			{
				WriteNetworkData();
			}
		}

		// IAfterClientPredictionReset INTERFACE

		void IAfterClientPredictionReset.AfterClientPredictionReset()
		{
			_restoreStateMarker.Begin();
			ReadNetworkData();
			_restoreStateMarker.End();
		}

		// IAfterTick INTERFACE

		void IAfterTick.AfterTick()
		{
			_afterTickMarker.Begin();
			WriteNetworkData();
			_afterTickMarker.End();
		}

		// MonoBehaviour INTERFACE

		protected virtual void Awake()
		{
			InitializeLayers();
			InitializeNetworkProperties();

			OnInitialize();
		}

		protected virtual void OnDestroy()
		{
			if (_isSpawned == true)
			{
				Despawned(null, false);
			}

			OnDeinitialize();

			DeinitializeNetworkProperties();
			DeinitializeLayers();
		}

		// PRIVATE METHODS

		private void InitializeLayers()
		{
			if (_layers != null)
				return;

			List<AnimationLayer> activeLayers = new List<AnimationLayer>(8);

			Transform root = _root;
			for (int i = 0, count = root.childCount; i < count; ++i)
			{
				Transform child = root.GetChild(i);

				AnimationLayer layer = child.GetComponentNoAlloc<AnimationLayer>();
				if (layer != null && layer.enabled == true && layer.gameObject.activeSelf == true)
				{
					activeLayers.Add(layer);
				}
			}

			_layers = activeLayers.ToArray();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Initialize(this);
			}
		}

		private void DeinitializeLayers()
		{
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers != null ? layers.Length : 0; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				if (layer != null)
				{
					layer.Deinitialize();
				}
			}

			_layers = null;
		}

		private void OnFixedUpdateInternal()
		{
			if (Runner.Stage == default)
				throw new InvalidOperationException();

			_deltaTime = Runner.DeltaTime;

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.ManualFixedUpdate();
			}

			OnFixedUpdate();

			if (_fixedEvaluationSettings.Mode != EEvaluationMode.None && (_evaluateOnResimulation == true || Runner.IsResimulation == false))
			{
				bool evaluate = true;

				if (_fixedEvaluationSettings.Mode == EEvaluationMode.Interlaced && _fixedEvaluationSettings.Frames > 1)
				{
					int targetSeed = Runner.Tick.Raw % _fixedEvaluationSettings.Frames;
					if (_fixedEvaluationSettings.Seed != targetSeed)
					{
						evaluate = false;
					}
				}
				else if (_fixedEvaluationSettings.Mode == EEvaluationMode.Periodic)
				{
					float deltaTime       = Runner.DeltaTime;
					float simulationTime  = Runner.SimulationTime + _fixedEvaluationSettings.Offset;
					float timeSincePeriod = simulationTime % _fixedEvaluationSettings.Period;

					if (timeSincePeriod > deltaTime)
					{
						evaluate = false;
					}
				}

				if (evaluate == true)
				{
					EvaluateInternal(false);
				}
			}
		}

		private void OnRenderUpdateInternal()
		{
			if (Runner.Stage != default)
				throw new InvalidOperationException();

			_deltaTime = Time.deltaTime;

			if (_renderEvaluationSettings.Mode != EEvaluationMode.None)
			{
				bool evaluate = true;

				if (_renderEvaluationSettings.Mode == EEvaluationMode.Interlaced && _renderEvaluationSettings.Frames > 1)
				{
					int targetSeed = Time.frameCount % _renderEvaluationSettings.Frames;
					if (_renderEvaluationSettings.Seed != targetSeed)
					{
						evaluate = false;
					}
				}
				else if (_renderEvaluationSettings.Mode == EEvaluationMode.Periodic)
				{
					_renderEvaluationSettings.Offset += Time.deltaTime;
					if (_renderEvaluationSettings.Offset > _renderEvaluationSettings.Period)
					{
						_renderEvaluationSettings.Offset %= _renderEvaluationSettings.Period;
					}
					else
					{
						evaluate = false;
					}
				}

				if (evaluate == true)
				{
					InterpolateInternal();
					EvaluateInternal(true);
				}
			}
		}

		private void InterpolateInternal()
		{
			_interpolateMarker.Begin();
			InterpolateNetworkData();

			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.Interpolate();
			}

			OnInterpolate();
			_interpolateMarker.End();
		}

		private void EvaluateInternal(bool interpolated)
		{
			_evaluateMarker.Begin();
			AnimationLayer[] layers = _layers;
			for (int i = 0, count = layers.Length; i < count; ++i)
			{
				AnimationLayer layer = layers[i];
				layer.SetPlayableInputWeights(interpolated);
			}

			_evaluateGraphMarker.Begin();
			_graph.Evaluate();
			_evaluateGraphMarker.End();

			OnEvaluate();
			_evaluateMarker.End();
		}

		private sealed class EvaluationSettings
		{
			public EEvaluationMode Mode = EEvaluationMode.Full;
			public int             Frames;
			public int             Seed;
			public float           Period;
			public float           Offset;

			public void Reset()
			{
				Mode   = EEvaluationMode.Full;
				Frames = default;
				Seed   = default;
				Period = default;
				Offset = default;
			}
		}
	}
}
