namespace Fusion.Addons.InterestManagement
{
	using UnityEngine;

	/// <summary>
	/// Provides interest to players in certain distance from this object.
	/// </summary>
	public sealed class InterestByDistance : InterestProvider
	{
		// PUBLIC MEMBERS

		[InlineHelp][Tooltip("Minimum distance from player camera to this object.")]
		public float MinDistance;
		[InlineHelp][Tooltip("Maximum distance from player camera to this object.")]
		public float MaxDistance;

		// InterestProvider INTERFACE

		/// <summary>
		/// Returns whether this provider should be processed for given player.
		/// </summary>
		/// <param name="playerView">Player interest view with player information used for filtering.</param>
		public override bool IsPlayerInterested(PlayerInterestView playerView)
		{
			// We compare player camera position against transform of this object.
			// It is enough for relatively small objects, bigger objects might need a special handling - distance from a closest point on bounding box for example.
			Vector3 positionDifference = Transform.position - playerView.CameraPosition;
			float sqrDistance = Vector3.SqrMagnitude(positionDifference);
			return sqrDistance <= MaxDistance * MaxDistance && sqrDistance >= MinDistance * MinDistance;
		}

		/// <summary>
		/// Called when an object with InterestByDistance or PlayerInterestManager component is selected.
		/// </summary>
		/// <param name="playerView">Provides player information, is valid only if the selected object has PlayerInterestManager component.</param>
		/// <param name="isSelected">True if the selected object has this component, false otherwise.</param>
		protected override void OnDrawGizmosForPlayer(PlayerInterestView playerView, bool isSelected)
		{
			if (isSelected == true)
			{
				InterestUtility.DrawWireSphere(Transform.position, MinDistance, Color.red);
				InterestUtility.DrawWireSphere(Transform.position, MaxDistance, Color.green);
			}
		}
	}
}
