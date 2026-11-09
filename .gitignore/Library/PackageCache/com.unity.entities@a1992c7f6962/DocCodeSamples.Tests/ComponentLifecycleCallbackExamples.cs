using Unity.Entities;
using UnityEngine;

namespace Doc.CodeSamples.Tests
{
    #region lifecycle-callbacks
    public struct ExampleLifecycleComponent : IComponentData, IDebugOnAdded, IDebugOnRemoved
    {
        public int Value;

        public static void OnAdded(Entity entity, in ExampleLifecycleComponent component)
        {
            // Set a breakpoint on the following line to inspect the call stack.
            Debug.Log($"Added ExampleLifecycleComponent to entity {entity.Index}");
        }

        public static void OnRemoved(Entity entity, in ExampleLifecycleComponent component)
        {
            Debug.Log($"Removed ExampleLifecycleComponent from entity {entity.Index}");
        }
    }
    #endregion

    #region lifecycle-callbacks-single
    public struct ExampleAddedOnlyComponent : IComponentData, IDebugOnAdded
    {
        public int Value;

        public static void OnAdded(Entity entity, in ExampleAddedOnlyComponent component)
        {
            Debug.Log($"Added ExampleAddedOnlyComponent to entity {entity.Index}");
        }
    }
    #endregion
}
