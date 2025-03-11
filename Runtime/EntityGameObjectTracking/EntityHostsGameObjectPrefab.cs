namespace ECSToolbox.EntityGameObjectTracking
{
	using Unity.Entities;
	using UnityEngine;

	public class EntityHostsGameObjectPrefab : IComponentData
	{
		public GameObject prefab;
	}
}
