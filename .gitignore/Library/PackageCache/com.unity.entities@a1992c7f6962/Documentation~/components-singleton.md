# Singleton components

A singleton component is a component that has only one instance in a given [world](concepts-worlds.md). For example, if only one entity in a world has a component of type `T`, then `T` is a singleton component. 

If a singleton component is added to another entity, then it's no longer a singleton component. Additionally, a singleton component can exist in another world, without affecting its singleton state.

## Singleton component APIs

The Entities package contains several APIs you can use to work with singleton components:

|**Namespace**|**Method**|
|---|---|
|**[EntityManager](xref:Unity.Entities.EntityManager)**|[CreateSingleton](xref:Unity.Entities.EntityManager.CreateSingleton*)|
|**[EntityQuery](xref:Unity.Entities.EntityQuery)**|<ul><li>[GetSingletonEntity](xref:Unity.Entities.EntityQuery.GetSingletonEntity)</li><li>[GetSingleton](xref:Unity.Entities.EntityQuery.GetSingleton*)</li><li>[GetSingletonRW](xref:Unity.Entities.EntityQuery.GetSingletonRW*)</li><li>[TryGetSingleton](xref:Unity.Entities.EntityQuery.TryGetSingleton*)</li><li>[HasSingleton](xref:Unity.Entities.EntityQuery.HasSingleton*)</li><li>[TryGetSingletonBuffer](xref:Unity.Entities.EntityQuery.TryGetSingletonBuffer*)</li><li>[TryGetSingletonEntity](xref:Unity.Entities.EntityQuery.TryGetSingletonEntity*)</li><li>[GetSingletonBuffer](xref:Unity.Entities.EntityQuery.GetSingletonBuffer*)</li><li>[SetSingleton](xref:Unity.Entities.EntityQuery.SetSingleton*)</li></ul>|
|**[SystemAPI](xref:Unity.Entities.SystemAPI)**|<ul><li>[GetSingletonEntity](xref:Unity.Entities.SystemAPI.GetSingletonEntity*)</li><li>[GetSingleton](xref:Unity.Entities.SystemAPI.GetSingleton*)</li><li>[GetSingletonRW](xref:Unity.Entities.SystemAPI.GetSingletonRW*)</li><li>[TryGetSingleton](xref:Unity.Entities.SystemAPI.TryGetSingleton*)</li><li>[HasSingleton](xref:Unity.Entities.SystemAPI.HasSingleton*)</li><li>[TryGetSingletonBuffer](xref:Unity.Entities.SystemAPI.TryGetSingletonBuffer*)</li><li>[TryGetSingletonEntity](xref:Unity.Entities.SystemAPI.TryGetSingletonEntity*)</li><li>[GetSingletonBuffer](xref:Unity.Entities.SystemAPI.GetSingletonBuffer*)</li><li>[SetSingleton](xref:Unity.Entities.SystemAPI.SetSingleton*)</li></ul>|

It's useful to use the singleton component APIs in situations where you know that there's only one instance of a component. For example, if you have a single-player application and only need one instance of a `PlayerController` component, you can use the singleton APIs to simplify your code. Additionally, in server-based architecture, client-side implementations typically track timestamps for their instance only, so the singleton APIs are convenient and simplify a lot of hand written code.

## Disabled entities and prefabs

The `SystemAPI` singleton methods build their query with the default [`EntityQueryOptions`](xref:Unity.Entities.EntityQueryOptions) flags, which exclude entities that have the `Disabled` component and entities that have the `Prefab` component. As a result:

* If the only entity that has component `T` is disabled, [HasSingleton](xref:Unity.Entities.SystemAPI.HasSingleton*) returns `false` and [GetSingleton](xref:Unity.Entities.SystemAPI.GetSingleton*) throws an `InvalidOperationException`.
* The same applies if the only entity that has component `T` is an [entity prefab](baking-prefabs.md).

To find a singleton component on a disabled entity or on an entity prefab, create an [`EntityQuery`](xref:Unity.Entities.EntityQuery) with the [`IncludeDisabledEntities`](xref:Unity.Entities.EntityQueryOptions) or [`IncludePrefab`](xref:Unity.Entities.EntityQueryOptions) option, then call the singleton methods on that query.

The singleton methods also throw an `InvalidOperationException` exception if the component type implements [`IEnableableComponent`](xref:Unity.Entities.IEnableableComponent), or if more than one entity matches.

## Dependency completion

Singleton components have special-case behavior in dependency completion in systems code. With normal component access, APIs such as [EntityManager.GetComponentData](xref:Unity.Entities.EntityManager.GetComponentData*) or [SystemAPI.GetComponent](xref:Unity.Entities.SystemAPI.GetComponent*) ensure that any running jobs that might write to the same component data on a worker thread are completed before returning the requested data.

However, singleton API calls don't ensure that running jobs are completed first. The Jobs Debugger logs an error on invalid access, and you either need to manually complete dependencies with [EntityManager.CompleteDependencyBeforeRO](xref:Unity.Entities.EntityManager.CompleteDependencyBeforeRO*) or [EntityManager.CompleteDependencyBeforeRW](xref:Unity.Entities.EntityManager.CompleteDependencyBeforeRW*), or you need to restructure the data dependencies.

### Choose between GetSingleton and GetSingletonRW

[GetSingleton](xref:Unity.Entities.SystemAPI.GetSingleton*) and [GetSingletonRW](xref:Unity.Entities.SystemAPI.GetSingletonRW*) have the following differences:

|**Difference**|**GetSingleton**|**GetSingletonRW**|
|---|---|---|
|**Return value**|A copy of the component.|A reference to the component.|
|**Query access**|Read-only, which registers the system as a reader of the component.|Read/write, which registers the system as a writer of the component.|
|**Safety check**|Throws an exception if a job that writes the component is still running.|Throws an exception if a job that reads or writes the component is still running.|
|**Change version**|Unchanged.|Incremented, even if you don't write to the reference.|

These differences have the following consequences:

* `GetSingletonRW` registers the system as a writer of the component, so systems that read that component and update later wait for this system's jobs.
* `GetSingletonRW` throws exceptions in more situations than `GetSingleton`, because a job that only reads the component also conflicts with read/write access. As described above, neither method completes the conflicting job for you, and both run this check only if safety checks are enabled.
* `GetSingletonRW` increments the change version even if you write the same value, so queries that filter on changes to the component match the singleton entity.

Use `GetSingleton` if you only read the value, and `GetSingletonRW` if you write to the component.

You should also be careful if you use [GetSingletonRW](xref:Unity.Entities.EntityQuery.GetSingletonRW*) to get read/write access to components. Because a reference to component data is returned, it's possible to modify data while jobs are also reading or writing it. The best practices for [GetSingletonRW](xref:Unity.Entities.EntityQuery.GetSingletonRW*) are:

* Only use to access a `NativeContainer` in a component. This is because native containers have their own safety mechanisms compatible with Jobs Debugger, separate from ECS component safety mechanisms.
* Check the Jobs Debugger for errors. Any errors indicate a dependency issue that you need to either restructure or manually complete.
