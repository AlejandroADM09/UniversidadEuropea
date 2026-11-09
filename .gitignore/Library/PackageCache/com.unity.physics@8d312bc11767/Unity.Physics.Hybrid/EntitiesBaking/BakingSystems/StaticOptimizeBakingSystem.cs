using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    /// <summary>
    /// Marks a primary entity as a static root when building the compound colliders.
    /// </summary>
    [TemporaryBakingType]
    struct StaticOptimizePhysicsBaking : IComponentData {}

    /// <summary>
    /// Component added on additional entities in bakers to mark the static root found during the baking of a collider.
    /// </summary>
    /// <remarks>
    /// Multiple bakers may find the same static root body. The system <see cref="StaticOptimizeBakingSystem"/>
    /// adds the component <see cref="StaticOptimizePhysicsBaking"/> to the static root primary entity.
    /// </remarks>
    [BakingType]
    struct BakeStaticRoot : IComponentData
    {
        public Entity Body;
        public EntityId ConvertedBodyEntityId;
        public float4x4 BodyLocalToWorld;
        public float3 BodyLossyScale;
    }

    [BurstCompile]
    [UpdateBefore(typeof(BuildCompoundCollidersBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct StaticOptimizeBakingSystem : ISystem
    {
        EntityQuery _ChangedBakeStaticRootQuery;
        EntityQuery _PreviousBakeStaticRootQuery;
        ComponentTypeSet _RootComponents;
        ComponentTypeSet _TransformComponents;
        NativeHashSet<Entity> _StaticRootState; // Holds the set of static roots baked in a previous iteration.

        [BurstCompile]
        public void OnCreate(ref SystemState systemState)
        {
            _StaticRootState = new NativeHashSet<Entity>(10, Allocator.Persistent);

            _PreviousBakeStaticRootQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BakeStaticRoot>()
                .WithNone<BakedEntity>()
                .Build(ref systemState);

            _ChangedBakeStaticRootQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BakeStaticRoot, BakedEntity>()
                .Build(ref systemState);

            _RootComponents = new ComponentTypeSet(
                ComponentType.ReadWrite<StaticOptimizePhysicsBaking>(),
                ComponentType.ReadWrite<PhysicsWorldIndex>(),
                ComponentType.ReadWrite<PhysicsCompoundData>(),
                ComponentType.ReadWrite<PhysicsCollider>());

            _TransformComponents = new ComponentTypeSet(
                ComponentType.ReadWrite<LocalToWorld>(),
                ComponentType.ReadWrite<LocalTransform>());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState systemState)
        {
            _StaticRootState.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState systemState)
        {
            var previousStaticRoots = _PreviousBakeStaticRootQuery.ToComponentDataArray<BakeStaticRoot>(Allocator.Temp);
            var changedStaticRoots = _ChangedBakeStaticRootQuery.ToComponentDataArray<BakeStaticRoot>(Allocator.Temp);

            var capacity = math.max(previousStaticRoots.Length, changedStaticRoots.Length);
            var uniqueRoots = new NativeHashMap<Entity, BakeStaticRoot>(capacity, Allocator.Temp);

            // clear the root components from roots that are no longer needed
            GetUniqueRoots(previousStaticRoots, ref uniqueRoots);
            var oldState = _StaticRootState.ToNativeArray(Allocator.Temp);
            for (int i = 0, count = oldState.Length; i < count; ++i)
            {
                var r = oldState[i];
                if (!uniqueRoots.ContainsKey(r))
                {
                    systemState.EntityManager.RemoveComponent(r, _RootComponents);
                    systemState.EntityManager.RemoveComponent(r, _TransformComponents);

                    _StaticRootState.Remove(r);
                }
            }

            // add the root components on the new static roots
            uniqueRoots.Clear();
            GetUniqueRoots(changedStaticRoots, ref uniqueRoots);
            foreach (var kv in uniqueRoots)
            {
                var rootEntity = kv.Value.Body;
                _StaticRootState.Add(rootEntity);
                systemState.EntityManager.AddComponent(rootEntity, _RootComponents);

                systemState.EntityManager.SetSharedComponent(rootEntity, new PhysicsWorldIndex());

                systemState.EntityManager.SetComponentData(rootEntity, new PhysicsCompoundData()
                {
                    AssociateBlobToBody = false,
                    ConvertedBodyEntityId = kv.Value.ConvertedBodyEntityId,
                    Hash = default,
                });

                SetupStaticRootTransform(ref systemState, rootEntity, kv.Value);
            }
        }

        void SetupStaticRootTransform(ref SystemState systemState, Entity rootEntity, in BakeStaticRoot staticRoot)
        {
            systemState.EntityManager.AddComponent(rootEntity, _TransformComponents);

            var bodyL2W = staticRoot.BodyLocalToWorld;
            var rigidBodyTransform = Math.DecomposeRigidBodyTransform(bodyL2W);

            systemState.EntityManager.SetComponentData(rootEntity, new LocalToWorld { Value = bodyL2W });

            var uniformScale = 1.0f;
            if (bodyL2W.HasShear() || bodyL2W.HasNonUniformScale())
            {
                var compositeScale = math.mul(math.inverse(new float4x4(rigidBodyTransform)), bodyL2W);
                if (!systemState.EntityManager.HasComponent<PostTransformMatrix>(rootEntity))
                    systemState.EntityManager.AddComponent<PostTransformMatrix>(rootEntity);
                systemState.EntityManager.SetComponentData(rootEntity, new PostTransformMatrix { Value = compositeScale });
            }
            else
            {
                uniformScale = math.abs(staticRoot.BodyLossyScale.x);
            }

            systemState.EntityManager.SetComponentData(rootEntity,
                LocalTransform.FromPositionRotationScale(rigidBodyTransform.pos, rigidBodyTransform.rot, uniformScale));
        }

        [BurstCompile]
        static void GetUniqueRoots(in NativeArray<BakeStaticRoot> rootMarkers, ref NativeHashMap<Entity, BakeStaticRoot> bodyRoots)
        {
            for (int i = 0, count = rootMarkers.Length; i < count; ++i)
            {
                var bakedStaticRoot = rootMarkers[i];
                var rootEntity = bakedStaticRoot.Body;
                if (bodyRoots.ContainsKey(rootEntity))
                    continue;

                bodyRoots.Add(rootEntity, bakedStaticRoot);
            }
        }
    }
}
