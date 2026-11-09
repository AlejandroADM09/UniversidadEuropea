#if UNITY_EDITOR
using UnityEditor;

namespace Unity.Physics.Authoring
{
    [CustomEditor(typeof(PhysicsDebugDisplayAuthoring))]
    public class PhysicsDebugDisplayAuthoringEditor : Editor
    {
        const string k_DebugDisplayFoldoutKey = "Unity.Physics.DebugDisplayFoldout";
        const string k_ConstraintGraphFoldoutKey = "Unity.Physics.ConstraintGraphFoldout";
        const string k_IntegrationModeFoldoutKey = "Unity.Physics.IntegrationModeFoldout";

        bool m_DebugDisplayFoldout;
        bool m_ConstraintGraphFoldout;
        bool m_IntegrationModeFoldout;

        void OnEnable()
        {
            m_DebugDisplayFoldout = EditorPrefs.GetBool(k_DebugDisplayFoldoutKey, true);
            m_ConstraintGraphFoldout = EditorPrefs.GetBool(k_ConstraintGraphFoldoutKey, true);
            m_IntegrationModeFoldout = EditorPrefs.GetBool(k_IntegrationModeFoldoutKey, true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Debug Display Options
            EditorGUI.BeginChangeCheck();
            m_DebugDisplayFoldout = EditorGUILayout.Foldout(m_DebugDisplayFoldout, "Debug Display Options", true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_DebugDisplayFoldoutKey, m_DebugDisplayFoldout);

            if (m_DebugDisplayFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawColliders"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawColliderEdges"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawColliderAabbs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawMassProperties"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawBroadphase"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawContacts"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawCollisionEvents"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawTriggerEvents"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawJoints"));
                EditorGUI.indentLevel--;
            }

            // Constraint Graph
            EditorGUI.BeginChangeCheck();
            m_ConstraintGraphFoldout = EditorGUILayout.Foldout(m_ConstraintGraphFoldout, "Constraint Graph", true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_ConstraintGraphFoldoutKey, m_ConstraintGraphFoldout);

            if (m_ConstraintGraphFoldout)
            {
                EditorGUI.indentLevel++;

                var drawDirectSolverIslandsProp = serializedObject.FindProperty("DisplayDirectSolverIslands");
                EditorGUILayout.PropertyField(drawDirectSolverIslandsProp);

#if UNITY_PHYSICS_DISPLAY_ADVANCED_SOLVER_DATA
                using (new EditorGUI.DisabledScope(!drawDirectSolverIslandsProp.boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DisplayDirectSolverIslandsIndex"));
                }
#endif

                var drawIterativeSolverPhasesProp = serializedObject.FindProperty("DrawIterativeSolverPhases");
                EditorGUILayout.PropertyField(drawIterativeSolverPhasesProp);

#if UNITY_PHYSICS_DISPLAY_ADVANCED_SOLVER_DATA
                using (new EditorGUI.DisabledScope(!drawIterativeSolverPhasesProp.boolValue))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DrawIterativeSolverPhaseIndex"));
                }
#endif

                EditorGUI.indentLevel--;
            }

            // Integration Mode
            EditorGUI.BeginChangeCheck();
            m_IntegrationModeFoldout = EditorGUILayout.Foldout(m_IntegrationModeFoldout, "Integration Mode", true);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(k_IntegrationModeFoldoutKey, m_IntegrationModeFoldout);

            if (m_IntegrationModeFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ColliderDisplayMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ColliderEdgesDisplayMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ColliderAabbDisplayMode"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
