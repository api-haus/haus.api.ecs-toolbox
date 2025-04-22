namespace ECSToolbox.Editor.Hybrid.Proxies
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using ECSToolbox.Hybrid.Proxies;
	using Unity.Scenes;
	using Unity.Scenes.Editor;
	using UnityEditor;
	using UnityEditor.SceneManagement;
	using UnityEngine;
	using UnityEngine.Rendering;
	using UnityEngine.SceneManagement;
	using ComponentUtility = UnityEditorInternal.ComponentUtility;

	public static class ProxyComponentScene
	{
		// static bool s_isSaving = false;

		[InitializeOnLoadMethod]
		static void EditorInitialize() => EditorSceneManager.sceneSaving += OnSceneSaving;

		static void OnSceneSaving(Scene scene, string path)
		{
			if (scene.isSubScene || Application.isPlaying)
				return;

			Publish(scene);
			// if (s_isSaving)
			// 	return;
			// s_isSaving = true;
			//
			// EditorApplication.delayCall += () =>
			// {
			// 	Publish(scene);
			// 	s_isSaving = false;
			// };
		}

		static void Publish(Scene scene)
		{
			if (scene.isSubScene || Application.isPlaying)
				return;
			Publish<Collider>(scene);
		}

		static SubScene FindProxySubScene<T>(Scene activeScene)
			where T : Component
		{
			GameObject[] roots = activeScene.GetRootGameObjects();

			string subSceneName = $"{activeScene.name}{typeof(T).Name}_Proxy";

			SubScene subScene = roots
				.SelectMany(r => r.GetComponentsInChildren<SubScene>())
				.FirstOrDefault(s => s.SceneName == subSceneName);

			if (null == subScene)
			{
				GameObject subSceneGo = new(subSceneName);
				subScene = subSceneGo.AddComponent<SubScene>();

				string dstSceneDir = Path.Join(
					Path.GetDirectoryName(activeScene.path),
					Path.GetFileNameWithoutExtension(activeScene.path)
				);
				string dstScenePath = Path.Join(dstSceneDir, subSceneName + ".unity");
				if (!Directory.Exists(dstSceneDir))
					Directory.CreateDirectory(dstSceneDir);

				Scene scene = EditorSceneManager.NewScene(
					NewSceneSetup.EmptyScene,
					NewSceneMode.Additive
				);
				scene.name = subSceneName;

				SubSceneInspectorUtility.SetSceneAsSubScene(scene);
				EditorSceneManager.SaveScene(scene, dstScenePath);
				EditorSceneManager.CloseScene(scene, true);
				AssetDatabase.ImportAsset(dstScenePath);

				subScene.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(dstScenePath);
			}

			return subScene;
		}

		static void Publish<T>(Scene activeScene)
			where T : Component
		{
			GameObject[] roots = activeScene.GetRootGameObjects();

			ProxyComponentProvider<T>[] proxyProviders = roots
				.SelectMany(rootObject =>
					rootObject.GetComponentsInChildren<ProxyComponentProvider<T>>()
				)
				.ToArray();

			if (proxyProviders.Length == 0)
				return;

			SubScene sub = FindProxySubScene<T>(activeScene);
			SubSceneUtility.EditScene(sub);

			SceneComponentCollection<T> collection = new();

			DestroyRootGameObjectsInScene(sub.EditingScene);

			foreach (ProxyComponentProvider<T> provider in proxyProviders)
			{
				T[] components = provider
					.GatherComponents()
					.OrderBy(c => c.GetComponentIndex())
					.ToArray();

				foreach (T component in components)
					PublishProxyComponent(sub, component, collection, provider.IsStatic());
			}

			EditorSceneManager.SaveScene(sub.EditingScene);
			EditorSceneManager.CloseScene(sub.EditingScene, true);
		}

		static void DestroyRootGameObjectsInScene(Scene subEditingScene)
		{
			foreach (GameObject rootGameObject in subEditingScene.GetRootGameObjects())
				CoreUtils.Destroy(rootGameObject);
		}

		static void PublishProxyComponent<T>(
			SubScene subScene,
			T originalComponent,
			SceneComponentCollection<T> sceneComponentCollection,
			bool createStaticGameObject
		)
			where T : Component
		{
			T proxyComponent = sceneComponentCollection.CreateHostGameObjectAndAddProxyComponent(
				originalComponent,
				subScene.EditingScene,
				createStaticGameObject
			);

			ComponentProxyUtility.CopyComponentValues(originalComponent, proxyComponent);
		}

		class SceneComponentCollection<T>
			where T : Component
		{
			readonly Dictionary<int, GameObject> m_ProxyHosts = new();

			public T CreateHostGameObjectAndAddProxyComponent(
				T original,
				Scene proxyScene,
				bool createStaticGameObject = false
			)
			{
				GameObject proxyHost = GetOrCreateProxyHost(
					original,
					proxyScene,
					createStaticGameObject
				);

				T clone = proxyHost.AddComponent(original.GetType()) as T;

				return clone;
			}

			GameObject GetOrCreateProxyHost(
				T original,
				Scene proxyScene,
				bool createStaticGameObject = false
			)
			{
				if (!m_ProxyHosts.TryGetValue(Hash(original.gameObject), out GameObject proxyHost))
					proxyHost = CreateProxyHost(
						original.gameObject,
						proxyScene,
						createStaticGameObject
					);

				return proxyHost;
			}

			GameObject CreateProxyHost(
				GameObject original,
				Scene proxyScene,
				bool createStaticGameObject
			)
			{
				if (!m_ProxyHosts.TryGetValue(Hash(original), out GameObject proxyHost))
				{
					proxyHost = new(original.gameObject.name);
					SceneManager.MoveGameObjectToScene(proxyHost, proxyScene);
					ReconstructHierarchy(original, proxyHost, proxyScene, createStaticGameObject);
					m_ProxyHosts.Add(Hash(original), proxyHost);
					proxyHost.isStatic = createStaticGameObject;
				}

				return proxyHost;
			}

			void ReconstructHierarchy(
				GameObject original,
				GameObject proxy,
				Scene proxyScene,
				bool createStaticGameObject
			)
			{
				Transform originalCursor = original.transform;
				Transform proxyCursor = proxy.transform;

				if (originalCursor.parent && !proxyCursor.parent)
					proxyCursor.parent = CreateProxyHost(
						originalCursor.parent.gameObject,
						proxyScene,
						createStaticGameObject
					).transform;

				ComponentProxyUtility.CopyComponentValues(original.transform, proxy.transform);
			}

			int Hash(GameObject originalComponent) =>
				ComponentProxyUtility.HierarchyHash(originalComponent);
		}

		public static void EnsureSubSceneExists<T>(ProxyComponentProvider<T> target)
			where T : Component => FindProxySubScene<T>(target.gameObject.scene);
	}

	internal static class ComponentProxyUtility
	{
		internal static int HierarchyHash(GameObject gameObject)
		{
			int instanceHash = gameObject.GetInstanceID();

			if (gameObject.transform.parent)
				return HashCode.Combine(
					HierarchyHash(gameObject.transform.parent.gameObject),
					instanceHash
				);

			return instanceHash;
		}

		internal static void CopyComponentValues(Component original, Component proxy)
		{
			ComponentUtility.CopyComponent(original);
			ComponentUtility.PasteComponentValues(proxy);
		}
	}
}
