namespace ECSToolbox.Runtime.EntityGameObjectTracking
{
	using Unity.Entities;
	using UnityEngine;

	public class EntityHostsGameObjectPrefab : IComponentData
	{
		public GameObject Prefab;
	}
}
