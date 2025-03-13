namespace ECSToolbox.EntityGameObjectTracking
{
	using Unity.Entities;
	using UnityEngine;

	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	internal partial class SpawnEntityGameObjectPrefabs : SystemBase
	{
		protected override void OnUpdate()
		{
			EntityCommandBuffer ecb = World
				.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>()
				.CreateCommandBuffer();
			foreach (
				var (pref, entity) in SystemAPI
					.Query<EntityHostsGameObjectPrefab>()
					.WithNone<EntityHostsGameObjectInstance>()
					.WithEntityAccess()
			)
				ecb.AddComponent(
					entity,
					new EntityHostsGameObjectInstance()
					{
						instance = Object.Instantiate(pref.prefab).transform,
					}
				);

			foreach (
				var (inst, entity) in SystemAPI
					.Query<EntityHostsGameObjectInstance>()
					.WithNone<EntityHostsGameObjectPrefab>()
					.WithEntityAccess()
			)
			{
				Object.Destroy(inst.instance);
				ecb.RemoveComponent<EntityHostsGameObjectInstance>(entity);
			}
		}
	}
}
