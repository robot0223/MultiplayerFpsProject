namespace Fusion.Addons.AnimationController
{
	public unsafe interface IAnimationWeightsProvider
	{
		float[] Weights             { get; }
		float[] InterpolatedWeights { get; }
	}

	public unsafe sealed class AnimationWeightsProvider : AnimationPropertyProvider<IAnimationWeightsProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int GetWordCount(IAnimationWeightsProvider item)
		{
			return item.Weights.Length;
		}

		protected override void Read(IAnimationWeightsProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			float[] weights = item.Weights;

			for (int i = 0, count = weights.Length; i < count; ++i)
			{
				weights[i] = *((float*)readWriteInfo.Ptr);
				++readWriteInfo.Ptr;
			}
		}

		protected override void Write(IAnimationWeightsProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			float[] weights = item.Weights;

			for (int i = 0, count = weights.Length; i < count; ++i)
			{
				*((float*)readWriteInfo.Ptr) = weights[i];
				++readWriteInfo.Ptr;
			}
		}

		protected override void Interpolate(IAnimationWeightsProvider item, ref AnimationInterpolationInfo interpolationInfo)
		{
			float[] weights             = item.Weights;
			float[] interpolatedWeights = item.InterpolatedWeights;

			for (int i = 0, count = weights.Length; i < count; ++i)
			{
				interpolatedWeights[i] = AnimationUtility.InterpolateWeight(interpolationInfo.FromBuffer.ReinterpretState<float>(interpolationInfo.Offset), interpolationInfo.ToBuffer.ReinterpretState<float>(interpolationInfo.Offset), interpolationInfo.Alpha);
				++interpolationInfo.Offset;
			}
		}
	}
}
