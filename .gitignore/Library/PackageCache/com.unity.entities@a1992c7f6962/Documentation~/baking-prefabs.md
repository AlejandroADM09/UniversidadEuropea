#  Prefabs in baking

During the baking process, [prefabs](xref:Prefabs) are baked into entity prefabs. An entity prefab is an entity that has the following components:

* A prefab tag: Identifies the entity as a prefab and excludes them from queries by default. 
* A [`LinkedEntityGroup`](linked-entity-group.md) buffer: Stores all children within the prefab in a flat list. For example, to quickly create the whole set of entities within a prefab without having to traverse the hierarchy. 

![The Hierarchy window with an entity prefab selected.](images/getting-started/ecs-entities-prefabs-instantiated.png)<br/>_An entity prefab selected in the Hierarchy window. The Entity Inspector shows its `Prefab` tag, and the entities instantiated from it appear below it in the Hierarchy._

To use entity prefabs at runtime, you must [bake](baking-overview.md) the GameObject prefabs and make them available in the [entity scene](conversion-scene-overview.md). For a step-by-step example, refer to [Entity prefab instantiation workflow](ecs-workflow-example-prefab-instantiation.md).

> [!NOTE]
> When prefab instances are present in the subscene hierarchy, baking treats them as normal GameObjects because they don't have the `Prefab` or `LinkedEntityGroup` components.

> [!NOTE]
> When a Prefab is baked, the `Dynamic` [transform usage flag](transforms-usage-flags.md) is always added to the prefab root. This ensures that the prefab entity has the required transform components for moving at runtime, for example for changing its position after instantiation.

## Create and register an Entity prefab

To ensure that prefabs are baked and available in the entity scene, you must register them to a [baker](baking-baker-overview.md). This makes sure that there is a dependency on the prefab object, and that the prefab is baked and receives the proper components. When you reference the entity prefab in a component, Unity serializes the content into the subscene that uses it.

[!code-cs[EntityPrefabInSubScene](../DocCodeSamples.Tests/BakingPrefabExamples.cs#EntityPrefabInSubScene)]

To bake a prefab with this authoring component:

1. Add the authoring component to a GameObject inside a [subscene](conversion-subscenes.md).
2. In the Inspector, assign a prefab to the **Prefab** field of the authoring component.

Unity bakes the prefab as soon as you assign it. The entity prefab then appears under the [world node](editor-hierarchy-world-node.md) in the Hierarchy window, with a blue icon:

![The Hierarchy window with an authoring GameObject, the Inspector showing the assigned Prefab field, and the resulting entity prefab under the Editor World node.](images/getting-started/ecs-entities-hierarchy-prefab-view.png)<br/>_The authoring GameObject inside the `ECS example` subscene with the `Cube` prefab assigned, and the resulting `Cube` entity prefab under the **Editor World** node._

Alternatively, to reference the entity prefab during baking, use the [`EntityPrefabReference`](xref:Unity.Entities.Serialization.EntityPrefabReference) struct. This serializes the ECS content of the prefab into a separate entity scene file that can be loaded at runtime before using the prefab. This prevents Unity from duplicating the entity prefab in every subscene that it's used in.

[!code-cs[EntityPrefabReferenceInSubScene](../DocCodeSamples.Tests/BakingPrefabExamples.cs#EntityPrefabReferenceInSubScene)]

## Instantiate prefabs

To instantiate prefabs that are referenced in components, use an [`EntityManager`](xref:Unity.Entities.EntityManager) or an [entity command buffer](systems-entity-command-buffers.md):

[!code-cs[InstantiateEmbeddedPrefabs](../DocCodeSamples.Tests/BakingPrefabExamples.cs#InstantiateEmbeddedPrefabs)]

> [!NOTE]
> Instanced prefabs will contain a [SceneSection component](streaming-scene-sections.md#entity-prefabs-and-sections). This could affect the lifetime of the entity.

To instantiate a prefab referenced with `EntityPrefabReference`, you must also add the [`RequestEntityPrefabLoaded`](xref:Unity.Scenes.RequestEntityPrefabLoaded) struct to the entity. This is because Unity needs to load the prefab before it can use it. `RequestEntityPrefabLoaded` ensures that the prefab is loaded and the result is added to the `PrefabLoadResult` component. Unity adds the [`PrefabLoadResult`](xref:Unity.Scenes.PrefabLoadResult) component to the same entity that contains the `RequestEntityPrefabLoaded`.

[!code-cs[InstantiateLoadedPrefabs](../DocCodeSamples.Tests/BakingPrefabExamples.cs#InstantiateLoadedPrefabs)]

## Prefabs in queries

By default, Unity excludes prefabs from queries. To include entity prefabs in queries, use the [`IncludePrefab`](xref:Unity.Entities.EntityQueryOptions) field in the query. The following example queries a `Turret` component that a baker adds to the prefab GameObject, so both the entity prefab and its instances have that component:

[!code-cs[PrefabsInQueries](../DocCodeSamples.Tests/BakingPrefabExamples.cs#PrefabsInQueries)]

## Destroy prefab instances

To destroy a prefab instance, use an [`EntityManager`](xref:Unity.Entities.EntityManager) or an [entity command buffer](systems-entity-command-buffers.md), in the same way that you destroy an entity. Also, destroying a prefab has [structural change](concepts-structural-changes.md) costs. 

Because queries exclude entity prefabs by default, a query that doesn't use `IncludePrefab` doesn't match the entity prefab. The following example destroys the instances of a prefab and leaves the entity prefab itself in place, so you can instantiate it again later. It also uses [`IncludeDisabledEntities`](xref:Unity.Entities.EntityQueryOptions), because queries exclude disabled entities by default:

[!code-cs[DestroyPrefabs](../DocCodeSamples.Tests/BakingPrefabExamples.cs#DestroyPrefabs)]

## Additional resources

* [Baker overview](baking-baker-overview.md)
* [Linked entity groups](linked-entity-group.md)
* [Entity prefab instantiation workflow](ecs-workflow-example-prefab-instantiation.md)
