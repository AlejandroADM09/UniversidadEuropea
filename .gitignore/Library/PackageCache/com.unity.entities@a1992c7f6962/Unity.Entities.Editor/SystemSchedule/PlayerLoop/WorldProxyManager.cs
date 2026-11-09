using System;
using System.Collections.Generic;

namespace Unity.Entities.Editor
{
    internal class WorldProxyManager : IDisposable
    {
        readonly Dictionary<World, WorldProxyUpdater> m_Updaters = new();
        readonly List<World> m_WorldsToRemove = new();

        bool m_UpdateAllWorlds;

        public void SetUpdateAllWorlds(bool value)
        {
            if (m_UpdateAllWorlds == value)
                return;
            m_UpdateAllWorlds = value;
            ApplyActiveState();
        }

        WorldProxy m_SelectedWorldProxy;

        public WorldProxy GetWorldProxyForGivenWorld(World world)
        {
            if (world == null || !world.IsCreated)
                throw new ArgumentNullException(nameof(world));

            if (m_Updaters.TryGetValue(world, out var updater))
                return updater.Proxy;

            throw new ArgumentException($"WorldProxy for given world {world.Name} does not exist or is null");
        }

        public bool TryGetWorldProxy(World world, out WorldProxy proxy)
        {
            proxy = null;
            if (world == null || !world.IsCreated)
                return false;

            if (m_Updaters.TryGetValue(world, out var updater))
            {
                proxy = updater.Proxy;
                return true;
            }
            return false;
        }

        public IEnumerable<WorldProxyUpdater> GetAllWorldProxyUpdaters() => m_Updaters.Values;

        public void SetSelectedWorldProxy(WorldProxy proxy)
        {
            if (m_SelectedWorldProxy != null && m_SelectedWorldProxy.Equals(proxy))
                return;

            m_SelectedWorldProxy = proxy;
            ApplyActiveState();
        }

        void ApplyActiveState()
        {
            foreach (var updater in m_Updaters.Values)
            {
                if (m_UpdateAllWorlds || (m_SelectedWorldProxy != null && updater.Proxy.Equals(m_SelectedWorldProxy)))
                    updater.EnableUpdater();
                else
                    updater.DisableUpdater();
            }
        }        

        public void CreateWorldProxiesForAllWorlds()
        {
            foreach (var world in World.All)
            {
                if (m_Updaters.ContainsKey(world))
                    continue;

                CreateWorldProxy(world);
            }

            CleanUpWorldProxyDictionary();
        }

        public void RebuildWorldProxyForGivenWorld(World world)
        {
            CleanUpWorldProxyDictionary();

            if (m_Updaters.TryGetValue(world, out var updater))
                updater.ResetWorldProxy();
            else
                CreateWorldProxy(world);
        }

        void CreateWorldProxy(World world)
        {
            if (world == null || !world.IsCreated)
                throw new ArgumentNullException(nameof(world));

            var mainFlag = WorldCategoryHelper.GetMainFlag(world);
            if (mainFlag == WorldFlags.None || !Enum.IsDefined(typeof(HierarchyWorldFilter), (int)mainFlag))
                return;

            if (m_Updaters.ContainsKey(world))
                return;

            var worldProxy = new WorldProxy(world.SequenceNumber);
            var updater = new WorldProxyUpdater(world, worldProxy);
            updater.PopulateWorldProxy();
            if (m_UpdateAllWorlds)
                updater.EnableUpdater();

            m_Updaters.Add(world, updater);
        }

        void CleanUpWorldProxyDictionary()
        {
            m_WorldsToRemove.Clear();

            foreach (var (world, updater) in m_Updaters)
            {
                if (!world.IsCreated)
                {
                    updater.DisableUpdater();
                    m_WorldsToRemove.Add(world);
                }
            }

            foreach (var world in m_WorldsToRemove)
                m_Updaters.Remove(world);

            m_WorldsToRemove.Clear();
        }

        public void Dispose()
        {
            foreach (var updater in m_Updaters.Values)
                updater.DisableUpdater();
            m_Updaters.Clear();
        }
    }
}
