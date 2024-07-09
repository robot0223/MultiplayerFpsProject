namespace Fusion.Addons.AnimationController
{
	using System.Collections.Generic;

	public unsafe interface IAnimationPropertyProvider
	{
		int WordCount { get; }

		void Initialize(AnimationController controller);
		void Deinitialize();
		void Read(AnimationReadWriteInfo readWriteInfo);
		void Write(AnimationReadWriteInfo readWriteInfo);
		void Interpolate(ref AnimationInterpolationInfo interpolationInfo);
	}

	/// <summary>
	/// AnimationPropertyProvider handles synchronization and interpolation of animation properties.
	/// Animation properties are provided by type T which can be implemented by AnimationController, AnimationLayer or AnimationState.
	/// T can be implemented only once by a single controller/layer/state type.
	/// All animation property providers must be registered in <c>AnimationController.AddAnimationPropertyProviders()</c>.
	/// </summary>
	public abstract unsafe class AnimationPropertyProvider<T> : IAnimationPropertyProvider where T : class
	{
		// PRIVATE MEMBERS

		private T[] _items;
		private int _wordCount;

		// IAnimationPropertyProvider INTERFACE

		int IAnimationPropertyProvider.WordCount => _wordCount;

		void IAnimationPropertyProvider.Initialize(AnimationController controller)
		{
			List<T> items = new List<T>();

			AddController(controller, items);

			_items = items.ToArray();

			_wordCount = default;

			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				_wordCount += GetWordCount(_items[i]);
			}

			OnInitialize(controller);
		}

		void IAnimationPropertyProvider.Deinitialize()
		{
			OnDeinitialize();

			_items     = default;
			_wordCount = default;
		}

		void IAnimationPropertyProvider.Read(AnimationReadWriteInfo readWriteInfo)
		{
			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				Read(_items[i], readWriteInfo);
			}
		}

		void IAnimationPropertyProvider.Write(AnimationReadWriteInfo readWriteInfo)
		{
			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				Write(_items[i], readWriteInfo);
			}
		}

		void IAnimationPropertyProvider.Interpolate(ref AnimationInterpolationInfo interpolationInfo)
		{
			for (int i = 0, count = _items.Length; i < count; ++i)
			{
				Interpolate(_items[i], ref interpolationInfo);
			}
		}

		// PROTECTED METHODS

		protected abstract int  GetWordCount(T item);
		protected abstract void Read(T item, AnimationReadWriteInfo info);
		protected abstract void Write(T item, AnimationReadWriteInfo info);
		protected abstract void Interpolate(T item, ref AnimationInterpolationInfo interpolationInfo);

		protected virtual void OnInitialize(AnimationController controller) {}
		protected virtual void OnDeinitialize()                             {}

		// PRIVATE METHODS

		private void AddController(AnimationController controller, List<T> items)
		{
			if (controller is T item)
			{
				items.Add(item);
			}

			IList<AnimationLayer> layers = controller.Layers;
			for (int i = 0, count = layers.Count; i < count; ++i)
			{
				AddLayer(layers[i], items);
			}
		}

		private void AddLayer(AnimationLayer layer, List<T> items)
		{
			if (layer is T item)
			{
				items.Add(item);
			}

			IList<AnimationState> states = layer.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddState(states[i], items);
			}
		}

		private void AddState(AnimationState state, List<T> items)
		{
			if (state is T item)
			{
				items.Add(item);
			}

			IList<AnimationState> states = state.States;
			for (int i = 0, count = states.Count; i < count; ++i)
			{
				AddState(states[i], items);
			}
		}
	}
}
