namespace ECSToolbox.EntityGameObjectTracking
{
	using Unity.Entities;
	using Unity.Transforms;

	[UpdateInGroup(typeof(PresentationSystemGroup))]
	[RequireMatchingQueriesForUpdate]
	internal partial class EntityGameObjectTrackingSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			foreach (
				var (entityHostsGameObjectInstance, ltw) in SystemAPI.Query<
					EntityHostsGameObjectInstance,
					LocalToWorld
				>()
			)
				entityHostsGameObjectInstance.instance.SetPositionAndRotation(
					ltw.Position,
					ltw.Rotation
				);
		}
	}
}
