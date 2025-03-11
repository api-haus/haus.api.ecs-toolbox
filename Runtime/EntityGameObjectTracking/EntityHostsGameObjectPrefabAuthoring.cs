namespace ECSToolbox.EntityGameObjectTracking
{
	using Unity.Entities;
	using UnityEngine;

	public class EntityHostsGameObjectPrefabAuthoring : MonoBehaviour
	{
		public GameObject prefab;
		public TransformUsageFlags transformUsageFlags = TransformUsageFlags.Dynamic;
	}
}
