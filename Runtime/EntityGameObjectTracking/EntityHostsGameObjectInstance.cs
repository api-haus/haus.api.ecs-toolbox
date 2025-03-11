namespace ECSToolbox.EntityGameObjectTracking
{
	using Unity.Entities;
	using UnityEngine;

	public class EntityHostsGameObjectInstance : ICleanupComponentData
	{
		public Transform instance;
	}
}
