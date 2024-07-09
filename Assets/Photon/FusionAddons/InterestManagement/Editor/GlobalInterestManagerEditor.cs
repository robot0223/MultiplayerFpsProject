namespace Fusion.Addons.InterestManagement.Editor
{
	using UnityEditor;
	using Fusion.Editor;

	[CustomEditor(typeof(GlobalInterestManager), true)]
	public class GlobalInterestManagerEditor : BehaviourEditor
	{
		// PRIVATE MEMBERS

		private static InterestProviderSet _interestProviders = new InterestProviderSet();

		// Editor INTERFACE

		public override bool RequiresConstantRepaint() => true;

		public override void OnInspectorGUI()
		{
			FusionEditorGUI.InjectScriptHeaderDrawer(serializedObject);

			DrawInspectorGUI();

			GlobalInterestManager globalInterestManager = target as GlobalInterestManager;

			globalInterestManager.GetProviders(_interestProviders, true);
			InterestEditorUtility.DrawInterestProviders(_interestProviders, true);
		}

		// GlobalInterestManagerEditor INTERFACE

		protected virtual void DrawInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
}
