namespace Fusion.Addons.AnimationController
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using Unity.Collections.LowLevel.Unsafe;

	public unsafe sealed class AnimationReadWriteInfo
	{
	    public int* Ptr;
	}

	public ref struct AnimationInterpolationInfo
	{
	    public NetworkBehaviourBuffer FromBuffer;
	    public NetworkBehaviourBuffer ToBuffer;
	    public int                    Offset;
	    public float                  Alpha;
	}

	public unsafe partial class AnimationController
	{
		// PRIVATE MEMBERS

		private AnimationProperiesInfo[]     _animationProperties;
		private IAnimationPropertyProvider[] _animationPropertyProviders;

		private static readonly AnimationReadWriteInfo _readWriteInfo = new AnimationReadWriteInfo();

		// PROTECTED METHODS

		protected virtual void AddAnimationPropertyProviders(List<IAnimationPropertyProvider> animationPropertyProviders) {}

		// PRIVATE METHODS

		private int GetNetworkDataWordCount()
		{
			InitializeLayers();
			InitializeNetworkProperties();

			int wordCount = 0;

			AnimationProperiesInfo animationProperty;
			for (int i = 0, count = _animationProperties.Length; i < count; ++i)
			{
				animationProperty = _animationProperties[i];
				for (int j = 0; j < animationProperty.Count; ++j)
				{
					wordCount += animationProperty.WordCounts[j];
				}
			}

			for (int i = 0, count = _animationPropertyProviders.Length; i < count; ++i)
			{
				wordCount += _animationPropertyProviders[i].WordCount;
			}

			return wordCount;
		}

		private unsafe void ReadNetworkData()
		{
			fixed (int* statePtr = &ReinterpretState<int>())
			{
				_readWriteInfo.Ptr = statePtr;

				AnimationProperiesInfo   animationProperty;
				AnimationProperiesInfo[] animationProperties = _animationProperties;
				for (int i = 0, count = animationProperties.Length; i < count; ++i)
				{
					animationProperty = animationProperties[i];

					byte* objectPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(animationProperty.Target, out ulong gcHandle);

					for (int j = 0; j < animationProperty.Count; ++j)
					{
						int  wordCount   = animationProperty.WordCounts[j];
						int* propertyPtr = (int*)(objectPtr + animationProperty.FieldOffsets[j]);

						for (int n = 0; n < wordCount; ++n)
						{
							*propertyPtr = *_readWriteInfo.Ptr;

							++_readWriteInfo.Ptr;
							++propertyPtr;
						}
					}

					UnsafeUtility.ReleaseGCObject(gcHandle);
				}

				IAnimationPropertyProvider   animationPropertyProvider;
				IAnimationPropertyProvider[] animationPropertyProviders = _animationPropertyProviders;
				for (int i = 0, count = animationPropertyProviders.Length; i < count; ++i)
				{
					animationPropertyProvider = animationPropertyProviders[i];
					animationPropertyProvider.Read(_readWriteInfo);
				}
			}
		}

		private unsafe void WriteNetworkData()
		{
			fixed (int* statePtr = &ReinterpretState<int>())
			{
				_readWriteInfo.Ptr = statePtr;

				AnimationProperiesInfo   animationProperty;
				AnimationProperiesInfo[] animationProperties = _animationProperties;
				for (int i = 0, count = animationProperties.Length; i < count; ++i)
				{
					animationProperty = animationProperties[i];

					byte* objectPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(animationProperty.Target, out ulong gcHandle);

					for (int j = 0; j < animationProperty.Count; ++j)
					{
						int  wordCount   = animationProperty.WordCounts[j];
						int* propertyPtr = (int*)(objectPtr + animationProperty.FieldOffsets[j]);

						for (int n = 0; n < wordCount; ++n)
						{
							*_readWriteInfo.Ptr = *propertyPtr;

							++_readWriteInfo.Ptr;
							++propertyPtr;
						}
					}

					UnsafeUtility.ReleaseGCObject(gcHandle);
				}

				IAnimationPropertyProvider   animationPropertyProvider;
				IAnimationPropertyProvider[] animationPropertyProviders = _animationPropertyProviders;
				for (int i = 0, count = animationPropertyProviders.Length; i < count; ++i)
				{
					animationPropertyProvider = animationPropertyProviders[i];
					animationPropertyProvider.Write(_readWriteInfo);
				}
			}
		}

		private unsafe void InterpolateNetworkData()
		{
			bool buffersValid = TryGetSnapshotsBuffers(out NetworkBehaviourBuffer fromBuffer, out NetworkBehaviourBuffer toBuffer, out float alpha);
			if (buffersValid == false)
				return;

			AnimationInterpolationInfo interpolationInfo = new AnimationInterpolationInfo();
			interpolationInfo.FromBuffer = fromBuffer;
			interpolationInfo.ToBuffer   = toBuffer;
			interpolationInfo.Alpha      = alpha;

			int   ticks = interpolationInfo.ToBuffer.Tick - interpolationInfo.FromBuffer.Tick;
			float tick  = interpolationInfo.FromBuffer.Tick + interpolationInfo.Alpha * ticks;

			AnimationProperiesInfo   animationProperty;
			AnimationProperiesInfo[] animationProperties = _animationProperties;
			for (int i = 0, count = animationProperties.Length; i < count; ++i)
			{
				animationProperty = animationProperties[i];

				for (int j = 0; j < animationProperty.Count; ++j)
				{
					int wordCount    = animationProperty.WordCounts[j];
					int targetOffset = interpolationInfo.Offset + wordCount;

					InterpolationDelegate interpolationDelegate = animationProperty.InterpolationDelegates[j];
					if (interpolationDelegate != null)
					{
						interpolationDelegate(interpolationInfo);
					}

					interpolationInfo.Offset = targetOffset;
				}
			}

			IAnimationPropertyProvider   animationPropertyProvider;
			IAnimationPropertyProvider[] animationPropertyProviders = _animationPropertyProviders;
			for (int i = 0, count = animationPropertyProviders.Length; i < count; ++i)
			{
				animationPropertyProvider = animationPropertyProviders[i];
				animationPropertyProvider.Interpolate(ref interpolationInfo);
			}
		}

		private void InitializeNetworkProperties()
		{
			if (_animationProperties != default)
				return;

			_animationProperties = GetAnimationProperties();

			List<IAnimationPropertyProvider> animationPropertyProviders = new List<IAnimationPropertyProvider>();
			animationPropertyProviders.Add(new AnimationTimeProvider());
			animationPropertyProviders.Add(new AnimationWeightProvider());
			animationPropertyProviders.Add(new AnimationFadingProvider());
			animationPropertyProviders.Add(new AnimationClipIDProvider());
			animationPropertyProviders.Add(new AnimationWeightsProvider());

			AddAnimationPropertyProviders(animationPropertyProviders);

			for (int i = 0, count = animationPropertyProviders.Count; i < count; ++i)
			{
				animationPropertyProviders[i].Initialize(this);
			}

			_animationPropertyProviders = animationPropertyProviders.ToArray();
		}

		private void DeinitializeNetworkProperties()
		{
			if (_animationPropertyProviders != default)
			{
				for (int i = 0, count = _animationPropertyProviders.Length; i < count; ++i)
				{
					_animationPropertyProviders[i].Deinitialize();
				}
			}

			_animationProperties        = default;
			_animationPropertyProviders = default;
		}

		private AnimationProperiesInfo[] GetAnimationProperties()
		{
			List<AnimationProperiesInfo> properties = new List<AnimationProperiesInfo>();

			AddTargetProperties(this, properties);

			IList<AnimationLayer> layers = _layers;
			for (int i = 0, count = layers.Count; i < count; ++i)
			{
				AddLayerProperties(layers[i], properties);
			}

			return properties.ToArray();
		}

		private static void AddLayerProperties(AnimationLayer layer, List<AnimationProperiesInfo> properties)
		{
			AddTargetProperties(layer, properties);

			IList<AnimationState> states = layer.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddStateProperties(states[i], properties);
			}
		}

		private static void AddStateProperties(AnimationState state, List<AnimationProperiesInfo> properties)
		{
			AddTargetProperties(state, properties);

			IList<AnimationState> states = state.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddStateProperties(states[i], properties);
			}
		}

		private static void AddTargetProperties(object target, List<AnimationProperiesInfo> properties)
		{
			bool                        hasProperties          = false;
			List<int>                   wordCounts             = default;
			List<int>                   fieldOffsets           = default;
			List<InterpolationDelegate> interpolationDelegates = default;

			FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; ++i)
			{
				FieldInfo field = fields[i];

				object[] attributes = field.GetCustomAttributes(typeof(AnimationPropertyAttribute), false);
				if (attributes.Length > 0)
				{
					if (field.FieldType.IsValueType == false)
					{
						throw new NotSupportedException(field.FieldType.FullName);
					}

					if (hasProperties == false)
					{
						hasProperties          = true;
						wordCounts             = new List<int>(8);
						fieldOffsets           = new List<int>(8);
						interpolationDelegates = new List<InterpolationDelegate>(8);
					}

					InterpolationDelegate interpolationDelegate = null;

					string interpolationDelegateName = ((AnimationPropertyAttribute)attributes[0]).InterpolationDelegate;
					if (string.IsNullOrEmpty(interpolationDelegateName) == false)
					{
						MethodInfo interpolationMethod = target.GetType().GetMethod(interpolationDelegateName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (interpolationMethod == null)
						{
							throw new ArgumentException($"Missing interpolation method {interpolationDelegateName}!");
						}

						interpolationDelegate = interpolationMethod.CreateDelegate(typeof(InterpolationDelegate), target) as InterpolationDelegate;
						if (interpolationMethod == null)
						{
							throw new ArgumentException($"Couldn't create delegate for interpolation method {interpolationDelegateName}!");
						}
					}

					wordCounts.Add(GetTypeWordCount(field.FieldType));
					fieldOffsets.Add(UnsafeUtility.GetFieldOffset(field));
					interpolationDelegates.Add(interpolationDelegate);
				}
			}

			if (hasProperties == true)
			{
				AnimationProperiesInfo animationObject = new AnimationProperiesInfo();
				animationObject.Count                  = fieldOffsets.Count;
				animationObject.Target                 = target;
				animationObject.WordCounts             = wordCounts.ToArray();
				animationObject.FieldOffsets           = fieldOffsets.ToArray();
				animationObject.InterpolationDelegates = interpolationDelegates.ToArray();

				properties.Add(animationObject);
			}
		}

		private static int GetTypeWordCount(Type type)
		{
			return (Marshal.SizeOf(type) + 3) / 4;
		}

		private sealed class AnimationProperiesInfo
		{
			public int                     Count;
			public object                  Target;
			public int[]                   WordCounts;
			public int[]                   FieldOffsets;
			public InterpolationDelegate[] InterpolationDelegates;
		}
	}
}
