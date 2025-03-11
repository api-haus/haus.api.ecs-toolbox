namespace ECSToolbox.Editor.Hybrid.Physics
{
	using Unity.Entities;
	using Unity.Physics;
	using TerrainCollider = UnityEngine.TerrainCollider;

	public class TerrainColliderBaker : Baker<TerrainCollider>
	{
		public override void Bake(TerrainCollider authoring)
		{
			Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

			AddComponent(entity, EntityCollisionUtility.Convert(authoring));
			AddSharedComponent(entity, new PhysicsWorldIndex { Value = 0 });
		}
	}
}
