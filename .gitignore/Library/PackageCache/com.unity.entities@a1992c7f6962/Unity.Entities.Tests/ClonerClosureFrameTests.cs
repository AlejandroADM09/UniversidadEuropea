using System;
using NUnit.Framework;

namespace Unity.Entities.Tests
{
    // The Cloner ILPP swaps a user-written system method's body for the source-generated one and then removes the
    // closure frames ("display classes") that only the replaced body used, so Mono does not end up with two sets of
    // sequence points on one source location. Removing the wrong set leaves IL pointing at types that are no longer
    // in the assembly: Mono can limp along, but IL2CPP conversion cannot, which is how UUM-86534 was reported
    // ("error C2039: '___entity': is not a member of 'BlobBuilder_t...'").
    //
    // The systems below are as much a compile-time test as a runtime one. They are patched by the source generators
    // and then processed by the Cloner, so a regression fails IL2CPP conversion of this assembly in the
    // build_playmode_entities_* (Il2Cpp) jobs. The assertions cover the Mono side of the same systems.
    public partial class ClonerClosureFrameTests : ECSTestsFixture
    {
        public struct ClosureResult : IComponentData
        {
            public int Value;
        }

        // Three chained frames: "total" belongs to the method scope, "captured" to the loop scope, and "doubled" to a
        // frame that is only ever allocated inside the outer lambda, so the method body never names it. That is the
        // UUM-86534 shape - there it was a BlobBuilder inside a Task.Run inside a SystemAPI.Query loop.
        partial struct NestedClosureFramesSystem : ISystem
        {
            public void OnCreate(ref SystemState state) =>
                state.EntityManager.AddComponent<ClosureResult>(state.SystemHandle);

            public void OnUpdate(ref SystemState state)
            {
                var total = 0;

                foreach (var data in SystemAPI.Query<RefRO<EcsTestData>>())
                {
                    var captured = data.ValueRO.value;

                    Action accumulate = () =>
                    {
                        var doubled = captured * 2;
                        Action apply = () => total += doubled;
                        apply();
                    };
                    accumulate();
                }

                state.EntityManager.SetComponentData(state.SystemHandle, new ClosureResult { Value = total });
            }
        }

        // "Add" is only ever reached through "AddTwice", so following the call from the method body alone does not
        // find it. Because both take an argument of their own, Roslyn puts the captured frame in their last
        // parameter rather than their first.
        partial struct TransitiveLocalFunctionSystem : ISystem
        {
            public void OnCreate(ref SystemState state) =>
                state.EntityManager.AddComponent<ClosureResult>(state.SystemHandle);

            public void OnUpdate(ref SystemState state)
            {
                var total = 0;

                void AddTwice(int amount)
                {
                    Add(amount);
                    Add(amount);
                }

                void Add(int amount) => total += amount;

                foreach (var data in SystemAPI.Query<RefRO<EcsTestData>>())
                    AddTwice(data.ValueRO.value);

                state.EntityManager.SetComponentData(state.SystemHandle, new ClosureResult { Value = total });
            }
        }

        [Test]
        public void SystemWithNestedCapturingScopes_StillRunsAfterPatching()
        {
            CreateEntitiesWithValues(1, 2, 3);

            var system = World.GetOrCreateSystem<NestedClosureFramesSystem>();
            system.Update(World.Unmanaged);

            Assert.AreEqual(12, m_Manager.GetComponentData<ClosureResult>(system).Value);
        }

        [Test]
        public void SystemWithTransitiveLocalFunctions_StillRunsAfterPatching()
        {
            CreateEntitiesWithValues(1, 2, 3);

            var system = World.GetOrCreateSystem<TransitiveLocalFunctionSystem>();
            system.Update(World.Unmanaged);

            Assert.AreEqual(12, m_Manager.GetComponentData<ClosureResult>(system).Value);
        }

        void CreateEntitiesWithValues(params int[] values)
        {
            foreach (var value in values)
                m_Manager.SetComponentData(m_Manager.CreateEntity(typeof(EcsTestData)), new EcsTestData(value));
        }
    }
}
