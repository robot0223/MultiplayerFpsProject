namespace Fusion.Addons.AnimationController
{
	using System.Collections.Generic;
	using UnityEngine.Animations;
	using UnityEngine.Playables;

	/// <summary>
	/// Animation state that mixes outputs from chidl states.
	/// </summary>
	public class MixerState : AnimationState, IAnimationStateOwner
	{
		// PRIVATE MEMBERS

		private AnimationMixerPlayable _mixer;

		// AnimationState INTERFACE

		protected override void CreatePlayable()
		{
			_mixer = AnimationMixerPlayable.Create(Controller.Graph, States.Count);
			AddPlayable(_mixer, 0);
		}

		protected override void OnDespawned()
		{
			if (_mixer.IsValid() == true)
			{
				_mixer.Destroy();
			}
		}

		// IAnimationStateOwner INTERFACE

		AnimationMixerPlayable IAnimationStateOwner.Mixer => _mixer;

		void IAnimationStateOwner.Activate(AnimationState source, float fadeDuration)
		{
			IList<AnimationState> states = States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AnimationState state = states[i];
				if (state.Port != source.Port)
				{
					state.Deactivate(fadeDuration, true);
				}
			}

			if ((FadingSpeed == 0.0f && Weight >= 1.0f) || FadingSpeed > 0.0f)
				return;

			if (fadeDuration <= 0.0f)
			{
				Weight      = 1.0f;
				FadingSpeed = 0.0f;
			}
			else
			{
				FadingSpeed = 1.0f / fadeDuration;
			}

			AnimationProfiler.Log(Controller, $"{nameof(MixerState)}.{nameof(Activate)} ({name}), Fade Duration: {fadeDuration:F3}", gameObject);

			OnActivate();

			Owner.Activate(this, fadeDuration);
		}

		void IAnimationStateOwner.Deactivate(AnimationState source, float fadeDuration)
		{
			if ((FadingSpeed == 0.0f && Weight <= 0.0f) || FadingSpeed < 0.0f)
				return;

			if (fadeDuration <= 0.0f)
			{
				Weight      = 0.0f;
				FadingSpeed = 0.0f;
			}
			else
			{
				FadingSpeed = 1.0f / -fadeDuration;
			}

			AnimationProfiler.Log(Controller, $"{nameof(MixerState)}.{nameof(Deactivate)} ({name}), Fade Duration: {fadeDuration:F3}", gameObject);

			OnDeactivate();

			Owner.Deactivate(this, fadeDuration);
		}
	}
}
