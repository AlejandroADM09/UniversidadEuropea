#nullable enable
using NUnit.Framework;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities.Tests;
using Unity.Jobs;
using Unity.PerformanceTesting;

namespace Unity.Entities.PerformanceTests
{
    [TestFixture]
    [Category("Performance")]
    public sealed partial class ComponentLifecycleCallbacksPerformanceTests : EntityPerformanceTestFixture
    {
        [Test, Performance]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void BatchInstantiate_OnAdded_MainThread([Values(10, 1000, 100000)] int entityCount,
            [Values(typeof(EcsTestFloatData), typeof(EcsTestFloatDataWithDebugOnAdded))] System.Type type)
        {
            var componentType = ComponentType.ReadOnly(type);
            var prefabEntity = m_Manager.CreateEntity(componentType, typeof(Prefab));
            using var query = m_Manager.CreateEntityQuery(componentType);
            using var entities = CollectionHelper.CreateNativeArray<Entity>(entityCount, World.UpdateAllocator.ToAllocator);
            Measure.Method(
                    () =>
                    {
                        m_Manager.Instantiate(prefabEntity, entities);
                    })
                .WarmupCount(1)
                .MeasurementCount(100)
                .SetUp(() => { })
                .CleanUp(() => { m_Manager.DestroyEntity(query); })
                .Run();
        }

        [Test, Performance]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponentData_WithoutOnAdded([Values(1, 10, 1000)] int entityCount)
        {
            var entities = default(NativeArray<Entity>);

            Measure.Method(() =>
                {
                    for (int i = 0; i < entities.Length; i++)
                        m_Manager.AddComponentData(entities[i], new EcsTestFloatData { Value = 1.0f });
                })
                .SetUp(() =>
                {
                    entities = new NativeArray<Entity>(entityCount, Allocator.Persistent);
                    for (int i = 0; i < entityCount; i++)
                        entities[i] = m_Manager.CreateEntity();
                })
                .CleanUp(() =>
                {
                    m_Manager.DestroyEntity(entities);
                    entities.Dispose();
                })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();
        }

        [Test, Performance]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void AddComponentData_WithOnAdded([Values(1, 10, 1000)] int entityCount)
        {
            var entities = default(NativeArray<Entity>);

            Measure.Method(() =>
                {
                    for (int i = 0; i < entities.Length; i++)
                        m_Manager.AddComponentData(entities[i], new EcsTestFloatDataWithDebugOnAdded { Value0 = 1.0f });
                })
                .SetUp(() =>
                {
                    entities = new NativeArray<Entity>(entityCount, Allocator.Persistent);
                    for (int i = 0; i < entityCount; i++)
                        entities[i] = m_Manager.CreateEntity();
                })
                .CleanUp(() =>
                {
                    m_Manager.DestroyEntity(entities);
                    entities.Dispose();
                })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();
        }

        [Test, Performance]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void RemoveComponent_WithoutOnRemoved([Values(1, 10, 1000)] int entityCount)
        {
            var componentType = ComponentType.ReadWrite<EcsTestFloatData>();
            var entities = default(NativeArray<Entity>);

            Measure.Method(() =>
                {
                    for (int i = 0; i < entities.Length; i++)
                        m_Manager.RemoveComponent(entities[i], componentType);
                })
                .SetUp(() =>
                {
                    entities = new NativeArray<Entity>(entityCount, Allocator.Persistent);
                    for (int i = 0; i < entityCount; i++)
                        entities[i] = m_Manager.CreateEntity(componentType);
                })
                .CleanUp(() =>
                {
                    m_Manager.DestroyEntity(entities);
                    entities.Dispose();
                })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();
        }

        [Test, Performance]
        [TestRequiresDotsDebugOrCollectionChecks("Component lifecycle callbacks only fire in debug/collection-checks builds")]
        public void RemoveComponent_WithOnRemoved([Values(1, 10, 1000)] int entityCount)
        {
            var componentType = ComponentType.ReadWrite<EcsTestFloatDataWithDebugOnRemoved>();
            var entities = default(NativeArray<Entity>);

            Measure.Method(() =>
                {
                    for (int i = 0; i < entities.Length; i++)
                        m_Manager.RemoveComponent(entities[i], componentType);
                })
                .SetUp(() =>
                {
                    entities = new NativeArray<Entity>(entityCount, Allocator.Persistent);
                    for (int i = 0; i < entityCount; i++)
                        entities[i] = m_Manager.CreateEntity(componentType);
                })
                .CleanUp(() =>
                {
                    m_Manager.DestroyEntity(entities);
                    entities.Dispose();
                })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();
        }
    }
}
