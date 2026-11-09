using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Entities.Editor
{
    class QueryWithEntitiesViewData
    {
        const int k_DefaultMaxEntityDisplayCount = 5;

        public readonly World World;
        public readonly SystemProxy SystemProxy;
        public readonly EntityQuery Query;
        public readonly int QueryOrder;
        public readonly int MaxEntityDisplayCount;

        int m_LastVersion;

        public QueryWithEntitiesViewData(World world, EntityQuery query, SystemProxy systemProxy = default, int queryOrder = 0, int maxEntityDisplayCount = k_DefaultMaxEntityDisplayCount)
        {
            World = world;
            SystemProxy = systemProxy;
            Query = query;
            QueryOrder = queryOrder;
            MaxEntityDisplayCount = maxEntityDisplayCount;
        }

        public int TotalEntityCount { get; private set; }
        public List<EntityViewData> Entities { get; } = new List<EntityViewData>();

        public bool Update()
        {
            if (!World.IsCreated)
            {
                var count = Entities.Count;
                TotalEntityCount = 0;
                Entities.Clear();
                return count != 0;
            }

            Query.CompleteDependency();
            if (!World.EntityManager.IsQueryValid(Query))
                return false;

            var query = Query;
            // TODO(DOTS-10317): Replace this with a proper EntityQuery results hash if & when we have one
            var currentVersion = query.GetCombinedComponentOrderVersion(true);
            if (m_LastVersion == currentVersion)
                return false;

            m_LastVersion = currentVersion;
            Entities.Clear();

            using var entities = query.ToEntityArray(Allocator.Temp);
            TotalEntityCount = entities.Length;
            for (var i = 0; i < Math.Min(entities.Length, MaxEntityDisplayCount); i++)
            {
                Entities.Add(new EntityViewData(World, entities[i]));
            }

            return true;
        }
    }
}
