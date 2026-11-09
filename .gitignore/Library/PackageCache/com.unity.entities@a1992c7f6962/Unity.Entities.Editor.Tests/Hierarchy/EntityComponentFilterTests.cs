using NUnit.Framework;
using Unity.Collections;
using Unity.Transforms;
using Unity.Hierarchy;

namespace Unity.Entities.Editor.Tests
{
    public class EntityComponentFilterTests
    {
        World m_World;
        EntityManager m_EntityManager;

        [SetUp]
        public void SetUp()
        {
            m_World = new World("TestWorld");
            m_EntityManager = m_World.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_World != null && m_World.IsCreated)
                m_World.Dispose();
        }

        [Test]
        public void ResolveComponentType_ValidComponentType_ResolvesSuccessfully()
        {
            var filter = new EntityComponentFilter();
            var query = CreateQuery("LocalToWorld");

            filter.SetQuery(query);

            Assert.That(filter.IsValid, Is.True, "Filter should be valid for known component type LocalToWorld");
        }

        [Test]
        public void ResolveComponentType_InvalidComponentType_MarksFilterInvalid()
        {
            var filter = new EntityComponentFilter();
            var query = CreateQuery("NonExistentComponent");

            filter.SetQuery(query);

            Assert.That(filter.IsValid, Is.False, "Filter should be invalid for unknown component type");
        }

        [Test]
        public void IsMatch_EntityWithRequestedComponent_ReturnsTrue()
        {
            var entity = m_EntityManager.CreateEntity(typeof(LocalToWorld));
            var filter = new EntityComponentFilter();
            var query = CreateQuery("LocalToWorld");

            filter.SetQuery(query);

            Assume.That(filter.IsValid, Is.True, "Filter should be valid before testing IsMatch");
            Assert.That(filter.IsMatch(entity, m_World.Unmanaged), Is.True,
                "Entity with LocalToWorld component should match filter");
        }

        [Test]
        public void IsMatch_EntityWithoutRequestedComponent_ReturnsFalse()
        {
            var entity = m_EntityManager.CreateEntity(typeof(LocalTransform));
            var filter = new EntityComponentFilter();
            var query = CreateQuery("LocalToWorld");

            filter.SetQuery(query);

            Assume.That(filter.IsValid, Is.True, "Filter should be valid before testing IsMatch");
            Assert.That(filter.IsMatch(entity, m_World.Unmanaged), Is.False,
                "Entity without LocalToWorld component should not match filter");
        }

        [Test]
        [Description("Regression test for UUM-142749")]
        public void SetQuery_DuringExclusiveEntityTransaction_DoesNotThrow()
        {
            m_EntityManager.CreateEntity(typeof(LocalToWorld));
            m_EntityManager.BeginExclusiveEntityTransaction();

            var filter = new EntityComponentFilter();
            var query = CreateQuery("LocalToWorld");

            Assert.DoesNotThrow(() => filter.SetQuery(query));

            filter.Dispose();
        }

        HierarchySearchQueryDescriptor CreateQuery(params string[] componentTypeNames)
        {
            var filters = new HierarchySearchFilter[componentTypeNames.Length];
            for (int i = 0; i < componentTypeNames.Length; i++)
            {
                filters[i] = new HierarchySearchFilter
                {
                    Name = "t",
                    Value = componentTypeNames[i],
                    Op = HierarchySearchFilterOperator.Equal
                };
            }
            return new HierarchySearchQueryDescriptor(filters);
        }
    }
}
