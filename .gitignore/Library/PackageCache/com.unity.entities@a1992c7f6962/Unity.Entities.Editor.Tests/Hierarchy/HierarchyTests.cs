using NUnit.Framework;
using Unity.Collections;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Editor.Tests
{
    sealed class HierarchyTests
    {
        World m_World;

        Hierarchy m_Hierarchy;

        [SetUp]
        public void SetUp()
        {
            m_World = new World("Hierarchy World");
            m_Hierarchy = new Hierarchy(m_World, Allocator.Persistent, DataMode.Disabled)
            {
                Configuration = new HierarchyConfiguration
                {
                    UpdateMode = Hierarchy.UpdateModeType.Synchronous
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            m_World.Dispose();
            m_World = null;

            m_Hierarchy.Dispose();
            m_Hierarchy = null;
        }

        [Test]
        public void Update_WhenParentIsNull_DoesNotThrow()
        {
            m_World.EntityManager.CreateEntity(ComponentType.ReadOnly<Parent>());

            Assert.DoesNotThrow(() =>
            {
                m_Hierarchy.Update(true);
            });
        }

        [Test]
        public void SetWorldAndUpdate_InExclusiveTransaction_DoesNotThrow()
        {
            using var world = new World("Transaction World");
            world.EntityManager.BeginExclusiveEntityTransaction();
            Assert.DoesNotThrow(() =>
            {
                m_Hierarchy.SetWorld(world);
                m_Hierarchy.Update(true);
            });
        }

        [Test]
        [Description("Regression test for UUM-152665")]
        public void SelectHierarchyNode_EntityWithAuthoringObject_SelectsEntity()
        {
            var authoring = new GameObject(nameof(SelectHierarchyNode_EntityWithAuthoringObject_SelectsEntity));
            try
            {
                var entity = m_World.EntityManager.CreateEntity();
                m_World.EntityManager.AddComponentData(entity, new EntityGuid(authoring.GetEntityId(), EntityId.None, 0, 0));
                Assume.That(m_World.EntityManager.Debug.GetAuthoringObjectForEntity(entity), Is.EqualTo(authoring));

                HierarchyWindow.SelectHierarchyNode(m_Hierarchy, HierarchyNodeHandle.FromEntity(entity), DataMode.Disabled);

                Assert.That(Selection.activeObject, Is.TypeOf<EntitySelectionProxy>());
                Assert.That(((EntitySelectionProxy)Selection.activeObject).Entity, Is.EqualTo(entity));
                Assert.That(Selection.activeContext, Is.Null);
            }
            finally
            {
                Selection.activeObject = null;
                Object.DestroyImmediate(authoring);
            }
        }
    }
}
