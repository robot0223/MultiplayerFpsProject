namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;
	using UnityEngine;

	public interface IInterestProvider
	{
		/// <summary>
		/// Reference to Transform component.
		/// </summary>
		Transform Transform { get; }

		/// <summary>
		/// Version used for tracking object state and releasing references.
		/// </summary>
		int Version { get; }

		/// <summary>
		/// Defines order in which interest providers are processed. Provider with lower value will be processed first.
		/// </summary>
		int SortOrder { get; }

		/// <summary>
		/// Defines processing behavior of interest provider.
	    /// <list type="bullet">
	    /// <item><description>Default - Interest from this provider is processed additively to existing interest.</description></item>
	    /// <item><description>Override - Interest from this provider overrides all existing interest.</description></item>
	    /// </list>
		/// </summary>
		EInterestMode InterestMode { get; }

		/// <summary>
		/// Mark all active registrations (including global) of this provider as invalid.
		/// Use this method to prevent further processing of this provider.
		/// Called automatically on despawn.
		/// </summary>
		void Release();

		/// <summary>
		/// Register other interest provider to self, allowing to create hierarchy of interest providers.
		/// Registered providers are processed only if the player is interested in this provider.
		/// </summary>
		/// <param name="interestProvider">Reference to interest provider.</param>
		/// <param name="duration">How long the interest provider stays registered.</param>
		bool RegisterProvider(IInterestProvider interestProvider, float duration = 0.0f);

		/// <summary>
		/// Unregister other interest provider from self.
		/// </summary>
		/// <param name="interestProvider">Reference to interest provider.</param>
		/// <param name="recursive">If true, the interest provider is unregistered recursively from all registered interest providers.</param>
		bool UnregisterProvider(IInterestProvider interestProvider, bool recursive);

		/// <summary>
		/// Add all valid registered interest providers to the set of unique providers.
		/// </summary>
		/// <param name="interestProviders">Set of interest providers that will be filled.</param>
		/// <param name="recursive">If true, registered interest providers are processed recursively.</param>
		void GetProviders(InterestProviderSet interestProviders, bool recursive);

		/// <summary>
		/// Returns whether this provider should be processed for given player.
		/// </summary>
		/// <param name="playerView">Player interest view with player information used for filtering.</param>
		bool IsPlayerInterested(PlayerInterestView playerView);

		/// <summary>
		/// Add interest providers that are relevant to the player.
		/// </summary>
		/// <param name="playerView">Player interest view used for filtering.</param>
		/// <param name="interestProviders">Interest providers set that will be filled.</param>
		/// <param name="recursive">Indicates whether registered interest providers should be iterated recursively.</param>
		void GetProvidersForPlayer(PlayerInterestView playerView, InterestProviderSet interestProviders, bool recursive);

		/// <summary>
		/// Add interest cells that are relevant to the player.
		/// This method typically converts custom list of InterestShape objects to cells, using InterestShape.GetCells().
		/// </summary>
		/// <param name="playerView">Player interest view used for filtering.</param>
		/// <param name="cells">Interest cells that will be set.</param>
		void GetCellsForPlayer(PlayerInterestView playerView, HashSet<int> cells);

		/// <summary>
		/// Called when an object with PlayerInterestManager component is selected.
		/// </summary>
		/// <param name="playerView">Provides player information when PlayerInterestManager component is selected.</param>
		void DrawGizmosForPlayer(PlayerInterestView playerView);
	}
}
