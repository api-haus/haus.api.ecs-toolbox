namespace ECSToolbox.Runtime.EntityGameObjectTracking
{
	using Unity.Entities;
	using Unity.Transforms;

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	partial class EntityGameObjectTrackingSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			foreach (var (entityHostsGameObjectInstance, ltw) in SystemAPI
				         .Query<EntityHostsGameObjectInstance, LocalToWorld>())
			{
				entityHostsGameObjectInstance.Instance.SetPositionAndRotation(ltw.Position, ltw.Rotation);
			}
		}
	}
}
