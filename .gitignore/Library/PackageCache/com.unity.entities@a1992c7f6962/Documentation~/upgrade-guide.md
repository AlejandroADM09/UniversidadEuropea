# Upgrade the Entities package

This page describes how to upgrade from an older version of the Entities package to version 6.6.0.

Starting with Unity 6.4, the Entities package is a [core package](https://docs.unity3d.com/Manual/pack-core.html) and is distributed alongside each version of the Unity Editor. The version of the package follows the Unity release (for example, version 6.6.0 in Unity 6.6) instead of the previous 1.x versioning scheme.

The changes in this guide are grouped by the version you upgrade from, with the most recent versions first. If you upgrade across multiple Unity versions, apply all the sections that cover the versions between your current version and the new one. For example, if you upgrade a project from Entities 1.3, apply every section on this page.

Some deprecated APIs produce console warnings that contain enough instructions to update the code. This page covers only the changes that require more significant updates to your project.

## Upgrading from Unity 6.5

Unity 6.6 introduces the following changes:

* [Convert managed components to unmanaged components](#convert-managed-components-to-unmanaged-components)
* [Change EntityCommandBuffer PlaybackPolicy code](#change-entitycommandbuffer-playbackpolicy-code)
* [Update code that relies on placeholder entities](#update-code-that-relies-on-placeholder-entities)
* [Migrate PostLoadCommandBuffer to RequestSceneLoaded.ImportEntity](#migrate-postloadcommandbuffer-to-requestsceneloadedimportentity)
* [Replace SubScene.HierarchyColor](#replace-subscenehierarchycolor)
* [Entities Hierarchy window is deprecated](#entities-hierarchy-window-is-deprecated)

### Convert managed components to unmanaged components

Managed components (`IComponentData`) and managed shared components (`ISharedComponentData`) are deprecated in Unity 6.6 and will be removed in a future version. A component or shared component is managed if it's a `class`, or a `struct` that contains managed (reference-type) fields such as `string` or other classes.

When you open a project that uses managed components in Unity 6.6:

* APIs that work with managed component types produce obsolete API warnings.
* Shared components that are structs with managed fields produce the `EA0017` analyzer warning. To temporarily silence this warning while you migrate, define the `UNITY_DISABLE_MANAGED_SHARED_COMPONENT_WARNINGS` [scripting symbol](https://docs.unity3d.com/6000.6/Documentation/Manual/scripting-symbol-reference.html).

To convert a managed component to an unmanaged one, declare it as a `struct` that contains only unmanaged fields:

* Replace references to `UnityEngine.Object` instances (such as materials, meshes, or `ScriptableObject` assets) with [`UnityObjectRef<T>`](xref:Unity.Entities.UnityObjectRef`1). In Unity 6.6, accessing an object through `UnityObjectRef<T>` is significantly faster than in previous versions, which makes it a suitable replacement for a managed reference. For more information, refer to [Reference Unity objects in your code](reference-unity-objects.md).
* Replace `string` fields with one of the `Unity.Collections.FixedString` types.
* Replace managed collections, such as arrays and lists, with a [dynamic buffer](components-buffer.md) or a [blob asset](blob-assets-intro.md).

Some managed types, such as `AnimationCurve`, have no unmanaged equivalent. To keep such data on an entity without a managed component, store it in a `ScriptableObject` instance that you create at bake time, and reference that instance from an unmanaged component. To do this:

1. Define a class that inherits from `ScriptableObject` and declares the managed fields you want to keep.
1. In a baker, create an instance of this class with `ScriptableObject.CreateInstance`, and copy the managed data from the authoring component into it.
1. Add an unmanaged component that stores the instance in a `UnityObjectRef<T>` field.
1. In your systems, access the managed data through the `Value` property of the reference.

This approach lets you keep using the API of the managed type, but the code that reads the data can't be Burst-compiled. Use it only for data that you can't represent with unmanaged types.

### Change EntityCommandBuffer PlaybackPolicy code

The `PlaybackPolicy` enum is deprecated in its entirety and will be removed in a future version. This includes both `PlaybackPolicy.SinglePlayback` and `PlaybackPolicy.MultiPlayback`, as well as any `EntityCommandBuffer` constructor overload that takes a `PlaybackPolicy` parameter.

`SinglePlayback` is the only supported behavior and is the default: an `EntityCommandBuffer` can be played back only once. Create an `EntityCommandBuffer` without specifying a `PlaybackPolicy`. If you need to apply the same set of commands more than once, record them again into a new `EntityCommandBuffer` for each playback.

### Update code that relies on placeholder entities

In Unity 6.6 the `EntityCommandBuffer.CreateEntity` and `EntityCommandBuffer.Instantiate` methods, including their `ParallelWriter` overloads, return valid `Entity` references at record time, instead of the placeholder entities that Unity remapped during playback. You can store these references and pass them to other code, and they stay valid before and after playback. As in earlier versions, the entities have no components until you play back the command buffer, so you can't access them through `EntityManager` or queries until playback completes.

If code in your project specifically detects placeholder entities (for example, by checking for a negative `Entity.Index`), update it, because command buffers no longer create placeholder entities.

For more information, refer to [Entities created by command buffers](systems-entity-command-buffers.md#entities-created-by-command-buffers).

### Migrate PostLoadCommandBuffer to RequestSceneLoaded.ImportEntity

[`PostLoadCommandBuffer`](xref:Unity.Entities.PostLoadCommandBuffer) is deprecated and will be removed in a future version.

Replace `PostLoadCommandBuffer` with the [`RequestSceneLoaded.ImportEntity`](xref:Unity.Entities.RequestSceneLoaded) field, which you can use to pass the same data to `ProcessAfterLoad` systems as an entity instead of as recorded commands. To migrate a call site:

1. Create an entity in the main world, and add the components that your `EntityCommandBuffer` commands created.
1. Set `ImportEntity` to that entity, either in the `SceneSystem.LoadParameters` that you give to `SceneSystem.LoadSceneAsync`, or on the scene meta entity.
1. Don't change the `ProcessAfterLoad` system. It queries the same components as it did before.

If your project sets `PostLoadCommandBuffer` on both a scene meta entity and its section meta entities, set `ImportEntity` on the section meta entities only, and leave the scene-level value unset. The streaming system reads `PostLoadCommandBuffer` from the section and ignores any `PostLoadCommandBuffer` on the scene. `ImportEntity` behaves differently: if the section and the scene reference different entities, the streaming system imports both, and your `ProcessAfterLoad` systems receive two imported entities.

Don't destroy the entity you created in the main world before the scene load completes, because the streaming system reads from it while the scene loads. Destroy it when your project no longer needs it.

While Unity loads a section of the scene, the streaming system copies the entity you created into that section's streaming world. Unless your `ProcessAfterLoad` system destroys that copy after it reads the components, Unity moves the copy into the main world together with the rest of the section's entities. `PostLoadCommandBuffer` doesn't create this copy, so after you make this change, the main world can contain an entity that it didn't contain before.

For more information about how `ImportEntity` works, refer to [RequestSceneLoaded.ImportEntity](streaming-scene-instancing.md#requestsceneloadedimportentity).

### Replace SubScene.HierarchyColor

`SubScene.HierarchyColor` is deprecated and will be removed in a future version. Using it produces an obsolete API warning.

The [Hierarchy](editor-hierarchy-world-node.md) window displays subscenes in the same way as it displays scenes and GameObjects, so it no longer applies a per-subscene color. Remove the property from your code. If your project reads the value for its own purposes, declare a field on your own component and use that instead.

### Entities Hierarchy window is deprecated

The [Entities Hierarchy](editor-hierarchy-window.md) window (**Window** > **Entities** > **Hierarchy (Deprecated)**) is deprecated and will be removed in a future release. Entities appear in the standard [Hierarchy](editor-hierarchy-world-node.md) window alongside GameObjects, scenes, and subscenes, so use that window instead.

## Upgrading from Unity 6.4

Unity 6.5 removes the APIs that were deprecated in Entities 1.4, and introduces the following changes:

* [Change code that uses `Entities.ForEach` and `Job.WithCode`](#change-code-that-uses-entitiesforeach-and-jobwithcode)
* [Change Aspects code](#change-aspects-code)
* [Migrate InstanceID code to EntityId](#migrate-instanceid-code-to-entityid)
* [Journaling API is deprecated](#journaling-api-is-deprecated)

### Change code that uses Entities.ForEach and Job.WithCode

To consolidate the Entities API and improve iteration time, `Entities.ForEach` is deprecated in Entities 1.4 and removed in Unity 6.5. Use either [`IJobEntity`](#ijobentity) or [`SystemAPI.Query`](#systemapiquery) instead.

The `Job.WithCode` API is also removed. Use [`IJob`](xref:Unity.Jobs.IJob) instead.

#### IJobEntity

Because `IJobEntity` `Execute` methods support `ref` and `in` parameters to denote read-only and read-write status, you can often copy the lambda of an `Entities.ForEach` into the `Execute` method for the `IJobEntity` job struct. Additionally, `IJobEntity` supports all the scheduling options that `Entities.ForEach` supports.

> [!NOTE]
> `IJobEntity` isn't Burst-compiled by default and it can't capture variables because there is no lambda body. Use the `[BurstCompile]` attribute to enable Burst compilation and write captured variables into fields on the job struct.

Code example using `Entities.ForEach`

```c#
public partial class RotationSpeedSystemForEachISystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        Entities
            .ForEach((ref LocalTransform transform, in RotationSpeed rotationSpeed) =>
            {
                transform.Rotation = math.mul(
                    math.normalize(transform.Rotation),
                    quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
            })
            .ScheduleParallel();
    }
}

```

Code example using `IJobEntity`

```c#
[BurstCompile]
public partial struct ASampleJob : IJobEntity
{
    public float DeltaTime;
    void Execute(ref LocalTransform transform, in RotationSpeed rotationSpeed)
    {
        transform.Rotation = math.mul(
            math.normalize(transform.Rotation),
            quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime));
    }
}

public partial class ASample : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        new ASampleJob{ DeltaTime = deltaTime }.ScheduleParallel();
    }
}
```

For more information about `IJobEntity`, refer to [Iterate over component data with IJobEntity](iterating-data-ijobentity.md).

#### SystemAPI.Query

For entity iteration that doesn't have to happen in a job (but can still be Burst compiled), `SystemAPI.Query` can provide a simpler option because it uses the `RefRO` and `RefRW` types to wrap type parameters that you access as read-only and read-write respectively. There are additional builder methods on `Query` to indicate `WithAll`, `WithNone`, `WithAny` and other options.

The following changes the previous `Entities.ForEach` example to use `SystemAPI.Query`

```c#
public partial class ASample : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (transform, rotationSpeed) in 
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            transform.ValueRW.Rotation = math.mul(
                math.normalize(transform.ValueRO.Rotation),
                quaternion.AxisAngle(math.up(), rotationSpeed.ValueRO.RadiansPerSecond * deltaTime));
        }
    }
}
```

For more information about `SystemAPI.Query`, refer to [Iterate over component data with SystemAPI.Query](systems-systemapi-query.md).

### Change Aspects code

Aspects are deprecated in Entities 1.4 and removed in Unity 6.5, and there's no direct replacement for them. Instead you must replace the abstraction with explicit code that queries for the correct set of components and performs the expected operation on them. The following code provides a simple example of converting an aspect and its usage into an explicit `EntityQuery` and a helper method designed to perform the operation.

Code example using Aspects:

```c#
public partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var elapsedTime = SystemAPI.Time.ElapsedTime;

        foreach (var movement in SystemAPI.Query<VerticalMovementAspect>())
        {
            movement.Move(elapsedTime);
        }
    }
}

