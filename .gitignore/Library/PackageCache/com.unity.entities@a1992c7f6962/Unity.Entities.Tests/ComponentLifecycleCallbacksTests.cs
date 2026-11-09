using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Tests
{
    internal struct EcsTestDataWithTrackedOnAdded : IComponentData, IDebugOnAdded
    {
        public int Value;

        static Entity s_LastEntity;
        static int s_LastValue;
        static int s_CallCount;
        static readonly Dictionary<Entity, int> s_ValueByEntity = new Dictionary<Entity, int>();

        public static void OnAdded(Entity entity, in EcsTestDataWithTrackedOnAdded component)
        {
            s_LastEntity = entity;
            s_LastValue = component.Value;
            s_CallCount++;
            s_ValueByEntity[entity] = component.Value;
        }

        public static void Reset()
        {
            s_LastEntity = Entity.Null;
            s_LastValue = 0;
            s_CallCount = 0;
            s_ValueByEntity.Clear();
        }

        public static Entity LastEntity => s_LastEntity;
        public static int LastValue => s_LastValue;
        public static int CallCount => s_CallCount;
        public static IReadOnlyDictionary<Entity, int> ValueByEntity => s_ValueByEntity;
    }

    internal struct EcsIntElementWithTrackedOnAdded : IBufferElementData, IDebugOnAdded
    {
        public int Value;

        static Entity s_LastEntity;
        static int s_CallCount;

        // The 'component' parameter for a buffer type refers to the buffer header, not an element,
        // so only the entity and the call count are recorded.
        public static void OnAdded(Entity entity, in EcsIntElementWithTrackedOnAdded component)
        {
            s_LastEntity = entity;
            s_CallCount++;
        }

        public static void Reset()
        {
            s_LastEntity = Entity.Null;
            s_CallCount = 0;
        }

        public static Entity LastEntity => s_LastEntity;
        public static int CallCount => s_CallCount;
    }

    internal struct EcsTestDataWithTrackedOnRemoved : IComponentData, IDebugOnRemoved
    {
        public int Value;

        static Entity s_LastEntity;
        static int s_LastValue;
        static int s_CallCount;

        public static void OnRemoved(Entity entity, in EcsTestDataWithTrackedOnRemoved component)
        {
            s_LastEntity = entity;
            s_LastValue = component.Value;
            s_CallCount++;
        }

        public static void Reset()
        {
            s_LastEntity = Entity.Null;
            s_LastValue = 0;
            s_CallCount = 0;
        }

        public static Entity LastEntity => s_LastEntity;
        public static int LastValue => s_LastValue;
        public static int CallCount => s_CallCount;
    }

    [TestFixture]
    public class ComponentLifecycleCallbacksTests : ECSTestsFixture
    {
        [SetUp]
        public void TestSetUp()
        {
            EcsTestDataWithTrackedOnAdded.Reset();
            EcsTestDataWithTrackedOnRemoved.Reset();
            EcsIntElementWithTrackedOnAdded.Reset();
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponentData_OnAddedCallback_ReceivesCorrectValue()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnAdded { Value = 42 });

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(entity, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(42, EcsTestDataWithTrackedOnAdded.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponent_OnAddedCallback_ReceivesZeroedValue()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent<EcsTestDataWithTrackedOnAdded>(entity);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(entity, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(0, EcsTestDataWithTrackedOnAdded.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponentData_ExistingComponent_NoAdditionalCallback()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnAdded { Value = 1 });
            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);

            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnAdded { Value = 2 });
            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void RemoveComponent_OnRemovedCallback_ReceivesCurrentValue()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnRemoved { Value = 7 });

            m_Manager.RemoveComponent<EcsTestDataWithTrackedOnRemoved>(entity);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnRemoved.CallCount);
            Assert.AreEqual(entity, EcsTestDataWithTrackedOnRemoved.LastEntity);
            Assert.AreEqual(7, EcsTestDataWithTrackedOnRemoved.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void RemoveComponent_EntityDoesNotHaveComponent_NoCallback()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.RemoveComponent<EcsTestDataWithTrackedOnRemoved>(entity);

            Assert.AreEqual(0, EcsTestDataWithTrackedOnRemoved.CallCount);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void DestroyEntity_OnRemovedCallback_ReceivesCurrentValue()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnRemoved { Value = 99 });

            m_Manager.DestroyEntity(entity);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnRemoved.CallCount);
            Assert.AreEqual(99, EcsTestDataWithTrackedOnRemoved.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_AddComponent_OnAddedCallback_ReceivesCorrectValue()
        {
            var entity = m_Manager.CreateEntity();
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.AddComponent(entity, new EcsTestDataWithTrackedOnAdded { Value = 55 });
            ecb.Playback(m_Manager);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(entity, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(55, EcsTestDataWithTrackedOnAdded.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_AddComponent_ExistingComponent_NoAdditionalCallback()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnAdded { Value = 1 });
            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.AddComponent(entity, new EcsTestDataWithTrackedOnAdded { Value = 2 });
            ecb.Playback(m_Manager);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_AddComponentForMultipleEntities_OnAddedCallback_ReceivesCorrectValue()
        {
            var entity = m_Manager.CreateEntity();
            using var entities = new NativeArray<Entity>(new[] { entity }, Allocator.Temp);
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.AddComponent(entities, new EcsTestDataWithTrackedOnAdded { Value = 77 });
            ecb.Playback(m_Manager);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(entity, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(77, EcsTestDataWithTrackedOnAdded.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_AddComponentForMultipleEntities_MixedExisting_OnAddedCallback_ReceivesCorrectValues()
        {
            // Use enough entities to take the CreateEntityBatchList playback path, with a mix of entities
            // that already have the component (no callback, value assigned) and entities that don't.
            const int entityCount = 32;
            var entities = new NativeArray<Entity>(entityCount, Allocator.Temp);
            for (int i = 0; i < entityCount; i++)
                entities[i] = m_Manager.CreateEntity();
            for (int i = 0; i < entityCount; i += 4)
                m_Manager.AddComponentData(entities[i], new EcsTestDataWithTrackedOnAdded { Value = 1000 + i });
            EcsTestDataWithTrackedOnAdded.Reset();

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.AddComponent(entities, new EcsTestDataWithTrackedOnAdded { Value = 77 });
            ecb.Playback(m_Manager);

            Assert.AreEqual(entityCount - entityCount / 4, EcsTestDataWithTrackedOnAdded.CallCount);
            for (int i = 0; i < entityCount; i++)
            {
                Assert.AreEqual(77, m_Manager.GetComponentData<EcsTestDataWithTrackedOnAdded>(entities[i]).Value);
                if (i % 4 == 0)
                {
                    Assert.IsFalse(EcsTestDataWithTrackedOnAdded.ValueByEntity.ContainsKey(entities[i]));
                }
                else
                {
                    Assert.IsTrue(EcsTestDataWithTrackedOnAdded.ValueByEntity.TryGetValue(entities[i], out var value));
                    Assert.AreEqual(77, value);
                }
            }
            entities.Dispose();
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponentData_Query_NativeArray_OnAddedCallback_ReceivesCorrectValues()
        {
            const int entityCount = 8;
            var entities = new Entity[entityCount];
            for (int i = 0; i < entityCount; i++)
                entities[i] = m_Manager.CreateEntity(typeof(EcsTestData));
            // One entity already has the component; it should get the new value but no callback.
            m_Manager.AddComponentData(entities[3], new EcsTestDataWithTrackedOnAdded { Value = 1000 });
            EcsTestDataWithTrackedOnAdded.Reset();

            using var query = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            using var queryEntities = query.ToEntityArray(Allocator.Temp);
            var values = new NativeArray<EcsTestDataWithTrackedOnAdded>(queryEntities.Length, Allocator.Temp);
            for (int i = 0; i < queryEntities.Length; i++)
                values[i] = new EcsTestDataWithTrackedOnAdded { Value = 10 * (i + 1) };

            m_Manager.AddComponentData(query, values);

            Assert.AreEqual(entityCount - 1, EcsTestDataWithTrackedOnAdded.CallCount);
            for (int i = 0; i < queryEntities.Length; i++)
            {
                var expected = 10 * (i + 1);
                Assert.AreEqual(expected, m_Manager.GetComponentData<EcsTestDataWithTrackedOnAdded>(queryEntities[i]).Value);
                if (queryEntities[i] == entities[3])
                {
                    Assert.IsFalse(EcsTestDataWithTrackedOnAdded.ValueByEntity.ContainsKey(queryEntities[i]));
                }
                else
                {
                    Assert.IsTrue(EcsTestDataWithTrackedOnAdded.ValueByEntity.TryGetValue(queryEntities[i], out var value));
                    Assert.AreEqual(expected, value);
                }
            }
            values.Dispose();
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_AddBuffer_NoCallbackSupportForBuffers_BufferContentsIntact()
        {
            // TypeManager only registers IDebugOnAdded callbacks for IComponentData types, so no
            // callback fires for dynamic buffers. This test pins that behavior and verifies the
            // AddBuffer playback still transfers the recorded buffer contents to the entity.
            var entity = m_Manager.CreateEntity();
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            var buffer = ecb.AddBuffer<EcsIntElementWithTrackedOnAdded>(entity);
            buffer.Add(new EcsIntElementWithTrackedOnAdded { Value = 5 });
            buffer.Add(new EcsIntElementWithTrackedOnAdded { Value = 6 });
            ecb.Playback(m_Manager);

            Assert.AreEqual(0, EcsIntElementWithTrackedOnAdded.CallCount);
            var playedBack = m_Manager.GetBuffer<EcsIntElementWithTrackedOnAdded>(entity);
            Assert.AreEqual(2, playedBack.Length);
            Assert.AreEqual(5, playedBack[0].Value);
            Assert.AreEqual(6, playedBack[1].Value);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_AddComponentForLinkedEntityGroup_OnAddedCallback_ReceivesCorrectValue()
        {
            var parent = m_Manager.CreateEntity(typeof(LinkedEntityGroup));
            var matchingChild = m_Manager.CreateEntity(typeof(EcsTestData));
            var nonMatchingChild = m_Manager.CreateEntity();
            var group = m_Manager.GetBuffer<LinkedEntityGroup>(parent);
            group.Add(parent);
            group.Add(matchingChild);
            group.Add(nonMatchingChild);

            using var maskQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            var mask = maskQuery.GetEntityQueryMask();

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ecb.AddComponentForLinkedEntityGroup(parent, mask, new EcsTestDataWithTrackedOnAdded { Value = 11 });
            ecb.Playback(m_Manager);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(matchingChild, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(11, EcsTestDataWithTrackedOnAdded.LastValue);
            Assert.IsFalse(m_Manager.HasComponent<EcsTestDataWithTrackedOnAdded>(nonMatchingChild));
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void Instantiate_OnAddedCallback_FiresForClonedEntity()
        {
            var prefab = m_Manager.CreateEntity();
            m_Manager.AddComponentData(prefab, new EcsTestDataWithTrackedOnAdded { Value = 10 });
            EcsTestDataWithTrackedOnAdded.Reset();

            var instance = m_Manager.Instantiate(prefab);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(instance, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(10, EcsTestDataWithTrackedOnAdded.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_Instantiate_OnAddedCallback_FiresForClonedEntity()
        {
            var prefab = m_Manager.CreateEntity();
            m_Manager.AddComponentData(prefab, new EcsTestDataWithTrackedOnAdded { Value = 10 });
            EcsTestDataWithTrackedOnAdded.Reset();

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbEntity = ecb.Instantiate(prefab);
            ecb.Playback(m_Manager);

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(10, EcsTestDataWithTrackedOnAdded.LastValue);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void ECB_Instantiate_Multiple_OnAddedCallback_FiresForEachClone()
        {
            var prefab = m_Manager.CreateEntity();
            m_Manager.AddComponentData(prefab, new EcsTestDataWithTrackedOnAdded { Value = 25 });
            EcsTestDataWithTrackedOnAdded.Reset();

            const int count = 4;
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (int i = 0; i < count; i++)
                ecb.Instantiate(prefab);
            ecb.Playback(m_Manager);

            Assert.AreEqual(count, EcsTestDataWithTrackedOnAdded.CallCount);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponentData_DestroyedEntity_Throws_LaterCallbacksStillFire()
        {
            var destroyed = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(destroyed);
            Assert.Catch(() => m_Manager.AddComponentData(destroyed, new EcsTestDataWithTrackedOnAdded { Value = 1 }));

            // A failed add must not disable the callbacks for later structural changes.
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestDataWithTrackedOnAdded { Value = 42 });

            Assert.AreEqual(1, EcsTestDataWithTrackedOnAdded.CallCount);
            Assert.AreEqual(entity, EcsTestDataWithTrackedOnAdded.LastEntity);
            Assert.AreEqual(42, EcsTestDataWithTrackedOnAdded.LastValue);
        }
    }
}
