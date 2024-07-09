namespace Fusion.Addons.AnimationController
{
	using System;
	using UnityEngine;

	public static partial class AnimationUtility
	{
		// PUBLIC METHODS

		public static float InterpolateWeight(float from, float to, float alpha)
		{
			float distance = to - from;

			if (distance == 1.0f || distance == -1.0f)
				return alpha < 0.5f ? from : to;

			return from + distance * alpha;
		}

		public static float InterpolateTime(float from, float to, float length, float alpha)
		{
			float time;

			if (to >= from)
			{
				time = Mathf.Lerp(from, to, alpha);
			}
			else
			{
				time = Mathf.Lerp(from, to + length, alpha);
				if (time > length)
				{
					time -= length;
				}
			}

			return time;
		}

		public static void LogInfo(SimulationBehaviour behaviour, params object[] messages)
		{
			Log(behaviour, default, EAnimationLogType.Info, messages);
		}

		public static void LogWarning(SimulationBehaviour behaviour, params object[] messages)
		{
			Log(behaviour, default, EAnimationLogType.Warning, messages);
		}

		public static void LogError(SimulationBehaviour behaviour, params object[] messages)
		{
			Log(behaviour, default, EAnimationLogType.Error, messages);
		}

		public static void Log(SimulationBehaviour behaviour, string logGroup, EAnimationLogType logType, params object[] messages)
		{
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

			NetworkRunner runner = behaviour.Runner;

#if UNITY_EDITOR
			if (Time.frameCount % 2 == 0)
			{
				stringBuilder.Append($"<color=#19A7CE>[{Time.frameCount}]</color>");
			}
			else
			{
				stringBuilder.Append($"<color=#FC3C3C>[{Time.frameCount}]</color>");
			}

			if (runner != null)
			{
				bool isInFixedUpdate = runner.Stage != default;
				bool isInForwardTick = runner.IsForward == true;

				if (isInFixedUpdate == true)
				{
					if (isInForwardTick == true)
					{
						stringBuilder.Append($"<color=#FFFF00>[{runner.Tick.Raw}]</color>");
					}
					else
					{
						stringBuilder.Append($"<color=#FF0000>[{runner.Tick.Raw}]</color>");
					}
				}
				else
				{
					stringBuilder.Append($"<color=#00FF00>[{runner.Tick.Raw}]</color>");
				}
			}
			else
			{
				stringBuilder.Append($"[--]");
			}
#else
			stringBuilder.Append($"[{Time.frameCount}]");

			if (runner != null)
			{
				bool isInFixedUpdate = runner.Stage != default;
				bool isInForwardTick = runner.IsForward == true;

				if (isInFixedUpdate == true)
				{
					if (isInForwardTick == true)
					{
						stringBuilder.Append($"[FF]");
					}
					else
					{
						stringBuilder.Append($"[FR]");
					}
				}
				else
				{
					stringBuilder.Append($"[RF]");
				}

				stringBuilder.Append($"[{runner.Tick.Raw}]");
			}
			else
			{
				stringBuilder.Append($"[--]");
				stringBuilder.Append($"[--]");
			}
#endif

			if (string.IsNullOrEmpty(logGroup) == false)
			{
				stringBuilder.Append($"[{logGroup}]");
			}

			stringBuilder.Append($"[{behaviour.name}]");

			for (int i = 0; i < messages.Length; ++i)
			{
				object message = messages[i];
				if (message != null)
				{
					stringBuilder.Append($" ");
					stringBuilder.Append(message);
				}
			}

			switch (logType)
			{
				case EAnimationLogType.Info:
					UnityEngine.Debug.Log(stringBuilder.ToString(), behaviour);
					break;
				case EAnimationLogType.Warning:
					UnityEngine.Debug.LogWarning(stringBuilder.ToString(), behaviour);
					break;
				case EAnimationLogType.Error:
					UnityEngine.Debug.LogError(stringBuilder.ToString(), behaviour);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
			}
		}
	}
}
