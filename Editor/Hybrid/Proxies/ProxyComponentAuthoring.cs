namespace ECSToolbox.Editor.Hybrid.Proxies
{
	using UnityEngine;

	[AddComponentMenu("")]
	public class ProxyComponentAuthoring<T> : MonoBehaviour where T : Component
	{
		[SerializeField]
		protected internal T componentRef;

		[SerializeField]
		T[] staging;

		public void Copy()
		{
		}
	}
}
