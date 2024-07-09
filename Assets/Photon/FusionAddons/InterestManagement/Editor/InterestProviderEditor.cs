namespace Fusion.Addons.InterestManagement.Editor
{
	using UnityEditor;
	using Fusion.Editor;

	[CustomEditor(typeof(InterestProvider), true)]
	public class InterestProviderEditor : BehaviourEditor
	{
		// PRIVATE MEMBERS

		private static InterestProviderSet _interestProviders = new InterestProviderSet();

		// Editor INTERFACE

		public override bool RequiresConstantRepaint() => true;

		public override void OnInspectorGUI()
		{
			FusionEditorGUI.InjectScriptHeaderDrawer(serializedObject);

			DrawInspectorGUI();

			InterestProvider interestProvider = target as InterestProvider;

			_interestProviders.Clear();
			interestProvider.GetProviders(_interestProviders, false);
			InterestEditorUtility.DrawInterestProviders(_interestProviders, true);
		}

		// InterestProviderEditor INTERFACE

		protected virtual void DrawInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
}
