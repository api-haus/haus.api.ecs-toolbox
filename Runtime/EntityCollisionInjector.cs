namespace _0.Scripts
{
	using Unity.Collections;
	using Unity.Entities;
	using Unity.Mathematics;
	using Unity.Physics;
	using Unity.Transforms;
	using UnityEngine;
	using Collider = UnityEngine.Collider;
	using Material = Unity.Physics.Material;
	using MeshCollider = UnityEngine.MeshCollider;
	using TerrainCollider = UnityEngine.TerrainCollider;

	public class EntityCollisionInjector : MonoBehaviour
	{
		public bool disableGameObjectColliders = true;

		NativeList<Entity> myEntities;

		void OnDestroy()
		{
			if (!myEntities.IsCreated) return;

			var enMan = World.DefaultGameObjectInjectionWorld?.EntityManager;

			if (null != enMan)
			{
				foreach (var myEntity in myEntities)
				{
					enMan.Value.DestroyEntity(myEntity);
				}
			}

			myEntities.Dispose();
		}

		public void Awake()
		{
			myEntities = new NativeList<Entity>(Allocator.Persistent);
			var enMan = World.DefaultGameObjectInjectionWorld.EntityManager;
			var colliders = GetComponentsInChildren<Collider>();

			var arc = enMan.CreateArchetype(
				ComponentType.ReadOnly<PhysicsCollider>(),
				ComponentType.ReadOnly<PhysicsWorldIndex>(),
				ComponentType.ReadOnly<LocalToWorld>()
			);

			foreach (var cd in colliders)
			{
				cd.enabled = !disableGameObjectColliders;
				if (cd is MeshCollider mc && mc.sharedMesh)
				{
					var ent = enMan.CreateEntity(arc);

					enMan.SetComponentData(ent, Convert(cd.transform));
					enMan.SetComponentData(ent, Convert(mc));
					enMan.SetSharedComponent(ent, new PhysicsWorldIndex(0));
					myEntities.Add(ent);
				}

				if (cd is TerrainCollider tc)
				{
					var ent = enMan.CreateEntity(arc);

					enMan.SetComponentData(ent, Convert(cd.transform));
					enMan.SetComponentData(ent, Convert(tc));
					enMan.SetSharedComponent(ent, new PhysicsWorldIndex(0));
					myEntities.Add(ent);
				}
			}
		}

		PhysicsCollider Convert(MeshCollider mc) => Convert(mc, CollisionFilter.Default);

		PhysicsCollider Convert(MeshCollider mc, CollisionFilter filter)
		{
			var physicsCollider = new PhysicsCollider
			{
				Value = Unity.Physics.MeshCollider.Create(mc.sharedMesh, filter, Material.Default),
			};

			return physicsCollider;
		}

		public static PhysicsCollider Convert(TerrainCollider terrain) => Convert(terrain, CollisionFilter.Default);

		public static PhysicsCollider Convert(TerrainCollider terrain, CollisionFilter filter)
		{
			var terrainData = terrain.terrainData;
			var physicsCollider = new PhysicsCollider();
			var size = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution);
			var scale = terrainData.heightmapScale;

			var colliderHeights = new NativeArray<float>(terrainData.heightmapResolution * terrainData.heightmapResolution,
				Allocator.TempJob);

			float[,] terrainHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution,
				terrainData.heightmapResolution);

			bool[,] terrainHoles = terrainData.GetHoles(0, 0, terrainData.holesResolution, terrainData.holesResolution);

			for (int j = 0; j < size.y; j++)
			for (int i = 0; i < size.x; i++)
			{
				float h = terrainHeights[i, j];
				if (i < size.x - 1 && j < size.y - 1)
				{
					bool hole = terrainHoles[i, j];
					if (!hole)
					{
						h = -.01f;
					}
				}

				colliderHeights[j + (i * size.x)] = h;
			}

			physicsCollider.Value = Unity.Physics.TerrainCollider.Create(colliderHeights, size, scale,
				Unity.Physics.TerrainCollider.CollisionMethod.Triangles, filter);

			colliderHeights.Dispose();

			return physicsCollider;
		}

		LocalToWorld Convert(Transform cdTransform) => new LocalToWorld { Value = cdTransform.localToWorldMatrix };
	}
}
