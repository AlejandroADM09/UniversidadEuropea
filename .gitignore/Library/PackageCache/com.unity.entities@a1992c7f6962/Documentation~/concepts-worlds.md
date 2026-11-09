---
uid: concepts-worlds
---

# World concepts

A **world** is a collection of [entities](concepts-entities.md). An entity's ID number is only unique within its own world. A world has an [`EntityManager`](xref:Unity.Entities.EntityManager) struct, which you use to create, destroy, and modify the entities within the world. 

A world owns a set of [systems](concepts-systems.md), which usually only accesses the entities within that same world. Additionally, a set of entities within a world which have the same set of component types are stored together in an [archetype](concepts-archetypes.md), which determines how the components in your program are organized in memory.

Entity worlds are displayed as nodes in the [Hierarchy window](editor-hierarchy-world-node.md).

## Initialization

By default, when you enter Play mode, Unity creates a `World` instance and adds every system to this default world.

Unity assigns this world to [`World.DefaultGameObjectInjectionWorld`](xref:Unity.Entities.World.DefaultGameObjectInjectionWorld). Code that runs outside a system has no world to work from, so it reads this property to reach the default world. This applies both to your own MonoBehaviour scripts and to Editor tools such as the SubScene Inspector and the [Entity Inspector](editor-entity-inspector.md). For example, to get the `EntityManager` of the default world from a MonoBehaviour, use `World.DefaultGameObjectInjectionWorld.EntityManager`.

If you prefer to add systems to the default world manually, create a single class implementing the [ICustomBootstrap](xref:Unity.Entities.ICustomBootstrap) interface.
 
If you want full manual control of bootstrapping, use these defines to  disable the default world creation:

* `#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD`: Disables generation of the default runtime world.
* `#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_EDITOR_WORLD`: Disables generation of the default Editor world.
* `#UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP`: Disables generation of both default worlds.

Your code is then responsible for creating your worlds and systems, plus inserting updates of your worlds into the Unity scriptable [PlayerLoop](xref:UnityEngine.LowLevel.PlayerLoop).
For more information on how to manage systems in multiple worlds, refer to [Manage systems in multiple worlds](systems-icustombootstrap.md).

Unity uses [`WorldFlags`](xref:Unity.Entities.WorldFlags) to create specialized worlds in the Editor.

## Additional resources

* [Entities concepts](concepts-entities.md)
* [Systems concepts](concepts-systems.md)
* [Entity world in Hierarchy window](editor-hierarchy-world-node.md)
