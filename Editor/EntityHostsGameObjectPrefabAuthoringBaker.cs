namespace ECSToolbox.Editor
{
	using Runtime;
	using Runtime.EntityGameObjectTracking;
	using Unity.Entities;

	public class EntityHostsGameObjectPrefabAuthoringBaker : Baker<EntityHostsGameObjectPrefabAuthoring>
	{
		public override void Bake(EntityHostsGameObjectPrefabAuthoring authoring)
		{
			var ent = GetEntity(authoring.transformUsageFlags);

			AddComponentObject(ent, new EntityHostsGameObjectPrefab { Prefab = authoring.prefab, });
		}
	}
}
