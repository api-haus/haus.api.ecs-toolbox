namespace ECSToolbox.Editor
{
	using EntityGameObjectTracking;
	using Unity.Entities;

	public class EntityHostsGameObjectPrefabAuthoringBaker : Baker<EntityHostsGameObjectPrefabAuthoring>
	{
		public override void Bake(EntityHostsGameObjectPrefabAuthoring authoring)
		{
			Entity ent = GetEntity(authoring.transformUsageFlags);

			AddComponentObject(ent, new EntityHostsGameObjectPrefab { prefab = authoring.prefab });
		}
	}
}
