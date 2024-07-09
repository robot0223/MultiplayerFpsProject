namespace Fusion.Addons.InterestManagement
{
	using System.Collections.Generic;

	/// <summary>
	/// Custom collection for tracking set of unique interest providers and some additional features.
	/// </summary>
	public sealed class InterestProviderSet
	{
		// PUBLIC MEMBERS

		public List<IInterestProvider> ReadOnlyValues => _values;

		// PRIVATE MEMBERS

		private List<IInterestProvider> _values            = new List<IInterestProvider>();
		private HashSet<int>            _hashes            = new HashSet<int>();
		private bool                    _isSorted          = false;
		private bool                    _hasOverrideIndex  = false;
		private int                     _lastOverrideIndex = -1;

		// PUBLIC METHODS

		public bool Add(IInterestProvider provider)
		{
			if (_hashes.Add(provider.GetHashCode()) == true)
			{
				_values.Add(provider);

				_isSorted         = false;
				_hasOverrideIndex = false;

				return true;
			}

			return false;
		}

		public void Clear()
		{
			_values.Clear();
			_hashes.Clear();

			_isSorted         = true;
			_hasOverrideIndex = false;
		}

		public void Sort()
		{
			if (_isSorted == true)
				return;

			InterestUtility.SortInterestProviders(_values);

			_isSorted         = true;
			_hasOverrideIndex = false;
		}

		public bool HasOverrideProvider()
		{
			return GetLastOverrideProviderIndex() >= 0;
		}

		public int GetLastOverrideProviderIndex()
		{
			if (_hasOverrideIndex == false)
			{
				_lastOverrideIndex = -1;

				for (int i = _values.Count - 1; i >= 0; --i)
				{
					if (_values[i].InterestMode == EInterestMode.Override)
					{
						_lastOverrideIndex = i;
						break;
					}
				}

				_hasOverrideIndex = true;
			}

			return _lastOverrideIndex;
		}
	}
}
