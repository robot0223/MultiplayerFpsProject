namespace Fusion.Addons.AnimationController
{
	public unsafe interface IAnimationClipIDProvider
	{
		int ClipID { get; set; }
	}

	public unsafe sealed class AnimationClipIDProvider : AnimationPropertyProvider<IAnimationClipIDProvider>
	{
		// AnimationPropertyProvider INTERFACE

		protected override int GetWordCount(IAnimationClipIDProvider item)
		{
			return 1;
		}

		protected override void Read(IAnimationClipIDProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			item.ClipID = *readWriteInfo.Ptr;
			++readWriteInfo.Ptr;
		}

		protected override void Write(IAnimationClipIDProvider item, AnimationReadWriteInfo readWriteInfo)
		{
			*readWriteInfo.Ptr = item.ClipID;
			++readWriteInfo.Ptr;
		}

		protected override void Interpolate(IAnimationClipIDProvider item, ref AnimationInterpolationInfo interpolationInfo)
		{
			item.ClipID = interpolationInfo.Alpha < 0.5f ? interpolationInfo.FromBuffer.ReinterpretState<int>(interpolationInfo.Offset) : interpolationInfo.ToBuffer.ReinterpretState<int>(interpolationInfo.Offset);
			++interpolationInfo.Offset;
		}
	}
}
