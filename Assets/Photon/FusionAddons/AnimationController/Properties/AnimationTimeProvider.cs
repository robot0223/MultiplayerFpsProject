namespace Fusion.Addons.AnimationController
{
	public unsafe interface IAnimationTimeProvider
	{
		float AnimationTime             { get; set; }
		float InterpolatedAnimationTime { get; set; }
	}

	public unsafe sealed class AnimationTimeProvider : AnimationPropertyProvider<IAnimationTimeProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int GetWordCount(IAnimationTimeProvider item)
		{
			return 1;
		}

		protected override void Read(IAnimationTimeProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			item.AnimationTime = *((float*)readWriteInfo.Ptr);
			++readWriteInfo.Ptr;
		}

		protected override void Write(IAnimationTimeProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			*((float*)readWriteInfo.Ptr) = item.AnimationTime;
			++readWriteInfo.Ptr;
		}

		protected override void Interpolate(IAnimationTimeProvider item, ref AnimationInterpolationInfo interpolationInfo)
		{
			item.InterpolatedAnimationTime = AnimationUtility.InterpolateTime(interpolationInfo.FromBuffer.ReinterpretState<float>(interpolationInfo.Offset), interpolationInfo.ToBuffer.ReinterpretState<float>(interpolationInfo.Offset), 1.0f, interpolationInfo.Alpha);
			++interpolationInfo.Offset;
		}
	}
}
