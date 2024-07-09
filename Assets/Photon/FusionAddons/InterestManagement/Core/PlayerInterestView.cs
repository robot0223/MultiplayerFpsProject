namespace Fusion.Addons.InterestManagement
{
	using UnityEngine;

	/// <summary>
	/// Special InterestProvider that tracks transform/view information and other interest providers related to a specific player.
	/// There can be more instances per player spawned at the same time, but only one can be active - set to PlayerInterestManager.InterestView.
	/// </summary>
	[DefaultExecutionOrder(PlayerInterestView.EXECUTION_ORDER)]
	public class PlayerInterestView : InterestProvider
	{
		// CONSTANTS

		public new const int EXECUTION_ORDER = InterestProvider.EXECUTION_ORDER - 1000;

		// PUBLIC MEMBERS

		/// <summary>
		/// Owner player, equals to object input authority.
		/// </summary>
		public PlayerRef Player => _player;

		/// <summary>
		/// Reference to player object transform. Typically a character root.
		/// </summary>
		public Transform PlayerTransform => _playerTransform;

		/// <summary>
		/// Cached player position.
		/// Prefer this property over PlayerTransform.position when resolving player interest conditions.
		/// </summary>
		public Vector3 PlayerPosition { get; private set; }

		/// <summary>
		/// Cached player rotation.
		/// Prefer this property over PlayerTransform.rotation when resolving player interest conditions.
		/// </summary>
		public Quaternion PlayerRotation { get; private set; }

		/// <summary>
		/// Cached normalized player direction.
		/// Prefer this property over PlayerTransform.rotation * Vector3.forward when resolving player interest conditions.
		/// </summary>
		public Vector3 PlayerDirection { get; private set; }

		/// <summary>
		/// Reference to player camera transform. Typically this can be an offsetted child object of the player object.
		/// Don't set Camera.main.transform, otherwise all players will share same view with incorrect interest.
		/// </summary>
		public Transform CameraTransform => _cameraTransform;

		/// <summary>
		/// Cached player camera position.
		/// Prefer this property over CameraTransform.position when resolving player interest conditions.
		/// </summary>
		public Vector3 CameraPosition { get; private set; }

		/// <summary>
		/// Cached player camera rotation.
		/// Prefer this property over CameraTransform.rotation when resolving player interest conditions.
		/// </summary>
		public Quaternion CameraRotation { get; private set; }

		/// <summary>
		/// Cached normalized player camera direction.
		/// Prefer this property over CameraTransform.rotation * Vector3.forward when resolving player interest conditions.
		/// </summary>
		public Vector3 CameraDirection { get; private set; }

		// PRIVATE MEMBERS

		private PlayerRef _player;
		private Transform _playerTransform;
		private Transform _cameraTransform;

		// PUBLIC METHODS

		/// <summary>
		/// Updates player and camera transform and caches their position, rotation and direction.
		/// </summary>
		public void SetPlayerInfo(Transform playerTransform, Transform cameraTransform)
		{
			_playerTransform = playerTransform;
			_cameraTransform = cameraTransform;

			UpdatePlayerInfo();
		}

		// PlayerInterestView INTERFACE

		/// <summary>
		/// Called on the end of Spawned(), after setting player info defaults, before registering child providers to self and before self-registring to GlobalInterestManager.
		/// </summary>
		protected virtual void OnViewSpawned() {}

		/// <summary>
		/// Called on the start of Despawn(), after self-unregistering from GlobalInterestManager and after unregistering all registered providers.
		/// </summary>
		protected virtual void OnViewDespawned(NetworkRunner runner, bool hasState) {}

		/// <summary>
		/// Called on the end of FixedUpdateNetwork(), after updating internal providers list and player info - transforms and related cached properties.
		/// </summary>
		protected virtual void OnViewFixedUpdateNetwork() {}

		/// <summary>
		/// Called on the end of Render(), after updating internal providers list and player info - transforms and related cached properties.
		/// </summary>
		protected virtual void OnViewRender() {}

		// InterestProvider INTERFACE

		protected override sealed void OnSpawned()
		{
			_player = Object.InputAuthority;

			UpdatePlayerInfo();

			if (IsGlobal == true)
			{
				Debug.LogWarning($"{nameof(IsGlobal)} is enabled on {GetType().Name} ({name}). This is not allowed and will be disabled.", gameObject);
				IsGlobal = false;
			}

			OnViewSpawned();

			GlobalInterestManager globalInterestManager = Runner.GetGlobalInterestManager();
			globalInterestManager.RegisterPlayerView(this);
		}

		protected override sealed void OnDespawned(NetworkRunner runner, bool hasState)
		{
			GlobalInterestManager globalInterestManager = runner.GetGlobalInterestManager();
			globalInterestManager.UnregisterPlayerView(this);

			OnViewDespawned(runner, hasState);

			_player          = default;
			_playerTransform = default;
			_cameraTransform = default;
		}

		protected override sealed void OnFixedUpdateNetwork()
		{
			_player = Object.InputAuthority;

			UpdatePlayerInfo();

			OnViewFixedUpdateNetwork();
		}

		protected override sealed void OnRender()
		{
			if (Object.IsInSimulation == false)
			{
				_player = Object.InputAuthority;

				UpdatePlayerInfo();
			}

			OnViewRender();
		}

		// PRIVATE METHODS

		private void UpdatePlayerInfo()
		{
			if (_playerTransform == null)
			{
				_playerTransform = Transform;
			}

			if (_cameraTransform == null)
			{
				_cameraTransform = _playerTransform;
			}

			_playerTransform.GetPositionAndRotation(out Vector3 playerPosition, out Quaternion playerRotation);
			PlayerPosition  = playerPosition;
			PlayerRotation  = playerRotation;
			PlayerDirection = playerRotation * Vector3.forward;

			if (ReferenceEquals(_cameraTransform, _playerTransform) == true)
			{
				CameraPosition  = PlayerPosition;
				CameraRotation  = PlayerRotation;
				CameraDirection = PlayerDirection;
			}
			else
			{
				_cameraTransform.GetPositionAndRotation(out Vector3 cameraPosition, out Quaternion cameraRotation);
				CameraPosition  = cameraPosition;
				CameraRotation  = cameraRotation;
				CameraDirection = cameraRotation * Vector3.forward;
			}
		}
	}
}
