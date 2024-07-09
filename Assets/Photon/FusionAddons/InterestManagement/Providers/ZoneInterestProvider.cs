namespace Fusion.Addons.InterestManagement
{
	using UnityEngine;

	/// <summary>
	/// Provides interest to players standing within defined Bounds.
	/// </summary>
	public class ZoneInterestProvider : InterestProvider
	{
		// PUBLIC MEMBERS

		[InlineHelp][Tooltip("Local bounds the player must stand within to get interest provided by this component.")]
		public Bounds Bounds = new Bounds(Vector3.zero, Vector3.one * 10.0f);

		public static Color HandleColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);

		// InterestProvider INTERFACE

		/// <summary>
		/// Returns whether this provider should be processed for given player.
		/// </summary>
		/// <param name="playerView">Player interest view with player information used for filtering.</param>
		public override bool IsPlayerInterested(PlayerInterestView playerView)
		{
			Bounds bounds = Bounds;
			bounds.center += Transform.position;
			return bounds.Contains(playerView.PlayerPosition);
		}
	}
}
