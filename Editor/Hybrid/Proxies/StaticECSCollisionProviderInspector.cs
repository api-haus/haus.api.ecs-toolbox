namespace ECSToolbox.Editor.Hybrid.Proxies
{
	using ECSToolbox.Hybrid.Proxies;
	using UnityEditor;
	using UnityEngine;

	[CustomEditor(typeof(StaticECSCollisionProvider))]
	internal class StaticECSCollisionProviderInspector : Editor
	{
		void OnEnable() => ProxyComponentScene.EnsureSubSceneExists(target as ProxyComponentProvider<Collider>);
	}
}
