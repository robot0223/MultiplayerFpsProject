namespace Fusion.Addons.AnimationController
{
	public unsafe interface IAnimationFadingProvider
	{
		float FadingSpeed { get; set; }
	}

	public unsafe sealed class AnimationFadingProvider : AnimationPropertyProvider<IAnimationFadingProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int GetWordCount(IAnimationFadingProvider item)
		{
			return 1;
		}

		protected override void Read(IAnimationFadingProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			item.FadingSpeed = *((float*)readWriteInfo.Ptr);
			++readWriteInfo.Ptr;
		}

		protected override void Write(IAnimationFadingProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			*((float*)readWriteInfo.Ptr) = item.FadingSpeed;
			++readWriteInfo.Ptr;
		}

		protected override void Interpolate(IAnimationFadingProvider item, ref AnimationInterpolationInfo interpolationInfo)
		{
			item.FadingSpeed = interpolationInfo.Alpha < 0.5f ? interpolationInfo.FromBuffer.ReinterpretState<float>(interpolationInfo.Offset) : interpolationInfo.ToBuffer.ReinterpretState<float>(interpolationInfo.Offset);
			++interpolationInfo.Offset;
		}
	}
}
