namespace Fusion.Addons.AnimationController
{
	/// <summary>
	/// Defines how PlayableGraph is evaluated.
    /// <list type="bullet">
    /// <item><description>None - Evaluation is disabled.</description></item>
    /// <item><description>Full - Evaluation runs every tick/frame.</description></item>
    /// <item><description>Periodic - Evaluation runs periodically.</description></item>
    /// <item><description>Interlaced - Evaluation runs once per [COUNT] ticks/frames.</description></item>
    /// </list>
	/// </summary>
	public enum EEvaluationMode
	{
		None       = 0,
		Full       = 1,
		Periodic   = 2,
		Interlaced = 3,
	}

	/// <summary>
	/// Defines target when setting up evaluation. Fixed and Render updates have separate configuration.
    /// <list type="bullet">
    /// <item><description>None - Default value, unused.</description></item>
    /// <item><description>FixedUpdate - Set configuration for fixed update.</description></item>
    /// <item><description>RenderUpdate - Set configuration for render update.</description></item>
    /// </list>
	/// </summary>
	public enum EEvaluationTarget
	{
		None         = 0,
		FixedUpdate  = 1,
		RenderUpdate = 2,
	}

	public enum EAnimationLogType
	{
		Info    = 0,
		Warning = 1,
		Error   = 2,
	}
}
