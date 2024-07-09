namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using Unity.Profiling;
	using UnityEngine;

	/// <summary>
	/// Interest manager for players.
	/// Provides reference to active PlayerInterestView for owner player.
	/// Also provides reference to observed player which can be different from owner player.
	/// There is always one unique instance of PlayerInterestManager per player.
	/// </summary>
	[DefaultExecutionOrder(PlayerInterestManager.EXECUTION_ORDER)]
	public class PlayerInterestManager : NetworkBehaviour
	{
		// CONSTANTS

		public const int EXECUTION_ORDER = InterestProvider.EXECUTION_ORDER + 1000;

		// PUBLIC MEMBERS

		/// <summary>
		/// Owner player, equals to object input authority.
		/// </summary>
		public PlayerRef Player { get => _player; }

		/// <summary>
		/// Observed player. Can be used to spectate another player and get updates for objects in his interest.
		/// </summary>
		public PlayerRef ObservedPlayer { get => _observedPlayer.IsRealPlayer == true ? _observedPlayer : _player; set => _observedPlayer = value; }

		/// <summary>
		/// Reference to active interest view. A player can have multiple views spawned and switch between them.
		/// Only the active view is used to provide player interest, others are ignored.
		/// </summary>
		public PlayerInterestView InterestView { get => _interestView; set => _interestView = value; }

		/// <summary>
		/// Last tick in which player interest was updated.
		/// </summary>
		public Tick LastUpdateTick { get => _lastUpdateTick; }

		/// <summary>
		/// Last known interest cell count.
		/// The value comes from regular update or drawing gizmos.
		/// At runtime the value for server/host is always 0, the interest is not processed.
		/// </summary>
		public int InterestCellCount { get => _interestCellCount; }

		/// <summary>
		/// List of interest cells from Gizmo preview.
		/// This is used for a editor preview of objects in player interest on state authority.
		/// </summary>
		public HashSet<int> GizmoInterestCells { get => _gizmoCells; }

		// PRIVATE MEMBERS

		[SerializeField]
		[InlineHelp][Tooltip("Reference to view that provides interest for the player.")]
		private PlayerInterestView _interestView;

		private PlayerRef           _player;
		private PlayerRef           _observedPlayer;
		private Tick                _lastUpdateTick;
		private bool                _isUpdateRequested;
		private int                 _interestCellCount;
		private PlayerInterestView  _interestViewBackup;
		private InterestProviderSet _interestProviders = new InterestProviderSet();

		private static HashSet<int>        _gizmoCells              = new HashSet<int>();
		private static List<NetworkObject> _objectsInPlayerInterest = new List<NetworkObject>();
		private static ProfilerMarker      _updateInterestsMarker   = new ProfilerMarker($"{nameof(PlayerInterestManager)}.{nameof(UpdatePlayerInterests)}");

		// PUBLIC METHODS

		/// <summary>
		/// Returns PlayerInterestView for observed player.
		/// </summary>
		/// <param name="observedPlayerView">PlayerInterestView instance.</param>
		public bool TryGetObservedPlayerView(out PlayerInterestView observedPlayerView)
		{
			GlobalInterestManager globalInterestManager = Runner.GetGlobalInterestManager();
			if (globalInterestManager.TryGetPlayerView(ObservedPlayer, out PlayerInterestView playerView) == true && playerView != null)
			{
				observedPlayerView = playerView;
				return true;
			}

			observedPlayerView = default;
			return false;
		}

		/// <summary>
		/// Request update of player interest. This allows for priority update after an important action so the player receives data as soon as possible.
		/// </summary>
		public void RequestInterestUpdate()
		{
			_isUpdateRequested = true;
		}

		/// <summary>
		/// Checks input authority and other properties and immediately propagates changes.
		/// </summary>
		public void RefreshPlayerProperties()
		{
			UpdatePlayerProperties();
		}

		// PlayerInterestManager INTERFACE

		/// <summary>
		/// Can be used to defer evaluation of interest.
		/// </summary>
		public virtual bool CanUpdatePlayerInterest(bool isUpdateRequested) => isUpdateRequested == true || Runner.IsForward == true;

		/// <summary>
		/// Called before the player interest is updated.
		/// </summary>
		protected virtual void BeforePlayerInterestUpdate() {}

		/// <summary>
		/// Called after the player interest is updated.
		/// </summary>
		protected virtual void AfterPlayerInterestUpdate(bool success, PlayerInterestView playerView) {}

		/// <summary>
		/// Called on the end of Awake().
		/// </summary>
		protected virtual void OnAwake() {}

		/// <summary>
		/// Called on the end of Spawned(), after updating player properties and registering to global interest manager.
		/// </summary>
		protected virtual void OnSpawned() {}

		/// <summary>
		/// Called on the start of Despawned(), before unregistering from global interest manager and resetting state.
		/// </summary>
		protected virtual void OnDespawned(NetworkRunner runner, bool hasState) {}

		/// <summary>
		/// Called on the end of FixedUpdateNetwork(), after updating player properties and resolving player interest.
		/// </summary>
		protected virtual void OnFixedUpdateNetwork() {}

		/// <summary>
		/// Called on the end of Render(), after updating player properties.
		/// </summary>
		protected virtual void OnRender() {}

		// NetworkBehaviour INTERFACE

		public override sealed void Spawned()
		{
			_player = Object.InputAuthority;

			if (_player.IsRealPlayer == true)
			{
				GlobalInterestManager globalInterestManager = Runner.GetGlobalInterestManager();
				globalInterestManager.RegisterPlayerManager(_player, this);
			}

			UpdatePlayerProperties();

			OnSpawned();
		}

		public override sealed void Despawned(NetworkRunner runner, bool hasState)
		{
			OnDespawned(runner, hasState);

			Runner.GetGlobalInterestManager().UnregisterPlayerManager(_player, this);

			_interestView = _interestViewBackup;

			_player            = default;
			_observedPlayer    = default;
			_lastUpdateTick    = default;
			_isUpdateRequested = default;
			_interestCellCount = default;
			_interestProviders.Clear();
		}

		public override sealed void FixedUpdateNetwork()
		{
			UpdatePlayerProperties();
			UpdatePlayerInterests();

			OnFixedUpdateNetwork();
		}

		public override sealed void Render()
		{
			UpdatePlayerProperties();

			OnRender();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			if (_interestView == null)
			{
				_interestView = GetComponent<PlayerInterestView>();
			}

			_interestViewBackup = _interestView;

			OnAwake();
		}

		// PRIVATE METHODS

		private void UpdatePlayerProperties()
		{
			PlayerRef oldPlayer = _player;
			PlayerRef newPlayer = Object.InputAuthority;

			if (oldPlayer != newPlayer)
			{
				GlobalInterestManager globalInterestManager = Runner.GetGlobalInterestManager();
				globalInterestManager.UnregisterPlayerManager(oldPlayer, this);

				_player = newPlayer;

				if (newPlayer.IsRealPlayer == true)
				{
					globalInterestManager.RegisterPlayerManager(newPlayer, this);
				}
			}

			if (_interestView == null)
			{
				// Replace Unity null object by true null before updating player interests.
				// This allows using ReferenceEquals() instead of == operator during evaluations.
				_interestView = null;
			}
		}

		private void UpdatePlayerInterests()
		{
			NetworkRunner runner = Runner;
			if (IsInterestManagementActive(runner) == false)
				return;

			if (_player.IsRealPlayer == false)
				return;
			if (_player == runner.LocalPlayer)
				return;
			if (CanUpdatePlayerInterest(_isUpdateRequested) == false)
				return;

			_isUpdateRequested = false;

			_updateInterestsMarker.Begin();

			BeforePlayerInterestUpdate();

			bool               success    = false;
			PlayerInterestView playerView = null;

			HashSet<int> playerCells = NetworkRunnerExtensions.GetAreaOfInterestCells(runner, _player);
			if (playerCells != null)
			{
				GlobalInterestManager globalInterestManager = Runner.GetGlobalInterestManager();
				if (globalInterestManager.GetProvidersForPlayer(ObservedPlayer, _interestProviders, out PlayerInterestView playerInterestView) == true)
				{
					GetInterestCells(_interestProviders, playerInterestView, playerCells);

					success    = true;
					playerView = playerInterestView;

					_lastUpdateTick = Runner.Tick;
				}

				_interestCellCount = playerCells.Count;
			}

			AfterPlayerInterestUpdate(success, playerView);

			_updateInterestsMarker.End();
		}

		private static void GetInterestCells(InterestProviderSet interestProviders, PlayerInterestView playerView, HashSet<int> cells)
		{
			cells.Clear();
			interestProviders.Sort();

			List<IInterestProvider> interestProviderValues = interestProviders.ReadOnlyValues;
			for (int i = Mathf.Max(0, interestProviders.GetLastOverrideProviderIndex()), count = interestProviderValues.Count; i < count; ++i)
			{
				IInterestProvider interestProvider = interestProviderValues[i];
				interestProvider.GetCellsForPlayer(playerView, cells);
			}
		}

		private static bool IsInterestManagementActive(NetworkRunner runner)
		{
			if (ReferenceEquals(runner, null) == true)
				return false;

			return runner.IsServer == true && runner.GameMode != GameMode.Shared && runner.Config.Simulation.ReplicationFeatures == NetworkProjectConfig.ReplicationFeatures.SchedulingAndInterestManagement;
		}

		private void DrawGizmos()
		{
			if (Application.isPlaying == true)
			{
				NetworkRunner runner = Runner;
				if (IsInterestManagementActive(runner) == true)
				{
					HashSet<int>            interestCells     = default;
					List<IInterestProvider> interestProviders = default;

					if (runner.LocalPlayer != _player)
					{
						interestCells = NetworkRunnerExtensions.GetAreaOfInterestCells(runner, _player);
					}

					GlobalInterestManager globalInterestManager = runner.GetGlobalInterestManager();
					if (globalInterestManager.GetProvidersForPlayer(ObservedPlayer, _interestProviders, out PlayerInterestView playerView) == true)
					{
						interestProviders = _interestProviders.ReadOnlyValues;

						if (runner.LocalPlayer == _player)
						{
							GetInterestCells(_interestProviders, playerView, _gizmoCells);
							interestCells = _gizmoCells;
						}
					}

					if (interestCells != null)
					{
						_interestCellCount = interestCells.Count;

						if (InterestUtility.DrawInterestCells == true)
						{
							InterestUtility.DrawCells(interestCells);
						}

						if (InterestUtility.DrawPlayerInterest == true)
						{
							if (NetworkRunnerExtensions.GetObjectsInPlayerInterest(Runner, Player, _objectsInPlayerInterest, interestCells) == true)
							{
								InterestUtility.DrawObjectsInPlayerInterest(_objectsInPlayerInterest);
							}
						}
					}

					if (interestProviders != null && InterestUtility.DrawPlayerInterest == true)
					{
						for (int i = Mathf.Max(0, _interestProviders.GetLastOverrideProviderIndex()), count = interestProviders.Count; i < count; ++i)
						{
							IInterestProvider interestProvider = interestProviders[i];
							interestProvider.DrawGizmosForPlayer(playerView);
						}
					}
				}
			}
			else
			{
				if (_interestView != null)
				{
					_interestView.SetPlayerInfo(_interestView.Transform, _interestView.Transform);

					_interestProviders.Clear();
					AddInterestProviderRecursive(_interestProviders, _interestView);
					_interestProviders.Sort();

					GetInterestCells(_interestProviders, _interestView, _gizmoCells);
					InterestUtility.DrawCells(_gizmoCells);

					_interestCellCount = _gizmoCells.Count;
				}
			}

			static void AddInterestProviderRecursive(InterestProviderSet interestProviders, InterestProvider interestProvider)
			{
				if (interestProvider == null)
					return;

				if (interestProviders.Add(interestProvider) == true)
				{
					if (interestProvider.ChildProviders != null)
					{
						for (int i = 0, count = interestProvider.ChildProviders.Count; i < count; ++i)
						{
							AddInterestProviderRecursive(interestProviders, interestProvider.ChildProviders[i]);
						}
					}
				}
			}
		}

#if UNITY_EDITOR
		[UnityEditor.DrawGizmo(UnityEditor.GizmoType.Active)]
		private static void DrawGizmo(PlayerInterestManager playerInterestManager, UnityEditor.GizmoType gizmoType)
		{
			if ((gizmoType & UnityEditor.GizmoType.Active) != UnityEditor.GizmoType.Active)
				return;

			playerInterestManager.DrawGizmos();
		}
#endif
	}
}
