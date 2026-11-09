#if UNITY_EDITOR
using Unity.Collections;
using Unity.Scenes;

namespace Unity.Entities.Editor
{
    // Removes stale subscene nodes when a scene entity leaves the subscene query
    // without being fully destroyed — e.g. entities carrying an ICleanupComponentData
    // (LiveConvertedSceneCleanup) survive DestroyEntity, so UpdateHierarchySystem's
    // EntityDiffer never emits a removal. Runs its own diff over the subscene query
    // and invokes UpdateHierarchySystem.OnRemoveEntityNodes for entities that left.
    [UnityEngine.ExecuteAlways]
    [DisableAutoCreation]
    [UpdateAfter(typeof(UpdateHierarchySystem))]
    partial class SubSceneNodeCleanupSystem : SystemBase
    {
        EntityDiffer m_Differ;
        EntityQuery m_SubSceneQuery;
        NativeList<Entity> m_Added;
        NativeList<Entity> m_Removed;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Differ = new EntityDiffer(World);
            m_SubSceneQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SubScene>()
                .WithNone<SceneSectionData>()
                .Build(EntityManager);
            m_Added = new NativeList<Entity>(Allocator.Persistent);
            m_Removed = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Differ?.Dispose();
            m_SubSceneQuery.Dispose();
            if (m_Added.IsCreated) m_Added.Dispose();
            if (m_Removed.IsCreated) m_Removed.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            m_Added.Clear();
            m_Removed.Clear();
            m_Differ.GetEntityQueryMatchDiffAsync(m_SubSceneQuery, m_Added, m_Removed).Complete();

            if (m_Removed.Length == 0)
                return;

            // EntityDiffer tracks entities per chunk. When an entity gains or loses a
            // component it moves to a different chunk, and the differ reports it as
            // removed from the old chunk and added to the new one, with the same handle.
            // Filter those pairs out — the entity is unchanged, only its archetype moved.
            FilterOutArchetypeChanges(m_Added, m_Removed);

            if (m_Removed.Length == 0)
                return;

            UpdateHierarchySystem.OnRemoveEntityNodes?.Invoke(m_Removed);
        }

        static void FilterOutArchetypeChanges(NativeList<Entity> added, NativeList<Entity> removed)
        {
            if (added.Length == 0)
                return;

            var addedSet = new NativeHashSet<Entity>(added.Length, Allocator.Temp);
            for (var i = 0; i < added.Length; i++)
                addedSet.Add(added[i]);

            for (var i = removed.Length - 1; i >= 0; i--)
            {
                if (addedSet.Contains(removed[i]))
                    removed.RemoveAtSwapBack(i);
            }

            addedSet.Dispose();
        }
    }
}
#endif
