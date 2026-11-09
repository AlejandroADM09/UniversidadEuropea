using UnityEditor;
using UnityEngine;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using System;
using System.Collections;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    abstract class WeakReferencePropertyDrawerBase : PropertyDrawer
    {
        public abstract WeakReferenceGenerationType GenerationType { get; }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var targetObjectType = DetermineTargetType(fieldInfo.FieldType);
            var gidName = $"{property.propertyPath}.Id.GlobalId.AssetGUID.Value";
            var propertyX = property.serializedObject.FindProperty($"{gidName}.x");
            var propertyY = property.serializedObject.FindProperty($"{gidName}.y");
            var propertyZ = property.serializedObject.FindProperty($"{gidName}.z");
            var propertyW = property.serializedObject.FindProperty($"{gidName}.w");
            var propertyObjectId = property.serializedObject.FindProperty($"{property.propertyPath}.Id.GlobalId.SceneObjectIdentifier0");
            var propertyIdType = property.serializedObject.FindProperty($"{property.propertyPath}.Id.GlobalId.IdentifierType");
            var genType = property.serializedObject.FindProperty($"{property.propertyPath}.Id.GenerationType");
            var uwrid = new UntypedWeakReferenceId(
                new Hash128((uint)propertyX.longValue, (uint)propertyY.longValue, (uint)propertyZ.longValue, (uint)propertyW.longValue),
                propertyObjectId.longValue,
                propertyIdType.intValue,
                GenerationType);

            var currObject = UntypedWeakReferenceId.GetEditorObject(uwrid);
            if (currObject == null && uwrid.GlobalId.AssetGUID.IsValid)
            {
                // currObject can be null if SceneObjectIdentifier0 is 0 (only AssetGUID has been set when creating the WeakAssetReference from code)
                // The drawer resolves the object through GlobalObjectId.GlobalObjectIdentifierToObjectSlow
                // GlobalObjectIdentifierToObjectSlow can't resolve an object without a local file id. So let's reload the asset at path from its GUID
                var path = AssetDatabase.GUIDToAssetPath((UnityEngine.GUID)uwrid.GlobalId.AssetGUID);
                if (!string.IsNullOrEmpty(path))
                    currObject = AssetDatabase.LoadAssetAtPath(path, targetObjectType);
            }
            var objectField = new ObjectField
            {
                objectType = targetObjectType,
                allowSceneObjects = false,
                label = property.name
            };

            objectField.SetValueWithoutNotify(currObject);
            objectField.RegisterCallback((ChangeEvent<UnityEngine.Object> e) =>
            {
                var uwr = UntypedWeakReferenceId.CreateFromObjectInstance(e.newValue);
                propertyX.longValue = uwr.GlobalId.AssetGUID.Value.x;
                propertyY.longValue = uwr.GlobalId.AssetGUID.Value.y;
                propertyZ.longValue = uwr.GlobalId.AssetGUID.Value.z;
                propertyW.longValue = uwr.GlobalId.AssetGUID.Value.w;
                propertyObjectId.longValue = uwr.GlobalId.SceneObjectIdentifier0;
                propertyIdType.intValue = uwr.GlobalId.IdentifierType;
                genType.enumValueIndex = (int)GenerationType;
                property.serializedObject.ApplyModifiedProperties();
            });
           objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
            container.Add(objectField);

            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetObjectType = DetermineTargetType(fieldInfo.FieldType);
            var gidName = $"{property.propertyPath}.Id.GlobalId.AssetGUID.Value";
            var propertyX = property.serializedObject.FindProperty($"{gidName}.x");
            var propertyY = property.serializedObject.FindProperty($"{gidName}.y");
            var propertyZ = property.serializedObject.FindProperty($"{gidName}.z");
            var propertyW = property.serializedObject.FindProperty($"{gidName}.w");
            var propertyObjectId = property.serializedObject.FindProperty($"{property.propertyPath}.Id.GlobalId.SceneObjectIdentifier0");
            var propertyIdType = property.serializedObject.FindProperty($"{property.propertyPath}.Id.GlobalId.IdentifierType");
            var genType = property.serializedObject.FindProperty($"{property.propertyPath}.Id.GenerationType");

            var uwrid = new UntypedWeakReferenceId(
                new Hash128((uint)propertyX.longValue, (uint)propertyY.longValue, (uint)propertyZ.longValue, (uint)propertyW.longValue),
                propertyObjectId.longValue,
                propertyIdType.intValue,
                GenerationType);

            var currObject = UntypedWeakReferenceId.GetEditorObject(uwrid);
            if (currObject == null && uwrid.GlobalId.AssetGUID.IsValid)
            {
                // currObject can be null if SceneObjectIdentifier0 is 0 (only AssetGUID has been set when creating the WeakAssetReference from code)
                // The drawer resolves the object through GlobalObjectId.GlobalObjectIdentifierToObjectSlow
                // GlobalObjectIdentifierToObjectSlow can't resolve an object without a local file id. So let's reload the asset at path from its GUID
                var path = AssetDatabase.GUIDToAssetPath((UnityEngine.GUID)uwrid.GlobalId.AssetGUID);
                if (!string.IsNullOrEmpty(path))
                    currObject = AssetDatabase.LoadAssetAtPath(path, targetObjectType);
            }
            EditorGUI.BeginChangeCheck();

            var newObject = EditorGUI.ObjectField(position, label, currObject, targetObjectType, false);

            if (EditorGUI.EndChangeCheck())
            {
                var uwr = UntypedWeakReferenceId.CreateFromObjectInstance(newObject);
                propertyX.longValue = uwr.GlobalId.AssetGUID.Value.x;
                propertyY.longValue = uwr.GlobalId.AssetGUID.Value.y;
                propertyZ.longValue = uwr.GlobalId.AssetGUID.Value.z;
                propertyW.longValue = uwr.GlobalId.AssetGUID.Value.w;
                propertyObjectId.longValue = uwr.GlobalId.SceneObjectIdentifier0;
                propertyIdType.intValue = uwr.GlobalId.IdentifierType;
                genType.enumValueIndex = (int)GenerationType;
                property.serializedObject.ApplyModifiedProperties();

            }
        }

        private static Type GetElementType(Type t)
        {
            if (t == typeof(WeakObjectSceneReference))
                return typeof(SceneAsset);
            if (t == typeof(EntityPrefabReference))
                return typeof(GameObject);
            if (t == typeof(EntitySceneReference))
                return typeof(SceneAsset);
            if (t.IsGenericType)
                return t.GetGenericArguments()[0];
            return t;
        }

        internal static Type DetermineTargetType(Type t)
        {
            if (typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
            {
                return GetElementType(t.GetGenericArguments()[0]);
            }
            else if (t.IsArray)
            {
                return GetElementType(t.GetElementType());
            }
            return GetElementType(t);
        }
    }

    [CustomPropertyDrawer(typeof(WeakObjectReference<>), true)]
    class WeakObjectReferencePropertyDrawer : WeakReferencePropertyDrawerBase
    {
        public override WeakReferenceGenerationType GenerationType => WeakReferenceGenerationType.UnityObject;
    }

    [CustomPropertyDrawer(typeof(WeakObjectSceneReference), true)]
    class WeakObjectSceneReferencePropertyDrawer : WeakReferencePropertyDrawerBase
    {
        public override WeakReferenceGenerationType GenerationType => WeakReferenceGenerationType.GameObjectScene;
    }

    [CustomPropertyDrawer(typeof(EntitySceneReference), true)]
    class EntitySceneReferencePropertyDrawer : WeakReferencePropertyDrawerBase
    {
        public override WeakReferenceGenerationType GenerationType => WeakReferenceGenerationType.EntityScene;
    }

    [CustomPropertyDrawer(typeof(EntityPrefabReference), true)]
    class EntityPrefabReferencePropertyDrawer : WeakReferencePropertyDrawerBase
    {
        public override WeakReferenceGenerationType GenerationType => WeakReferenceGenerationType.EntityPrefab;
    }
}
