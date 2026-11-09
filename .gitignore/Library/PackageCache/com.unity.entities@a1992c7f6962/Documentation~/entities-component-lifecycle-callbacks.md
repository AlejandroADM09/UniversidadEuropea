---
uid: entities-component-lifecycle-callbacks
---

# Component lifecycle callbacks

Use component lifecycle callbacks to find out what adds a component to an entity, and what removes it. The callback lets you implement your own debugging code on the component type, and Unity runs it at the moment the component is added or removed.

Unity calls the lifecycle callbacks in the Editor and in development builds, and excludes them from release builds. Don't use them for behavior that your project needs at runtime.

## How component lifecycle callbacks work

For a component type to receive callbacks, implement the [`IDebugOnAdded`](xref:Unity.Entities.IDebugOnAdded) interface for when Unity adds the component to an entity, and [`IDebugOnRemoved`](xref:Unity.Entities.IDebugOnRemoved) for when Unity removes it. Each interface requires a matching static method on the same component type. For the signature that each method must have, refer to [Lifecycle callback method requirements](#lifecycle-callback-method-requirements).

You can implement either interface on its own. The following component receives a callback only when Unity adds it to an entity:

[!code-cs[Component with an OnAdded callback only](../DocCodeSamples.Tests/ComponentLifecycleCallbackExamples.cs#lifecycle-callbacks-single)]

Unity calls the callback while it performs a [structural change](concepts-structural-changes.md) that adds or removes the component. Unity passes the entity that the component was added to or removed from, and a read-only reference to the component.

Only the component types that implement one of the interfaces receive callbacks. You can add an interface to the type you need to investigate without affecting the rest of your project.

## Find out what added or removed a component

To inspect the call stack at the moment when Unity adds or removes a component:

1. Implement `IDebugOnAdded`, `IDebugOnRemoved`, or both on the component type that you want to investigate.
2. Add the matching static method for each interface that you implement:

    [!code-cs[Component with lifecycle callbacks](../DocCodeSamples.Tests/ComponentLifecycleCallbackExamples.cs#lifecycle-callbacks)]

3. Set a breakpoint inside the method body.
4. Attach a debugger to the Unity Editor or to the development build. For information on how to attach a debugger, refer to [Debug C# code in Unity](xref:um-managed-code-debugging).
5. Enter Play mode or run your build until the debugger pauses at the breakpoint.

The call stack shows the code that added or removed the component, including the system that this code runs in. Several frames of Unity's own code separate the callback from the method that made the change, so that method isn't in the frame directly below the callback. If the change comes from an [entity command buffer](systems-entity-command-buffers.md), Unity calls the callback when the command buffer runs the command. The call stack then shows the command buffer and not the system that recorded the command.

## What the callbacks report

Unity calls `OnAdded` when you add the component to an entity that doesn't have it, and `OnRemoved` when you remove the component from an entity, or destroy an entity that has the component.

Unity calls the callback once for each affected entity, even when a single call changes many entities. If you add or remove the component with a call that takes an [entity query](systems-entityquery.md) or an array of entities, Unity calls the callback once for every entity in that set.

Unity doesn't call the callbacks in the following cases:

* You add a component that the entity already has, or remove a component that the entity doesn't have. Neither operation changes the entity's set of components.
* You create an entity that already has the component, for example by passing an [archetype](concepts-archetypes.md) that includes it to [`EntityManager.CreateEntity`](xref:Unity.Entities.EntityManager.CreateEntity*).
* You enable or disable the component with [`EntityManager.SetComponentEnabled`](xref:Unity.Entities.EntityManager.SetComponentEnabled*). A disabled component is still present on the entity. For more information, refer to [Enableable components](components-enableable.md).
* You add or remove a different component on the same entity. Unity moves the entity to another [chunk](components-chunk.md), but the component with the callback stays on the entity.
* You write a new value to a component that the entity already has. The callbacks report that a component was added or removed, and not the values that your project writes to it.
* You add or remove the component as a [dynamic buffer component](components-buffer.md). Unity doesn't call lifecycle callbacks for types that implement `IBufferElementData`.

## Lifecycle callback method requirements

Unity generates the code that calls the callback when it compiles the project, so it looks for a method with an exact name, modifiers, return type, and parameters. A callback method must:

* Be named `OnAdded` for `IDebugOnAdded`, or `OnRemoved` for `IDebugOnRemoved`.
* Be `public` and `static`.
* Return `void`.
* Take exactly two parameters: an [`Entity`](xref:Unity.Entities.Entity), followed by an `in` parameter of the component's own type.

If a component type implements one of the interfaces without a matching method, compilation fails with an error.

To resolve the error, add the method that the error names to the component type, or remove the interface from the component type.

## Callbacks and build configuration

Unity compiles the callbacks into the Editor and into development builds. A release build excludes them, so a callback has no effect on the performance of a release build.

To keep the callbacks in a release build, add the `UNITY_DOTS_DEBUG` scripting symbol to your project's [scripting symbols](xref:um-custom-scripting-symbols).

## Additional resources

* [Add components to an entity](components-add-to-entity.md)
* [Remove components from an entity](components-remove-from-entity.md)
* [Entities Structural Changes Profiler module reference](profiler-module-structural-changes.md)
* [Journaling](entities-journaling.md) (deprecated)
