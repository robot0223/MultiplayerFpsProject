namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Defines processing behavior of interest provider.
    /// <list type="bullet">
    /// <item><description>Default - Interest from this provider is processed additively to existing interest.</description></item>
    /// <item><description>Override - Interest from this provider overrides all existing interest.</description></item>
    /// <item><description>Exclude - Interest from this provider is removed from existing interest.</description></item>
    /// </list>
	/// </summary>
	public enum EInterestMode
	{
		Default  = 0,
		Override = 1,
		Exclude  = 2,
	}

	/// <summary>
	/// Base class for interest providers.
	/// Can be used as is and setup interest by linking interest shapes.
	/// </summary>
	[DefaultExecutionOrder(InterestProvider.EXECUTION_ORDER)]
	public class InterestProvider : NetworkBehaviour, IInterestProvider
	{
		// CONSTANTS

		public const int EXECUTION_ORDER = 20000;

		// PUBLIC MEMBERS

		[InlineHelp][Tooltip("This provider will be registered to global interest manager and automatically processed for every player.")]
		public bool IsGlobal;
		[InlineHelp][Tooltip("Defines order in which interest providers are processed. Provider with lower value will be processed first.")]
		public int SortOrder;
		[InlineHelp][Tooltip("Defines processing behavior.\n" +
		"• Default - Interest from this provider is processed additively to existing interest.\n" +
		"• Override - Interest from this provider overrides all existing interest.\n" +
		"• Exclude - Interest from this provider is removed from existing interest.")]
		public EInterestMode InterestMode;
		[InlineHelp][Tooltip("List of interest shapes that are processed to player interest.")]
		public List<InterestShape> Shapes = new List<InterestShape>();
		[InlineHelp][Tooltip("List of child interest providers which are registered to this provider upon spawn.")]
		public List<InterestProvider> ChildProviders = new List<InterestProvider>();

		/// <summary>
		/// Cached Transform component.
		/// </summary>
		public Transform Transform { get { if (ReferenceEquals(_transform, null) == true) { _transform = transform; } return transform; } }

		public static Color ChildProvidersConnectionColor      = new Color(0.0f, 0.0f, 1.0f, 1.0f);
		public static Color RegisteredProvidersConnectionColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

		// PRIVATE MEMBERS

		private bool                  _isGlobal;
		private int                   _sortOrder;
		private EInterestMode         _interestMode;
		private List<RuntimeProvider> _runtimeProviders = new List<RuntimeProvider>();
		private bool                  _isRegistered;
		private Transform             _transform;
		private int                   _version;

		private static Stack<RuntimeProvider> _runtimeProvidersPool = new Stack<RuntimeProvider>();

		// PUBLIC METHODS

		/// <summary>
		/// Mark all active registrations (including global) of this provider as invalid.
		/// Use this method to prevent further processing of this provider.
		/// Called automatically on despawn.
		/// </summary>
		public void Release()
		{
			++_version;
		}

		/// <summary>
		/// Register other interest provider to self, allowing to create hierarchy of interest providers.
		/// Registered providers are processed only if the player is interested in this provider.
		/// </summary>
		/// <param name="interestProvider">Reference to interest provider.</param>
		/// <param name="duration">How long the interest provider stays registered.</param>
		public bool RegisterProvider(IInterestProvider interestProvider, float duration = 0.0f)
		{
			int version = interestProvider.Version;

			for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
			{
				RuntimeProvider runtimeProvider = _runtimeProviders[i];
				if (ReferenceEquals(runtimeProvider.Provider, interestProvider) == true)
				{
					bool hasChanged = false;

					if (duration > runtimeProvider.Duration)
					{
						runtimeProvider.Duration = duration;
						hasChanged = true;
					}

					if (version != runtimeProvider.Version)
					{
						runtimeProvider.Version = version;
						hasChanged = true;
					}

					return hasChanged;
				}
			}

			if (_runtimeProvidersPool.TryPop(out RuntimeProvider newProvider) == false)
			{
				newProvider = new RuntimeProvider();
			}

			newProvider.Provider = interestProvider;
			newProvider.Duration = duration;
			newProvider.Version  = version;

			_runtimeProviders.Add(newProvider);

			return true;
		}

		/// <summary>
		/// Unregister other interest provider from self.
		/// </summary>
		/// <param name="interestProvider">Reference to interest provider.</param>
		/// <param name="recursive">If true, the interest provider is unregistered recursively from all registered interest providers.</param>
		public bool UnregisterProvider(IInterestProvider interestProvider, bool recursive)
		{
			bool unregistered = false;

			if (recursive == true)
			{
				for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
				{
					unregistered |= _runtimeProviders[i].Provider.UnregisterProvider(interestProvider, true);
				}
			}

			for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
			{
				RuntimeProvider runtimeProvider = _runtimeProviders[i];
				if (ReferenceEquals(runtimeProvider.Provider, interestProvider) == true)
				{
					_runtimeProviders.RemoveAt(i);

					runtimeProvider.Clear();
					_runtimeProvidersPool.Push(runtimeProvider);

					unregistered = true;
					break;
				}
			}

			return unregistered;
		}

		/// <summary>
		/// Add all valid registered interest providers to the set of unique providers.
		/// </summary>
		/// <param name="interestProviders">Set of interest providers that will be filled.</param>
		/// <param name="recursive">If true, registered interest providers are processed recursively.</param>
		public void GetProviders(InterestProviderSet interestProviders, bool recursive)
		{
			for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
			{
				RuntimeProvider   runtimeProvider  = _runtimeProviders[i];
				IInterestProvider interestProvider = runtimeProvider.Provider;

				if (interestProvider.Version == runtimeProvider.Version)
				{
					interestProviders.Add(interestProvider);

					if (recursive == true)
					{
						interestProvider.GetProviders(interestProviders, true);
					}
				}
			}
		}

		// InterestProvider INTERFACE

		/// <summary>
		/// Returns whether this provider should be processed for given player.
		/// </summary>
		/// <param name="playerView">Player interest view with player information used for filtering.</param>
		public virtual bool IsPlayerInterested(PlayerInterestView playerView)
		{
			return true;
		}

		/// <summary>
		/// Add interest providers that are relevant to the player.
		/// This method can be used for adding providers selectively based on gameplay conditions.
		/// </summary>
		/// <param name="playerView">Player interest view used for filtering.</param>
		/// <param name="interestProviders">Interest providers set that will be filled.</param>
		protected virtual void GetProvidersForPlayer(PlayerInterestView playerView, InterestProviderSet interestProviders) {}

		/// <summary>
		/// Add interest cells that are relevant to the player.
		/// This method can be used for adding shape cells selectively based on gameplay conditions, using InterestShape.GetCells().
		/// </summary>
		/// <param name="playerView">Player interest view used for filtering.</param>
		/// <param name="cells">Interest cells that will be set.</param>
		protected virtual void GetCellsForPlayer(PlayerInterestView playerView, HashSet<int> cells) {}

		/// <summary>
		/// Called on the end of Spawned(), before registering child providers to self and before self-registring to GlobalInterestManager (only global interest providers).
		/// </summary>
		protected virtual void OnSpawned() {}

		/// <summary>
		/// Called on the start of Despawn(), after self-unregistering from GlobalInterestManager and after unregistering all registered providers.
		/// </summary>
		protected virtual void OnDespawned(NetworkRunner runner, bool hasState) {}

		/// <summary>
		/// Called on the end of FixedUpdateNetwork(), after updating internal providers list.
		/// </summary>
		protected virtual void OnFixedUpdateNetwork() {}

		/// <summary>
		/// Called on the end of Render(), after updating internal providers list.
		/// </summary>
		protected virtual void OnRender() {}

		/// <summary>
		/// Called when an object with InterestProvider or PlayerInterestManager component is selected.
		/// </summary>
		/// <param name="playerView">Provides player information, is valid only if the selected object has PlayerInterestManager component.</param>
		/// <param name="isSelected">True if the selected object has this component, false otherwise.</param>
		protected virtual void OnDrawGizmosForPlayer(PlayerInterestView playerView, bool isSelected) {}

		// NetworkBehaviour INTERFACE

		public override sealed void Spawned()
		{
			_isGlobal     = IsGlobal;
			_sortOrder    = SortOrder;
			_interestMode = InterestMode;
			_isRegistered = false;

			OnSpawned();

			Register();
		}

		public override sealed void Despawned(NetworkRunner runner, bool hasState)
		{
			Unregister();

			OnDespawned(runner, hasState);

			IsGlobal     = _isGlobal;
			SortOrder    = _sortOrder;
			InterestMode = _interestMode;

			_isRegistered   = default;
			_isGlobal       = default;
			_sortOrder      = default;
			_interestMode   = default;
			_runtimeProviders.Clear();

			Release();
		}

		public override sealed void FixedUpdateNetwork()
		{
			if (Runner.IsForward == true)
			{
				UpdateRuntimeProviders(Runner.DeltaTime);
			}

			OnFixedUpdateNetwork();
		}

		public override sealed void Render()
		{
			if (Object.IsInSimulation == false)
			{
				UpdateRuntimeProviders(Time.unscaledDeltaTime);
			}

			OnRender();
		}

		// IInterestProvider INTERFACE

		int           IInterestProvider.Version      => _version;
		int           IInterestProvider.SortOrder    => SortOrder;
		EInterestMode IInterestProvider.InterestMode => InterestMode;

		void IInterestProvider.GetProvidersForPlayer(PlayerInterestView playerView, InterestProviderSet interestProviders, bool recursive)
		{
			for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
			{
				RuntimeProvider   runtimeProvider  = _runtimeProviders[i];
				IInterestProvider interestProvider = runtimeProvider.Provider;

				if (interestProvider.Version == runtimeProvider.Version && interestProvider.IsPlayerInterested(playerView) == true)
				{
					interestProviders.Add(interestProvider);

					if (recursive == true)
					{
						interestProvider.GetProvidersForPlayer(playerView, interestProviders, true);
					}
				}
			}

			GetProvidersForPlayer(playerView, interestProviders);
		}

		void IInterestProvider.GetCellsForPlayer(PlayerInterestView playerView, HashSet<int> cells)
		{
			GetCells(playerView, cells);
			GetCellsForPlayer(playerView, cells);
		}

		void IInterestProvider.DrawGizmosForPlayer(PlayerInterestView playerView)
		{
			DrawGizmos(playerView, false);
			OnDrawGizmosForPlayer(playerView, false);
		}

		// PRIVATE METHODS

		private void Register()
		{
			for (int i = 0, count = ChildProviders.Count; i < count; ++i)
			{
				InterestProvider interestProvider = ChildProviders[i];
				if (interestProvider != null)
				{
					RegisterProvider(interestProvider);
				}
			}

			if (IsGlobal == true)
			{
				_isRegistered = Runner.GetGlobalInterestManager().RegisterProvider(this);
			}
		}

		private void Unregister()
		{
			if (_isRegistered == true)
			{
				Runner.GetGlobalInterestManager().UnregisterProvider(this, false);
				_isRegistered = false;
			}

			for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
			{
				RuntimeProvider registeredProvider = _runtimeProviders[i];
				registeredProvider.Clear();

				_runtimeProvidersPool.Push(registeredProvider);
			}

			_runtimeProviders.Clear();
		}

		private void UpdateRuntimeProviders(float deltaTime)
		{
			for (int i = _runtimeProviders.Count - 1; i >= 0; --i)
			{
				RuntimeProvider   runtimeProvider  = _runtimeProviders[i];
				IInterestProvider interestProvider = runtimeProvider.Provider;

				bool isValid = interestProvider.Version == runtimeProvider.Version;
				if (isValid == true)
				{
					if (runtimeProvider.Duration > 0.0f)
					{
						runtimeProvider.Duration -= deltaTime;
						if (runtimeProvider.Duration <= 0.0f)
						{
							isValid = false;
						}
					}
				}

				if (isValid == false)
				{
					_runtimeProviders.RemoveAt(i);

					runtimeProvider.Clear();
					_runtimeProvidersPool.Push(runtimeProvider);
				}
			}
		}

		private void GetCells(PlayerInterestView playerView, HashSet<int> cells)
		{
			if (InterestMode == EInterestMode.Override)
			{
				cells.Clear();
			}

			for (int i = 0, count = Shapes.Count; i < count; ++i)
			{
				InterestShape shape = Shapes[i];
				if (shape != null)
				{
					shape.GetCells(playerView, cells, InterestMode);
				}
			}
		}

		private void DrawGizmos(PlayerInterestView playerView, bool isSelected)
		{
			for (int i = 0, count = Shapes.Count; i < count; ++i)
			{
				InterestShape shape = Shapes[i];
				if (shape != null)
				{
					shape.DrawGizmo(playerView);
				}
			}

			if (isSelected == true)
			{
				if (Object != null)
				{
					for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
					{
						RuntimeProvider   runtimeProvider  = _runtimeProviders[i];
						IInterestProvider interestProvider = runtimeProvider.Provider;

						if (interestProvider != null && interestProvider.Transform != null && interestProvider.Version == runtimeProvider.Version)
						{
							InterestUtility.DrawLine(Transform.position, interestProvider.Transform.position, RegisteredProvidersConnectionColor);
						}
					}
				}
				else
				{
					if (ChildProviders != null && ChildProviders.Count > 0)
					{
						for (int i = 0, count = ChildProviders.Count; i < count; ++i)
						{
							IInterestProvider interestProvider = ChildProviders[i];
							if (interestProvider != null && interestProvider.Transform != null)
							{
								InterestUtility.DrawLine(Transform.position, interestProvider.Transform.position, ChildProvidersConnectionColor);
							}
						}
					}
				}
			}
		}

#if UNITY_EDITOR
		[UnityEditor.DrawGizmo(UnityEditor.GizmoType.Active)]
		private static void DrawGizmo(InterestProvider interestProvider, UnityEditor.GizmoType gizmoType)
		{
			if ((gizmoType & UnityEditor.GizmoType.Active) != UnityEditor.GizmoType.Active)
				return;

			PlayerInterestView playerView = interestProvider as PlayerInterestView;

			interestProvider.DrawGizmos(playerView, true);
			interestProvider.OnDrawGizmosForPlayer(playerView, true);
		}
#endif

		// DATA STRUCTURES

		private sealed class RuntimeProvider
		{
			public IInterestProvider Provider;
			public float             Duration;
			public int               Version;

			public void Clear()
			{
				Provider = default;
				Duration = default;
				Version  = default;
			}
		}
	}
}
