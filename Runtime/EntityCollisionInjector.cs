using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = UnityEngine.BoxCollider;
using CapsuleCollider = UnityEngine.CapsuleCollider;
using Collider = UnityEngine.Collider;
using Material = Unity.Physics.Material;
using MeshCollider = UnityEngine.MeshCollider;
using SphereCollider = UnityEngine.SphereCollider;
using TerrainCollider = UnityEngine.TerrainCollider;

namespace ECSToolbox.Runtime
{
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

			NativeList<CompoundCollider.ColliderBlobInstance> colliders = new(2, Allocator.Temp);

			colliders.Add(new()
			{
				CompoundFromChild = new RigidTransform(float4x4.identity), //
				Collider = CreateTerrainCollider(terrainData, filter),
			});

			if (terrainData.treeInstanceCount > 0)
			{
				colliders.Add(new()
				{
					CompoundFromChild = new RigidTransform(float4x4.identity), //
					Collider = CreateTreeCollider(terrainData, filter),
				});
			}

			physicsCollider.Value = CompoundCollider.Create(colliders.AsArray());
			foreach (var colliderBlobInstance in colliders)
			{
				colliderBlobInstance.Collider.Dispose();
			}

			return physicsCollider;
		}

		static BlobAssetReference<Unity.Physics.Collider> CreateTreeCollider(TerrainData terrainData,
			CollisionFilter filter)
		{
			NativeList<CompoundCollider.ColliderBlobInstance> treeColliders = new(terrainData.treeInstanceCount,
				Allocator.Temp);

			var prototypes = terrainData.treePrototypes;
			for (int i = 0; i < terrainData.treeInstanceCount; i++)
			{
				var tree = terrainData.treeInstances[i];
				var proto = prototypes[tree.prototypeIndex];

				var unityCollider = GetColliderFromPrefab(proto.prefab);
				// var physicsShape = proto.prefab.GetComponentInChildren<PhysicsShapeAuthoring>(); // TODO:
				var collider = CreateCollider(unityCollider, filter);

				var trs = float4x4.TRS(
					Vector3.Scale(tree.position, terrainData.size),
					quaternion.RotateY(tree.rotation),
					new float3(tree.widthScale, tree.heightScale, tree.widthScale)
				);

				var rt = new RigidTransform(trs);

				treeColliders.Add(new()
				{
					CompoundFromChild = rt, //
					Collider = collider,
					Entity = default,
				});
			}

			var treeCompound = CompoundCollider.Create(treeColliders.AsArray());

			treeColliders.Dispose();

			return treeCompound;
		}

		static BlobAssetReference<Unity.Physics.Collider> CreateCollider(Collider unityCollider, CollisionFilter filter)
		{
			if (unityCollider is CapsuleCollider cc)
			{
				return Unity.Physics.CapsuleCollider.Create(ColliderBakingUtility.CapsuleGeometry(cc, cc.transform));
			}

			if (unityCollider is BoxCollider bc)
			{
				return Unity.Physics.BoxCollider.Create(new BoxGeometry
				{
					Center = bc.center, //
					Orientation = quaternion.identity,
					Size = bc.size,
					BevelRadius = 0,
				});
			}

			if (unityCollider is SphereCollider sc)
			{
				return Unity.Physics.SphereCollider.Create(new SphereGeometry
				{
					Center = sc.center, //
					Radius = sc.radius,
				});
			}

			if (unityCollider is MeshCollider mc)
			{
				if (mc.convex)
				{
					using var mda = Mesh.AcquireReadOnlyMeshData(mc.sharedMesh);
					var md = mda[0];
					var verts = new NativeArray<Vector3>(md.vertexCount, Allocator.Temp);
					md.GetVertices(verts);
					var ccc = Unity.Physics.ConvexCollider.Create(verts.Reinterpret<float3>(),
						ConvexHullGenerationParameters.Default);
					verts.Dispose();
					return ccc;
				}

				return Unity.Physics.MeshCollider.Create(mc.sharedMesh, filter, Material.Default);
			}

			if (unityCollider is TerrainCollider tc)
			{
				return CreateTerrainCollider(tc.terrainData, filter);
			}

			throw new ArgumentOutOfRangeException(nameof(unityCollider),
				$"Unsupported collider type {unityCollider.GetType().FullName}");
		}

		static Collider GetColliderFromPrefab(GameObject protoPrefab)
		{
			return protoPrefab.GetComponentInChildren<Collider>();
		}

		static BlobAssetReference<Unity.Physics.Collider> CreateTerrainCollider(TerrainData terrainData,
			CollisionFilter filter)
		{
			var size = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution);
			var scale = terrainData.heightmapScale;

			var colliderHeights = new NativeArray<float>(terrainData.heightmapResolution * terrainData.heightmapResolution,
				Allocator.TempJob);

			var colliderHoles = new NativeArray<bool>(terrainData.holesResolution * terrainData.holesResolution,
				Allocator.TempJob);

			float[,] terrainHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution,
				terrainData.heightmapResolution);

			bool[,] terrainHoles = terrainData.GetHoles(0, 0, terrainData.holesResolution, terrainData.holesResolution);

			for (int j = 0; j < size.y; j++)
			for (int i = 0; i < size.x; i++)
			{
				colliderHeights[j + (i * size.x)] = terrainHeights[i, j];
			}

			for (int j = 0; j < size.y - 1; j++)
			for (int i = 0; i < size.x - 1; i++)
			{
				colliderHoles[j + (i * (size.x - 1))] = terrainHoles[i, j];
			}

			var c = Unity.Physics.TerrainCollider.Create(colliderHeights, colliderHoles, size, scale,
				Unity.Physics.TerrainCollider.CollisionMethod.Triangles, filter);
			colliderHeights.Dispose();
			colliderHoles.Dispose();
			return c;
		}

		LocalToWorld Convert(Transform cdTransform) => new LocalToWorld { Value = cdTransform.localToWorldMatrix };
	}
}