readonly partial struct VerticalMovementAspect : IAspect
{
    readonly RefRW<LocalTransform> m_Transform;
    readonly RefRO<RotationSpeed> m_Speed;

    public void Move(double elapsedTime)
    {
        m_Transform.ValueRW.Position.y = (float)math.sin(elapsedTime * m_Speed.ValueRO.RadiansPerSecond);
    }
}


```

Code example using `EntityQuery`:

```c#
public partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var elapsedTime = SystemAPI.Time.ElapsedTime;

        foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            VerticalMovementHelper.Move(elapsedTime, transform, speed);
        }
    }
}

static class VerticalMovementHelper
{
    public static void Move(double elapsedTime, RefRW<LocalTransform> transform, RefRO<RotationSpeed> speed)
    {
        transform.ValueRW.Position.y = (float)math.sin(elapsedTime * speed.ValueRO.RadiansPerSecond);
    }
}
```

### Migrate `InstanceID` code to `EntityId`

In Unity 6.5, the `EntityId` struct replaces the `InstanceID`-based APIs as the identifier that's shared between GameObjects and entities, and the obsolete `InstanceID` APIs produce compilation errors.

`EntityId` is a 64-bit value, so this change also affects code that never calls the obsolete APIs directly. For example, if your code stores identifiers as `int` values, or serializes them, the conversion truncates the identifier and Unity can't resolve it back to an object. For migration instructions that cover these scenarios, refer to the [EntityId API migration guide](xref:um-instanceid-to-entityid-migration).

### Journaling API is deprecated

The [Journaling](entities-journaling.md) window and the `EntitiesJournaling` API are deprecated in Unity 6.5 and will be removed in a future version. Remove dependencies on this API from your project.

From Unity 6.6, to track when components are added to or removed from entities, you can implement the [`IDebugOnAdded`](xref:Unity.Entities.IDebugOnAdded) and [`IDebugOnRemoved`](xref:Unity.Entities.IDebugOnRemoved) interfaces on a component type. These callbacks are available in the Unity Editor and in development builds.

## Upgrading from Entities 1.3 or earlier

In Unity 6.4, Entities became a core package included with the Unity Editor, and its version changed from 1.4 to 6.4.0. The 6.4.0 version of the package is functionally equivalent to Entities 1.4: it deprecates `Entities.ForEach`, `Job.WithCode`, and Aspects, which are then removed in Unity 6.5. To upgrade from Entities 1.x, apply all the sections above, and additionally:

* Replace the `ComponentLookup.GetRefRWOptional` and `ComponentLookup.GetRefROOptional` methods, deprecated in Entities 1.4, with `ComponentLookup.TryGetRefRW` and `ComponentLookup.TryGetRefRO`.

### Upgrading from Entities 1.0–1.2

If you upgrade from a version earlier than Entities 1.3, also note the following changes:

* The `EntityQueryCaptureMode.AtRecord` enum value is deprecated (Entities 1.3). For `EntityCommandBuffer` methods that target an `EntityQuery`, use `EntityQueryCaptureMode.AtPlayback`, which is also significantly faster. If you need capture-at-record semantics, capture the array of entities that match the query manually and pass the array to the corresponding `EntityCommandBuffer` command.
* The `EntityQuery.GetEntityQueryDesc` method is deprecated (Entities 1.3) because it only returns the first query description element. Use `EntityQuery.GetEntityQueryDescs` to get the full list.
* The `EntityManager.CopyEntities` method is deprecated (Entities 1.2). Use `EntityManager.Instantiate` to create copies of existing entities.
* Entities are no longer guaranteed to have the same `Entity` value across worlds (Entities 1.2). If you use `EntityManager.CopyAndReplaceEntitiesFrom`, pass a remapping table to the method to look up which entity in the destination world corresponds to a given entity in the source world.
