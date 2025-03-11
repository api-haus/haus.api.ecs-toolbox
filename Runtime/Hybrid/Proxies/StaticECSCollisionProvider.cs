namespace ECSToolbox.Hybrid.Proxies
{
	using UnityEngine;

	public class StaticECSCollisionProvider : ProxyComponentProvider<Collider>
	{
		internal override bool IsStatic() => true;
	}
}
