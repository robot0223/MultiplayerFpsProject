namespace Fusion.Addons.InterestManagement
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;

	/// <summary>
	/// Contains methods to for extending NetworkRunner public API.
	/// </summary>
	public static class NetworkRunnerExtensions
	{
		// PRIVATE MEMBERS

		private static bool                          _isInitialized;
		private static bool                          _isNotAvailable;
		private static FieldInfo                     _simulationFieldInfo;
		private static FieldInfo                     _playersConnectionsFieldInfo;
		private static FieldInfo                     _areaOfInterestCellsFieldInfo;
		private static List<PlayerRef>               _interestCellPlayers = new List<PlayerRef>();
		private static List<NetworkId>               _interestCellObjects = new List<NetworkId>();
		private static HashSet<NetworkId>            _uniqueInterestObjects = new HashSet<NetworkId>();
		private static Dictionary<PlayerRef, object> _playersAsObjects = new Dictionary<PlayerRef, object>();

		// PUBLIC METHODS

		/// <summary>
		/// Returns global interest manager singleton. New one (of type GlobalInterestManager) is created on NetworkRunner if it doesn't exist.
		/// </summary>
		public static GlobalInterestManager GetGlobalInterestManager(this NetworkRunner runner)
		{
			TryGetGlobalInterestManager(runner, out GlobalInterestManager globalInterestManager, true);
			return globalInterestManager;
		}

		/// <summary>
		/// Returns global interest manager singleton of type T.
		/// </summary>
		/// <param name="runner">Reference to NetworkRunner.</param>
		/// <param name="globalInterestManager">Returned reference to global interest manager.</param>
		/// <param name="createIfNotExists">Create new interest manager of type T on NetworkRunner if it doesn't exist.</param>
		public static bool TryGetGlobalInterestManager<T>(this NetworkRunner runner, out T globalInterestManager, bool createIfNotExists) where T : GlobalInterestManager
		{
			globalInterestManager = default;

			if (ReferenceEquals(runner, null) == true)
				return false;

			if (runner.TryGetComponent(out globalInterestManager) == true)
				return true;

			if (createIfNotExists == false)
				return false;

			if (runner.TryGetComponent(out GlobalInterestManager randomGlobalInterestManager) == true)
			{
				Debug.LogError($"Trying to get/create {typeof(T).Name} on {nameof(NetworkRunner)}({runner.name}), but {randomGlobalInterestManager.GetType().Name} already exists. This is not allowed!", runner.gameObject);
				return false;
			}

			Debug.Log($"Creating new {typeof(T).Name} on {nameof(NetworkRunner)}({runner.name}).", runner.gameObject);
			globalInterestManager = runner.gameObject.AddComponent<T>();
			return true;
		}

		/// <summary>
		/// Returns player interest manager for given player.
		/// </summary>
		public static bool TryGetPlayerInterestManager(this NetworkRunner runner, PlayerRef player, out PlayerInterestManager playerInterestManager)
		{
			if (runner.TryGetGlobalInterestManager(out GlobalInterestManager globalInterestManager, true) == true)
				return globalInterestManager.TryGetPlayerManager(player, out playerInterestManager);

			playerInterestManager = default;
			return false;
		}

		/// <summary>
		/// Returns player interest view for given player.
		/// </summary>
		public static bool TryGetPlayerInterestView(this NetworkRunner runner, PlayerRef player, out PlayerInterestView playerInterestView)
		{
			if (runner.TryGetGlobalInterestManager(out GlobalInterestManager globalInterestManager, true) == true)
				return globalInterestManager.TryGetPlayerView(player, out playerInterestView);

			playerInterestView = default;
			return false;
		}

		/// <summary>
		/// Returns reference to internal Fusion hashset with interest cells.
		/// DO NOT USE, THIS IS FOR INTERNAL PURPOSES ONLY!
		/// </summary>
		public static HashSet<int> GetAreaOfInterestCells(NetworkRunner runner, PlayerRef player)
		{
			if (Initialize(runner, player) == false)
				return default;

			object simulation = _simulationFieldInfo.GetValue(runner);
			if (ReferenceEquals(simulation, null) == true)
				return default;

			IDictionary playerConnections = _playersConnectionsFieldInfo.GetValue(simulation) as IDictionary;
			if (ReferenceEquals(playerConnections, null) == true)
				return default;

			if (_playersAsObjects.TryGetValue(player, out object playerAsObject) == false)
			{
				playerAsObject = player;
				_playersAsObjects[player] = playerAsObject;
			}

			if (playerConnections.Contains(playerAsObject) == false)
				return default;

			object simulationConnection = playerConnections[playerAsObject];
			if (ReferenceEquals(simulationConnection, null) == true)
				return default;

			return _areaOfInterestCellsFieldInfo.GetValue(simulationConnection) as HashSet<int>;
		}

		/// <summary>
		/// Returns all objects in the player interest.
		/// DO NOT USE, THIS IS FOR INTERNAL PURPOSES ONLY!
		/// </summary>
		public static bool GetObjectsInPlayerInterest(NetworkRunner runner, PlayerRef player, List<NetworkObject> objects, HashSet<int> playerInterestCells = null)
		{
			objects.Clear();

			if (Initialize(runner, player) == false)
				return false;

			if (playerInterestCells == null)
			{
				playerInterestCells = GetAreaOfInterestCells(runner, player);
			}

			if (playerInterestCells == null)
				return false;

			_uniqueInterestObjects.Clear();

			Simulation simulation = (Simulation)_simulationFieldInfo.GetValue(runner);

			foreach (int cell in playerInterestCells)
			{
				simulation.GetObjectsAndPlayersInAreaOfInterestCell(cell, _interestCellPlayers, _interestCellObjects);

				foreach (NetworkId interestCellObject in _interestCellObjects)
				{
					if (_uniqueInterestObjects.Add(interestCellObject) == true)
					{
						NetworkObject networkObject = runner.FindObject(interestCellObject);
						if (ReferenceEquals(networkObject, null) == false)
						{
							objects.Add(networkObject);
						}
					}
				}
			}

			return true;
		}

		// PRIVATE METHODS

		private static bool Initialize(NetworkRunner runner, PlayerRef player)
		{
			if (_isInitialized == true)
				return true;
			if (_isNotAvailable == true)
				return false;

			if (_simulationFieldInfo == null)
			{
				try
				{
					_simulationFieldInfo = typeof(NetworkRunner).GetField("_simulation", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}

				if (_simulationFieldInfo == null)
				{
					_isNotAvailable = true;
					return false;
				}
			}

			object simulation = _simulationFieldInfo.GetValue(runner);
			if (ReferenceEquals(simulation, null) == true)
				return false;

			if (_playersConnectionsFieldInfo == null)
			{
				try
				{
					_playersConnectionsFieldInfo = typeof(Simulation).GetField("_playersConnections", BindingFlags.NonPublic | BindingFlags.Instance);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}

				if (_playersConnectionsFieldInfo == null)
				{
					_isNotAvailable = true;
					return false;
				}
			}

			IDictionary playerConnections = _playersConnectionsFieldInfo.GetValue(simulation) as IDictionary;
			if (ReferenceEquals(playerConnections, null) == true)
				return false;

			if (_playersAsObjects.TryGetValue(player, out object playerAsObject) == false)
			{
				playerAsObject = player;
				_playersAsObjects[player] = playerAsObject;
			}

			if (playerConnections.Contains(playerAsObject) == false)
				return false;

			object simulationConnection = playerConnections[playerAsObject];
			if (ReferenceEquals(simulationConnection, null) == true)
				return false;

			if (_areaOfInterestCellsFieldInfo == null)
			{
				try
				{
					_areaOfInterestCellsFieldInfo = simulationConnection.GetType().GetField("AreaOfInterestCells", BindingFlags.Instance | BindingFlags.Public);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}

				if (_areaOfInterestCellsFieldInfo == null)
				{
					_isNotAvailable = true;
					return false;
				}
			}

			_isInitialized = true;
			return true;
		}
	}
}
