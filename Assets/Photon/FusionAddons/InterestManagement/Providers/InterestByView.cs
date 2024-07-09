namespace Fusion.Addons.InterestManagement
{
	using UnityEngine;

	/// <summary>
	/// Provides interest to players within certain view angle and distance from this object.
	/// </summary>
	public sealed class InterestByView : InterestProvider
	{
		// PUBLIC MEMBERS

		[Range(0.0f, 90.0f)]
		[InlineHelp][Tooltip("Maximum angle between player camera look direction and direction to this object.")]
		public float MaxViewAngle;
		[InlineHelp][Tooltip("Minimum distance from player camera to this object.")]
		public float MinViewDistance;
		[InlineHelp][Tooltip("Maximum distance from player camera to this object.")]
		public float MaxViewDistance;

		// PRIVATE MEMBERS

		private float _cachedAngle;
		private float _cachedAngleCos;

		// InterestProvider INTERFACE

		/// <summary>
		/// Returns whether this provider should be processed for given player.
		/// </summary>
		/// <param name="playerView">Player interest view with player information used for filtering.</param>
		public override bool IsPlayerInterested(PlayerInterestView playerView)
		{
			Vector3 positionDifference = Transform.position - playerView.CameraPosition;

			// We compare player camera position against transform of this object.
			// It is enough for relatively small objects, bigger objects might need a special handling - distance from a closest point on bounding box for example.
			float sqrDistance = Vector3.SqrMagnitude(positionDifference);
			if (sqrDistance > MaxViewDistance * MaxViewDistance || sqrDistance < MinViewDistance * MinViewDistance)
				return false;

			if (_cachedAngle != MaxViewAngle)
			{
				_cachedAngle    = MaxViewAngle;
				_cachedAngleCos = Mathf.Cos(MaxViewAngle * Mathf.Deg2Rad);
			}

			float dot = Vector3.Dot(playerView.CameraDirection, Vector3.Normalize(positionDifference));
			if (dot < _cachedAngleCos)
				return false;

			return true;
		}

		/// <summary>
		/// Called when an object with InterestByView or PlayerInterestManager component is selected.
		/// </summary>
		/// <param name="playerView">Provides player information, is valid only if the selected object has PlayerInterestManager component.</param>
		/// <param name="isSelected">True if the selected object has this component, false otherwise.</param>
		protected override void OnDrawGizmosForPlayer(PlayerInterestView playerView, bool isSelected)
		{
			if (isSelected == true)
			{
				InterestUtility.DrawAngle(Transform.position - Transform.rotation * Vector3.forward * MaxViewDistance, Transform.rotation, MaxViewAngle, MinViewDistance, MaxViewDistance, Color.green);
			}
		}
	}
}
