namespace Fusion.Addons.AnimationController
{
	public static partial class AnimationProfiler
	{
		// PUBLIC METHODS

		[System.Diagnostics.Conditional(AnimationController.PROFILING_SCRIPT_DEFINE)]
		public static void BeginSample(string name)
		{
			UnityEngine.Profiling.Profiler.BeginSample(name);
		}

		[System.Diagnostics.Conditional(AnimationController.PROFILING_SCRIPT_DEFINE)]
		public static void EndSample()
		{
			UnityEngine.Profiling.Profiler.EndSample();
		}

		[System.Diagnostics.Conditional(AnimationController.LOGGING_SCRIPT_DEFINE)]
		public static void Log(SimulationBehaviour simulationBehaviour, params object[] messages)
		{
			AnimationUtility.Log(simulationBehaviour, default, EAnimationLogType.Info, messages);
		}
	}
}
