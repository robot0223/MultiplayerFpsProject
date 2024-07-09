namespace Fusion.Addons.AnimationController
{
	public unsafe interface IAnimationWeightProvider
	{
		float Weight             { get; set; }
		float InterpolatedWeight { get; set; }
	}

	public unsafe sealed class AnimationWeightProvider : AnimationPropertyProvider<IAnimationWeightProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int GetWordCount(IAnimationWeightProvider item)
		{
			return 1;
		}

		protected override void Read(IAnimationWeightProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			item.Weight = *((float*)readWriteInfo.Ptr);
			++readWriteInfo.Ptr;
		}

		protected override void Write(IAnimationWeightProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			*((float*)readWriteInfo.Ptr) = item.Weight;
			++readWriteInfo.Ptr;
		}

		protected override void Interpolate(IAnimationWeightProvider item, ref AnimationInterpolationInfo interpolationInfo)
		{
			item.InterpolatedWeight = AnimationUtility.InterpolateWeight(interpolationInfo.FromBuffer.ReinterpretState<float>(interpolationInfo.Offset), interpolationInfo.ToBuffer.ReinterpretState<float>(interpolationInfo.Offset), interpolationInfo.Alpha);
			++interpolationInfo.Offset;
		}
	}
}
