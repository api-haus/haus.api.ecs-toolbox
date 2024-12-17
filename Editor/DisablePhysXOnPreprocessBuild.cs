#if UNITY_EDITOR
namespace _0.Scripts
{
	using UnityEditor;
	using UnityEditor.Build;
	using UnityEditor.Build.Reporting;
	using UnityEditor.Callbacks;
	using UnityEngine;

	public class DisablePhysXOnPreprocessBuild : IPreprocessBuildWithReport
	{
		const string DynamicsManagerAssetPath = "ProjectSettings/DynamicsManager.asset";

		public enum PhysicsBackend : uint
		{
			None = 3737844653,
			PhysX = 4072204805,
		}

		public int callbackOrder => -1;

		public void OnPreprocessBuild(BuildReport report) => SetBackEnd(PhysicsBackend.None);

		[PostProcessBuild(1000)]
		public static void PostprocessBuild(BuildTarget target, string pathToBuiltProject) =>
			SetBackEnd(PhysicsBackend.PhysX);

		public static void SetBackEnd(PhysicsBackend be)
		{
			var dynamicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(DynamicsManagerAssetPath)[0]);
			var m_CurrentBackendId = dynamicsManager.FindProperty("m_CurrentBackendId");

			m_CurrentBackendId.uintValue = (uint)be;
			dynamicsManager.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();

			Debug.Log($"Set Physics backend to {be}");
		}
	}
}
#endif
