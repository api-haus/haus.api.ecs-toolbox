#if UNITY_EDITOR
namespace ECSToolbox.Editor.Physics
{
	using UnityEditor;
	using UnityEditor.Build;
	using UnityEditor.Build.Reporting;
	using UnityEditor.Callbacks;
	using UnityEngine;

	public class DisablePhysXOnPreprocessBuild : IPreprocessBuildWithReport
	{
		const string DYNAMICS_MANAGER_ASSET_PATH = "ProjectSettings/DynamicsManager.asset";

		public enum PhysicsBackend : uint
		{
			NONE = 3737844653,
			PHYS_X = 4072204805,
		}

		public int callbackOrder => -1;

		public void OnPreprocessBuild(BuildReport report) => SetBackEnd(PhysicsBackend.NONE);

		[PostProcessBuild(1000)]
		public static void PostprocessBuild(BuildTarget target, string pathToBuiltProject) =>
			SetBackEnd(PhysicsBackend.PHYS_X);

		public static void SetBackEnd(PhysicsBackend be)
		{
			SerializedObject dynamicsManager = new(
				AssetDatabase.LoadAllAssetsAtPath(DYNAMICS_MANAGER_ASSET_PATH)[0]
			);
			SerializedProperty currentBackendId = dynamicsManager.FindProperty(
				"m_CurrentBackendId"
			);

			currentBackendId.uintValue = (uint)be;
			dynamicsManager.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();

			Debug.Log($"Set Physics backend to {be}");
		}
	}
}
#endif
