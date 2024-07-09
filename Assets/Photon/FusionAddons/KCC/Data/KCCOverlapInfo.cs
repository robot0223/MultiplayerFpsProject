namespace Fusion.Addons.KCC
{
	using UnityEngine;

	public sealed class KCCOverlapInfo
	{
		// PUBLIC MEMBERS

		public Vector3                 Position;
		public float                   Radius;
		public float                   Height;
		public float                   Extent;
		public LayerMask               LayerMask;
		public QueryTriggerInteraction TriggerInteraction;
		public KCCOverlapHit[]         AllHits;
		public int                     AllHitCount;
		public KCCOverlapHit[]         ColliderHits;
		public int                     ColliderHitCount;
		public KCCOverlapHit[]         TriggerHits;
		public int                     TriggerHitCount;

		// CONSTRUCTORS

		public KCCOverlapInfo() : this(KCC.CACHE_SIZE)
		{
		}

		public KCCOverlapInfo(int maxHits)
		{
			AllHits      = new KCCOverlapHit[maxHits];
			TriggerHits  = new KCCOverlapHit[maxHits];
			ColliderHits = new KCCOverlapHit[maxHits];

			for (int i = 0; i < maxHits; ++i)
			{
				AllHits[i] = new KCCOverlapHit();
			}
		}

		// PUBLIC METHODS

		public bool HasCollider(Collider collider)
		{
			for (int i = 0, count = AllHitCount; i < count; ++i)
			{
				KCCOverlapHit hit = AllHits[i];
				if (object.ReferenceEquals(hit.Collider, collider) == true)
					return true;
			}

			return false;
		}

		public bool HasCollider(EColliderType colliderType)
		{
			for (int i = 0, count = AllHitCount; i < count; ++i)
			{
				KCCOverlapHit hit = AllHits[i];
				if (hit.Type == colliderType)
					return true;
			}

			return false;
		}

		public bool HasColliderWithinExtent(Collider collider)
		{
			for (int i = 0, count = AllHitCount; i < count; ++i)
			{
				KCCOverlapHit hit = AllHits[i];
				if (object.ReferenceEquals(hit.Collider, collider) == true && hit.IsWithinExtent == true)
					return true;
			}

			return false;
		}

		public bool HasColliderWithinExtent(EColliderType colliderType)
		{
			for (int i = 0, count = AllHitCount; i < count; ++i)
			{
				KCCOverlapHit hit = AllHits[i];
				if (hit.Type == colliderType && hit.IsWithinExtent == true)
					return true;
			}

			return false;
		}

		public void AddHit(Collider collider)
		{
			if (AllHitCount == AllHits.Length)
				return;

			KCCOverlapHit hit = AllHits[AllHitCount];
			if (hit.Set(collider) == true)
			{
				++AllHitCount;

				if (hit.IsTrigger == true)
				{
					TriggerHits[TriggerHitCount] = hit;
					++TriggerHitCount;
				}
				else
				{
					ColliderHits[ColliderHitCount] = hit;
					++ColliderHitCount;
				}
			}
		}

		public void ToggleConvexMeshColliders(bool convex)
		{
			KCCOverlapHit hit;

			for (int i = 0; i < ColliderHitCount; ++i)
			{
				hit = ColliderHits[i];

				if (hit.Type == EColliderType.Mesh && hit.IsConvertible == true)
				{
					((MeshCollider)hit.Collider).convex = convex;
				}
			}
		}

		public bool AllHitsWithinExtent()
		{
			KCCOverlapHit[] hits = AllHits;
			for (int i = 0, count = AllHitCount; i < count; ++i)
			{
				if (AllHits[i].IsWithinExtent == false)
					return false;
			}

			return true;
		}

		public void Reset(bool deep)
		{
			Position           = default;
			Radius             = default;
			Height             = default;
			Extent             = default;
			LayerMask          = default;
			TriggerInteraction = QueryTriggerInteraction.Collide;
			AllHitCount        = default;
			TriggerHitCount    = default;
			ColliderHitCount   = default;

			if (deep == true)
			{
				for (int i = 0, count = AllHits.Length; i < count; ++i)
				{
					AllHits[i].Reset();
				}
			}
		}

		public void CopyFromOther(KCCOverlapInfo other)
		{
			Position           = other.Position;
			Radius             = other.Radius;
			Height             = other.Height;
			Extent             = other.Extent;
			LayerMask          = other.LayerMask;
			TriggerInteraction = other.TriggerInteraction;
			AllHitCount        = other.AllHitCount;
			TriggerHitCount    = default;
			ColliderHitCount   = default;

			KCCOverlapHit hit;

			for (int i = 0; i < AllHitCount; ++i)
			{
				hit = AllHits[i];

				hit.CopyFromOther(other.AllHits[i]);

				if (hit.IsTrigger == true)
				{
					TriggerHits[TriggerHitCount] = hit;
					++TriggerHitCount;
				}
				else
				{
					ColliderHits[ColliderHitCount] = hit;
					++ColliderHitCount;
				}
			}
		}

		public void DumpHits(KCC kcc)
		{
			if (AllHitCount <= 0)
				return;

			kcc.Log($"Overlap Hits ({AllHitCount})");

			KCCOverlapHit[] hits = AllHits;
			for (int i = 0, count = AllHitCount; i < count; ++i)
			{
				KCCOverlapHit hit = AllHits[i];
				kcc.Log($"Collider: {hit.Collider.name}, Type: {hit.Type}, IsTrigger: {hit.IsTrigger}, IsConvex: {hit.IsConvex}, IsWithinExtent: {hit.IsWithinExtent}, HasPenetration: {hit.HasPenetration}, CollisionType: {hit.CollisionType}");
			}
		}
	}
}
