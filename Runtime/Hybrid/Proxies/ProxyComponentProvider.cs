namespace ECSToolbox.Hybrid.Proxies
{
	using UnityEngine;

	[AddComponentMenu("")]
	public class ProxyComponentProvider<T> : MonoBehaviour where T : Component
	{
		internal virtual bool IsStatic() => false;

		internal T[] GatherComponents()
		{
			T[] componentsInChildren = GetComponentsInChildren<T>();

			return componentsInChildren;
		}
	}
}
