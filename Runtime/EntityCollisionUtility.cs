namespace ECSToolbox
{
	using System;
	using System.Linq;
	using Unity.Collections;
	using Unity.Entities;
	using Unity.Mathematics;
	using Unity.Physics;
	using UnityEngine;
	using BoxCollider = UnityEngine.BoxCollider;
	using CapsuleCollider = UnityEngine.CapsuleCollider;
	using Collider = UnityEngine.Collider;
	using Material = Unity.Physics.Material;
	using MeshCollider = UnityEngine.MeshCollider;
	using SphereCollider = UnityEngine.SphereCollider;
	using TerrainCollider = UnityEngine.TerrainCollider;

	public static class EntityCollisionUtility
	{
		public static PhysicsCollider Convert(TerrainCollider terrain) =>
			Convert(terrain, CollisionFilter.Default);

		public static PhysicsCollider Convert(TerrainCollider terrain, CollisionFilter filter)
		{
			TerrainData terrainData = terrain.terrainData;
			PhysicsCollider physicsCollider = new();

			using NativeList<CompoundCollider.ColliderBlobInstance> colliders = new(
				2,
				Allocator.Temp
			);

			try
			{
				colliders.Add(
					new()
					{
						CompoundFromChild = new RigidTransform(float4x4.identity), //
						Collider = CreateTerrainCollider(terrainData, filter),
					}
				);

				if (terrainData.treeInstanceCount > 0 && HasAnyTreeColliders(terrainData))
					colliders.Add(
						new()
						{
							CompoundFromChild = new RigidTransform(float4x4.identity),
							Collider //
							= CreateTreeCollider(terrainData, filter),
						}
					);

				physicsCollider.Value = CompoundCollider.Create(colliders.AsArray());
			}
			finally
			{
				foreach (CompoundCollider.ColliderBlobInstance colliderBlobInstance in colliders)
					colliderBlobInstance.Collider.Dispose();
			}

			return physicsCollider;
		}

		static bool HasAnyTreeColliders(TerrainData terrainData)
		{
			TreePrototype[] prototypes = terrainData.treePrototypes;

			return prototypes.Any(p => GetColliderFromPrefab(p.prefab));
		}

		static BlobAssetReference<Unity.Physics.Collider> CreateTreeCollider(
			TerrainData terrainData,
			CollisionFilter filter
		)
		{
			NativeList<CompoundCollider.ColliderBlobInstance> treeColliders = new(
				terrainData.treeInstanceCount,
				Allocator.Temp
			);

			TreePrototype[] prototypes = terrainData.treePrototypes;
			Collider[] prototypeColliders = new Collider[prototypes.Length];

			for (int i = 0; i < prototypes.Length; i++)
			{
				TreePrototype proto = prototypes[i];

				Collider unityCollider = GetColliderFromPrefab(proto.prefab);

				prototypeColliders[i] = unityCollider;
			}

			for (int i = 0; i < terrainData.treeInstanceCount; i++)
			{
				TreeInstance tree = terrainData.treeInstances[i];
				Collider unityCollider = prototypeColliders[tree.prototypeIndex];
				if (!unityCollider)
					continue;

				// var physicsShape = proto.prefab.GetComponentInChildren<PhysicsShapeAuthoring>(); // TODO:
				BlobAssetReference<Unity.Physics.Collider> collider = CreateCollider(
					unityCollider,
					filter
				);

				float4x4 trs = float4x4.TRS(
					Vector3.Scale(tree.position, terrainData.size),
					quaternion.RotateY(tree.rotation),
					new float3(tree.widthScale, tree.heightScale, tree.widthScale)
				);

				RigidTransform rt = new(trs);

				treeColliders.Add(
					new()
					{
						CompoundFromChild = rt, //
						Collider = collider,
						Entity = default,
					}
				);
			}

			BlobAssetReference<Unity.Physics.Collider> treeCompound = CompoundCollider.Create(
				treeColliders.AsArray()
			);

			treeColliders.Dispose();

			return treeCompound;
		}

		static BlobAssetReference<Unity.Physics.Collider> CreateCollider(
			Collider unityCollider,
			CollisionFilter filter
		)
		{
			if (unityCollider is CapsuleCollider cc)
				return Unity.Physics.CapsuleCollider.Create(
					ColliderBakingUtility.CapsuleGeometry(cc, cc.transform)
				);

			if (unityCollider is BoxCollider bc)
				return Unity.Physics.BoxCollider.Create(
					new BoxGeometry
					{
						Center = bc.center, //
						Orientation = quaternion.identity,
						Size = bc.size,
						BevelRadius = 0,
					}
				);

			if (unityCollider is SphereCollider sc)
				return Unity.Physics.SphereCollider.Create(
					new SphereGeometry
					{
						Center = sc.center,
						Radius //
						= sc.radius,
					}
				);

			if (unityCollider is MeshCollider mc)
			{
				if (mc.convex)
				{
					using Mesh.MeshDataArray mda = Mesh.AcquireReadOnlyMeshData(mc.sharedMesh);
					Mesh.MeshData md = mda[0];
					NativeArray<Vector3> verts = new(md.vertexCount, Allocator.Temp);
					md.GetVertices(verts);
					BlobAssetReference<Unity.Physics.Collider> ccc = ConvexCollider.Create(
						verts.Reinterpret<float3>(),
						ConvexHullGenerationParameters.Default
					);
					verts.Dispose();
					return ccc;
				}

				return Unity.Physics.MeshCollider.Create(mc.sharedMesh, filter, Material.Default);
			}

			if (unityCollider is TerrainCollider tc)
				return CreateTerrainCollider(tc.terrainData, filter);

			throw new ArgumentOutOfRangeException(
				nameof(unityCollider),
				$"Unsupported collider type {unityCollider.GetType().FullName}"
			);
		}

		static Collider GetColliderFromPrefab(GameObject protoPrefab) =>
			protoPrefab.GetComponentInChildren<Collider>();

		static BlobAssetReference<Unity.Physics.Collider> CreateTerrainCollider(
			TerrainData terrainData,
			CollisionFilter filter
		)
		{
			int2 size = new(terrainData.heightmapResolution, terrainData.heightmapResolution);
			Vector3 scale = terrainData.heightmapScale;

			NativeArray<float> colliderHeights = new(
				terrainData.heightmapResolution * terrainData.heightmapResolution,
				Allocator.TempJob
			);

			NativeArray<bool> colliderHoles = new(
				terrainData.holesResolution * terrainData.holesResolution,
				Allocator.TempJob
			);

			float[,] terrainHeights = terrainData.GetHeights(
				0,
				0,
				terrainData.heightmapResolution,
				terrainData.heightmapResolution
			);

			bool[,] terrainHoles = terrainData.GetHoles(
				0,
				0,
				terrainData.holesResolution,
				terrainData.holesResolution
			);

			for (int j = 0; j < size.y; j++)
			for (int i = 0; i < size.x; i++)
				colliderHeights[j + (i * size.x)] = terrainHeights[i, j];

			for (int j = 0; j < size.y - 1; j++)
			for (int i = 0; i < size.x - 1; i++)
				colliderHoles[j + (i * (size.x - 1))] = terrainHoles[i, j];

			BlobAssetReference<Unity.Physics.Collider> c = Unity.Physics.TerrainCollider.Create(
				colliderHeights,
				colliderHoles,
				size,
				scale,
				Unity.Physics.TerrainCollider.CollisionMethod.Triangles,
				filter
			);
			colliderHeights.Dispose();
			colliderHoles.Dispose();
			return c;
		}
	}
}
