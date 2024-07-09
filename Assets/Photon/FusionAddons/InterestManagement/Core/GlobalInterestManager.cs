namespace Fusion.Addons.InterestManagement
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Global interest manager registered as a singleton on NetworkRunner.
	/// Provides access to PlayerInterestManager and PlayerInterestView instances and methods for filtering.
	/// Tracks global interest providers.
	/// There is always only one instance of GlobalInterestManager.
	/// </summary>
	[DefaultExecutionOrder(GlobalInterestManager.EXECUTION_ORDER)]
	public class GlobalInterestManager : SimulationBehaviour
	{
		// CONSTANTS

		public const int EXECUTION_ORDER = InterestProvider.EXECUTION_ORDER + 500;

		// PRIVATE MEMBERS

		[SerializeField][Tooltip("Size of each cell in the interest grid. Use only for Client/Server architecture.")]
		private EInterestCellSize _interestCellSize = EInterestCellSize.Default;

		private List<RuntimeProvider>                        _runtimeProviders       = new List<RuntimeProvider>();
		private List<PlayerInterestView>                     _playerInterestViews    = new List<PlayerInterestView>();
		private Dictionary<PlayerRef, PlayerInterestManager> _playerInterestManagers = new Dictionary<PlayerRef, PlayerInterestManager>();

		private static Stack<RuntimeProvider> _runtimeProvidersPool = new Stack<RuntimeProvider>();

		// PUBLIC METHODS

		/// <summary>
		/// Register interest provider to self.
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
		/// Unregister interest provider from self.
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
		/// Get all registered interest providers.
		/// The set is cleared on the beginning and the result is sorted.
		/// </summary>
		/// <param name="interestProviders">Set of interest providers that will be filled.</param>
		/// <param name="recursive">If true, registered interest providers are processed recursively.</param>
		public void GetProviders(InterestProviderSet interestProviders, bool recursive)
		{
			interestProviders.Clear();

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

			interestProviders.Sort();
		}

		/// <summary>
		/// Get all interest providers that are relevant to the player.
		/// This processes active PlayerInterestView (PlayerInterestManager.InterestView) and all registered interest providers.
		/// </summary>
		/// <param name="player">Player reference.</param>
		/// <param name="interestProviders">Interest providers set that will be filled.</param>
		public bool GetProvidersForPlayer(PlayerRef player, InterestProviderSet interestProviders)
		{
			return GetProvidersForPlayer(player, interestProviders, out _);
		}

		/// <summary>
		/// Get all interest providers that are relevant to the player.
		/// This processes active PlayerInterestView (PlayerInterestManager.InterestView) and all registered interest providers.
		/// </summary>
		/// <param name="player">Player reference.</param>
		/// <param name="interestProviders">Interest providers set that will be filled.</param>
		/// <param name="playerView">Returned reference to active PlayerInterestView.</param>
		public bool GetProvidersForPlayer(PlayerRef player, InterestProviderSet interestProviders, out PlayerInterestView playerView)
		{
			interestProviders.Clear();
			playerView = null;

			if (_playerInterestManagers.TryGetValue(player, out PlayerInterestManager playerInterestManager) == false)
				return false;
			if (playerInterestManager.Player != player)
				return false;

			playerView = playerInterestManager.InterestView;
			if (playerView == null)
				return false;

			IInterestProvider playerInterestProvider = playerView;
			if (playerInterestProvider.IsPlayerInterested(playerView) == true)
			{
				interestProviders.Add(playerInterestProvider);
				playerInterestProvider.GetProvidersForPlayer(playerView, interestProviders, true);
			}

			for (int i = 0, count = _runtimeProviders.Count; i < count; ++i)
			{
				RuntimeProvider   runtimeProvider  = _runtimeProviders[i];
				IInterestProvider interestProvider = runtimeProvider.Provider;

				if (interestProvider.Version == runtimeProvider.Version && interestProvider.IsPlayerInterested(playerView) == true)
				{
					interestProviders.Add(interestProvider);
					interestProvider.GetProvidersForPlayer(playerView, interestProviders, true);
				}
			}

			interestProviders.Sort();
			return true;
		}

		/// <summary>
		/// Try getting registered PlayerInterestManager for a given player.
		/// </summary>
		/// <param name="player">Player reference.</param>
		/// <param name="playerInterestManager">Returned reference to registered PlayerInterestManager.</param>
		public bool TryGetPlayerManager(PlayerRef player, out PlayerInterestManager playerInterestManager)
		{
			if (_playerInterestManagers.TryGetValue(player, out playerInterestManager) == true && playerInterestManager != null && playerInterestManager.Player == player)
				return true;

			playerInterestManager = default;
			return false;
		}

		/// <summary>
		/// Registers PlayerInterestManager for a given player.
		/// </summary>
		/// <param name="player">Player reference.</param>
		/// <param name="playerInterestManager">Reference to PlayerInterestManager instance that will be registered.</param>
		public bool RegisterPlayerManager(PlayerRef player, PlayerInterestManager playerInterestManager)
		{
			if (player.IsRealPlayer == false)
				return false;

			_playerInterestManagers[player] = playerInterestManager;
			return true;
		}

		/// <summary>
		/// Unregisters PlayerInterestManager for a given player.
		/// If the manager reference doesn't match, the currently registered manager stays untouched.
		/// </summary>
		/// <param name="player">Player reference.</param>
		/// <param name="playerInterestManager">Reference to PlayerInterestManager instance that will be unregistered.</param>
		public bool UnregisterPlayerManager(PlayerRef player, PlayerInterestManager playerInterestManager)
		{
			if (_playerInterestManagers.TryGetValue(player, out PlayerInterestManager registeredManager) == true && ReferenceEquals(registeredManager, playerInterestManager) == true)
			{
				_playerInterestManagers.Remove(player);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Fills the list with all valid player interest managers.
		/// </summary>
		public void GetPlayerManagers(List<PlayerInterestManager> playerInterestManagers)
		{
			playerInterestManagers.Clear();

			foreach (var playerInterestManagerKVP in _playerInterestManagers)
			{
				PlayerInterestManager playerInterestManager = playerInterestManagerKVP.Value;
				playerInterestManagers.Add(playerInterestManager);
			}
		}

		/// <summary>
		/// Fills the list with all valid player interest managers using a filter function.
		/// </summary>
		public void GetPlayerManagers(List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, bool> filter)
		{
			playerInterestManagers.Clear();

			foreach (var playerInterestManagerKVP in _playerInterestManagers)
			{
				PlayerInterestManager playerInterestManager = playerInterestManagerKVP.Value;
				if (filter(playerInterestManager) == true)
				{
					playerInterestManagers.Add(playerInterestManager);
				}
			}
		}

		/// <summary>Fills the list with all valid player interest managers using a filter function.</summary>
		public void GetPlayerManagers<T1>                    (List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, T1,                     bool> filter, T1 arg1)                                              { playerInterestManagers.Clear(); foreach (var playerInterestProviderItem in _playerInterestManagers) { PlayerInterestManager playerInterestProvider = playerInterestProviderItem.Value; if (filter(playerInterestProvider, arg1)                               == true) { playerInterestManagers.Add(playerInterestProvider); } } }
		/// <summary>Fills the list with all valid player interest managers using a filter function.</summary>
		public void GetPlayerManagers<T1, T2>                (List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, T1, T2,                 bool> filter, T1 arg1, T2 arg2)                                     { playerInterestManagers.Clear(); foreach (var playerInterestProviderItem in _playerInterestManagers) { PlayerInterestManager playerInterestProvider = playerInterestProviderItem.Value; if (filter(playerInterestProvider, arg1, arg2)                         == true) { playerInterestManagers.Add(playerInterestProvider); } } }
		/// <summary>Fills the list with all valid player interest managers using a filter function.</summary>
		public void GetPlayerManagers<T1, T2, T3>            (List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, T1, T2, T3,             bool> filter, T1 arg1, T2 arg2, T3 arg3)                            { playerInterestManagers.Clear(); foreach (var playerInterestProviderItem in _playerInterestManagers) { PlayerInterestManager playerInterestProvider = playerInterestProviderItem.Value; if (filter(playerInterestProvider, arg1, arg2, arg3)                   == true) { playerInterestManagers.Add(playerInterestProvider); } } }
		/// <summary>Fills the list with all valid player interest managers using a filter function.</summary>
		public void GetPlayerManagers<T1, T2, T3, T4>        (List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, T1, T2, T3, T4,         bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4)                   { playerInterestManagers.Clear(); foreach (var playerInterestProviderItem in _playerInterestManagers) { PlayerInterestManager playerInterestProvider = playerInterestProviderItem.Value; if (filter(playerInterestProvider, arg1, arg2, arg3, arg4)             == true) { playerInterestManagers.Add(playerInterestProvider); } } }
		/// <summary>Fills the list with all valid player interest managers using a filter function.</summary>
		public void GetPlayerManagers<T1, T2, T3, T4, T5>    (List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, T1, T2, T3, T4, T5,     bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)          { playerInterestManagers.Clear(); foreach (var playerInterestProviderItem in _playerInterestManagers) { PlayerInterestManager playerInterestProvider = playerInterestProviderItem.Value; if (filter(playerInterestProvider, arg1, arg2, arg3, arg4, arg5)       == true) { playerInterestManagers.Add(playerInterestProvider); } } }
		/// <summary>Fills the list with all valid player interest managers using a filter function.</summary>
		public void GetPlayerManagers<T1, T2, T3, T4, T5, T6>(List<PlayerInterestManager> playerInterestManagers, Func<PlayerInterestManager, T1, T2, T3, T4, T5, T6, bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) { playerInterestManagers.Clear(); foreach (var playerInterestProviderItem in _playerInterestManagers) { PlayerInterestManager playerInterestProvider = playerInterestProviderItem.Value; if (filter(playerInterestProvider, arg1, arg2, arg3, arg4, arg5, arg6) == true) { playerInterestManagers.Add(playerInterestProvider); } } }

		/// <summary>
		/// Try getting active PlayerInterestView for a given player.
		/// There might be more PlayerInterestView instances registered, but only one is active (PlayerInterestManager.InterestView).
		/// </summary>
		/// <param name="player">Player reference.</param>
		/// <param name="playerView">Returned reference to active PlayerInterestView.</param>
		public bool TryGetPlayerView(PlayerRef player, out PlayerInterestView playerView)
		{
			if (_playerInterestManagers.TryGetValue(player, out PlayerInterestManager playerInterestManager) == true && playerInterestManager != null && playerInterestManager.Player == player)
			{
				playerView = playerInterestManager.InterestView;
				return true;
			}

			playerView = default;
			return false;
		}

		/// <summary>
		/// Registers PlayerInterestView. One player can have multiple views registered at the same time.
		/// </summary>
		/// <param name="playerView">Reference to PlayerInterestView instance that will be registered.</param>
		public bool RegisterPlayerView(PlayerInterestView playerView)
		{
			for (int i = 0, count = _playerInterestViews.Count; i < count; ++i)
			{
				PlayerInterestView registeredView = _playerInterestViews[i];
				if (ReferenceEquals(registeredView, playerView) == true)
					return false;
			}

			_playerInterestViews.Add(playerView);
			return true;
		}

		/// <summary>
		/// Unregisters PlayerInterestView.
		/// </summary>
		/// <param name="playerView">Reference to PlayerInterestView instance that will be unregistered.</param>
		public bool UnregisterPlayerView(PlayerInterestView playerView)
		{
			for (int i = 0, count = _playerInterestViews.Count; i < count; ++i)
			{
				PlayerInterestView registeredView = _playerInterestViews[i];
				if (ReferenceEquals(registeredView, playerView) == true)
				{
					_playerInterestViews.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Fills the list with all valid player interest views.
		/// </summary>
		public void GetPlayerViews(List<PlayerInterestView> playerViews)
		{
			playerViews.Clear();
			playerViews.AddRange(_playerInterestViews);
		}

		/// <summary>
		/// Fills the list with all valid player interest views using a filter function.
		/// </summary>
		public void GetPlayerViews(List<PlayerInterestView> playerViews, Func<PlayerInterestView, bool> filter)
		{
			playerViews.Clear();

			foreach (PlayerInterestView playerView in _playerInterestViews)
			{
				if (filter(playerView) == true)
				{
					playerViews.Add(playerView);
				}
			}
		}

		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T1>                    (List<PlayerInterestView> playerViews, Func<PlayerInterestView, T1,                     bool> filter, T1 arg1)                                              { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (filter(playerView, arg1)                               == true) { playerViews.Add(playerView); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T1, T2>                (List<PlayerInterestView> playerViews, Func<PlayerInterestView, T1, T2,                 bool> filter, T1 arg1, T2 arg2)                                     { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (filter(playerView, arg1, arg2)                         == true) { playerViews.Add(playerView); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T1, T2, T3>            (List<PlayerInterestView> playerViews, Func<PlayerInterestView, T1, T2, T3,             bool> filter, T1 arg1, T2 arg2, T3 arg3)                            { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (filter(playerView, arg1, arg2, arg3)                   == true) { playerViews.Add(playerView); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T1, T2, T3, T4>        (List<PlayerInterestView> playerViews, Func<PlayerInterestView, T1, T2, T3, T4,         bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4)                   { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (filter(playerView, arg1, arg2, arg3, arg4)             == true) { playerViews.Add(playerView); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T1, T2, T3, T4, T5>    (List<PlayerInterestView> playerViews, Func<PlayerInterestView, T1, T2, T3, T4, T5,     bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)          { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (filter(playerView, arg1, arg2, arg3, arg4, arg5)       == true) { playerViews.Add(playerView); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T1, T2, T3, T4, T5, T6>(List<PlayerInterestView> playerViews, Func<PlayerInterestView, T1, T2, T3, T4, T5, T6, bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (filter(playerView, arg1, arg2, arg3, arg4, arg5, arg6) == true) { playerViews.Add(playerView); } } }

		/// <summary>
		/// Fills the list with all valid player interest views using a filter function.
		/// </summary>
		public void GetPlayerViews<T>(List<T> playerViews, Func<T, bool> filter) where T : PlayerInterestView
		{
			playerViews.Clear();

			foreach (PlayerInterestView playerView in _playerInterestViews)
			{
				if (playerView is T playerViewAsT && filter(playerViewAsT) == true)
				{
					playerViews.Add(playerViewAsT);
				}
			}
		}

		/// <summary>Fills the list with all valid player interest views using a filter function.</summary>
		public void GetPlayerViews<T, T1>                    (List<T> playerViews, Func<T, T1,                     bool> filter, T1 arg1)                                              where T : PlayerInterestView { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (playerView is T playerViewAsT && filter(playerViewAsT, arg1)                               == true) { playerViews.Add(playerViewAsT); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary> where T : PlayerInterestView
		public void GetPlayerViews<T, T1, T2>                (List<T> playerViews, Func<T, T1, T2,                 bool> filter, T1 arg1, T2 arg2)                                     where T : PlayerInterestView { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (playerView is T playerViewAsT && filter(playerViewAsT, arg1, arg2)                         == true) { playerViews.Add(playerViewAsT); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary> where T : PlayerInterestView
		public void GetPlayerViews<T, T1, T2, T3>            (List<T> playerViews, Func<T, T1, T2, T3,             bool> filter, T1 arg1, T2 arg2, T3 arg3)                            where T : PlayerInterestView { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (playerView is T playerViewAsT && filter(playerViewAsT, arg1, arg2, arg3)                   == true) { playerViews.Add(playerViewAsT); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary> where T : PlayerInterestView
		public void GetPlayerViews<T, T1, T2, T3, T4>        (List<T> playerViews, Func<T, T1, T2, T3, T4,         bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4)                   where T : PlayerInterestView { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (playerView is T playerViewAsT && filter(playerViewAsT, arg1, arg2, arg3, arg4)             == true) { playerViews.Add(playerViewAsT); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary> where T : PlayerInterestView
		public void GetPlayerViews<T, T1, T2, T3, T4, T5>    (List<T> playerViews, Func<T, T1, T2, T3, T4, T5,     bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)          where T : PlayerInterestView { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (playerView is T playerViewAsT && filter(playerViewAsT, arg1, arg2, arg3, arg4, arg5)       == true) { playerViews.Add(playerViewAsT); } } }
		/// <summary>Fills the list with all valid player interest views using a filter function.</summary> where T : PlayerInterestView
		public void GetPlayerViews<T, T1, T2, T3, T4, T5, T6>(List<T> playerViews, Func<T, T1, T2, T3, T4, T5, T6, bool> filter, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) where T : PlayerInterestView { playerViews.Clear(); foreach (PlayerInterestView playerView in _playerInterestViews) { if (playerView is T playerViewAsT && filter(playerViewAsT, arg1, arg2, arg3, arg4, arg5, arg6) == true) { playerViews.Add(playerViewAsT); } } }

		// GlobalInterestManager INTERFACE

		/// <summary>
		/// Called on the end of Awake(), after updating internal providers list.
		/// </summary>
		protected void OnAwake() {}

		/// <summary>
		/// Called on the end of Update(), after updating internal providers list.
		/// </summary>
		protected void OnUpdate() {}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			NetworkRunner runner = GetComponent<NetworkRunner>();
			if (runner == null)
			{
				Debug.LogError($"{GetType().Name} should be instantiated on a game object with NetworkRunner component.");
			}

			if (_interestCellSize != EInterestCellSize.Default)
			{
				Simulation.AreaOfInterest.CELL_SIZE = (int)_interestCellSize;
				Debug.LogWarning($"[{GetType().Name}] Set interest cell size to {Simulation.AreaOfInterest.CELL_SIZE}m.");
			}

			OnAwake();
		}

		private void Update()
		{
			if (_runtimeProviders.Count > 0)
			{
				float deltaTime = Time.unscaledDeltaTime;

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

			OnUpdate();
		}

		// DATA STRUCTURES

		private enum EInterestCellSize
		{
			Default = 0,
			_8      = 8,
			_16     = 16,
			_24     = 24,
			_32     = 32,
			_48     = 48,
			_64     = 64,
		}

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
