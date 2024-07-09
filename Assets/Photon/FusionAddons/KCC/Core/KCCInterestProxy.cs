namespace Fusion.Addons.KCC
{
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// This component serves as an AoI position proxy. It is useful for objects which don't need their own NetworkTransform component, but still need to be filtered by AoI.
	/// Example: Platform processor is a separate NetworkObject with KCCInterestProxy component. Upon spawn the processor is parented usually under the player object with KCC.
	/// There is no need to synchronize position of platform processor because it is driven locally. But if a player runs out of AoI, we want to stop sending data.
	/// </summary>
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(10000)]
	public sealed unsafe class KCCInterestProxy : NetworkTRSP
	{
		// PUBLIC MEMBERS

		public Transform PositionSource => _positionSource;

		// PRIVATE MEMBERS

		[SerializeField]
		private Transform _positionSource;

		private Transform _defaultPositionSource;
		private bool      _hasExplicitPositionSource;
		private bool      _isSpawned;

		// PUBLIC METHODS

		public void SetPosition(Vector3 position)
		{
			if (_isSpawned == true)
			{
				State.Position = position;
			}
		}

		public void SetPositionSource(Transform positionSource)
		{
			_positionSource = positionSource;
			_hasExplicitPositionSource = true;

			Synchronize();
		}

		public void ResetPositionSource()
		{
			_positionSource = _defaultPositionSource;
			_hasExplicitPositionSource = false;

			Synchronize();
		}

		public void FindPositionSourceInParent()
		{
			FindPositionSourceInParent(true);
			Synchronize();
		}

		public void Synchronize()
		{
			if (_isSpawned == true && _positionSource != null)
			{
				State.Position = _positionSource.position;
			}
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			_isSpawned = true;

			// Don't synchronize this component at all, it is needed on server only.
			ReplicateToAll(false);

			if (_hasExplicitPositionSource == false && _positionSource == null)
			{
				State.Position = transform.position;

				FindPositionSourceInParent(false);
			}

			Synchronize();
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			_isSpawned = false;

			ResetPositionSource();
		}

		public override void FixedUpdateNetwork()
		{
			Synchronize();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_defaultPositionSource = _positionSource;
		}

		// PRIVATE METHODS

		private void FindPositionSourceInParent(bool isExplicit)
		{
			Transform parentTransform = transform.parent;
			while (parentTransform != null)
			{
				NetworkObject networkObject = parentTransform.GetComponent<NetworkObject>();
				if (networkObject != null)
				{
					_positionSource = parentTransform;
					_hasExplicitPositionSource = isExplicit;
					break;
				}

				parentTransform = parentTransform.parent;
			}
		}
	}
}
