namespace ECSToolbox.Runtime.EntityGameObjectTracking
{
	using Unity.Entities;
	using UnityEngine;

	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	partial class SpawnEntityGameObjectPrefabs : SystemBase
	{
		protected override void OnUpdate()
		{
			var ecb = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>().CreateCommandBuffer();
			foreach (var (pref, entity) in SystemAPI.Query<EntityHostsGameObjectPrefab>()
				         .WithNone<EntityHostsGameObjectInstance>().WithEntityAccess())
			{
				ecb.AddComponent(entity,
					new EntityHostsGameObjectInstance() { Instance = Object.Instantiate(pref.Prefab).transform, });
			}

			foreach (var (inst, entity) in SystemAPI.Query<EntityHostsGameObjectInstance>()
				         .WithNone<EntityHostsGameObjectPrefab>().WithEntityAccess())
			{
				Object.Destroy(inst.Instance);
				ecb.RemoveComponent<EntityHostsGameObjectInstance>(entity);
			}
		}
	}
}
