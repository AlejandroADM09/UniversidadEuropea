# Check whether baking is deterministic

Baking is deterministic when the same authoring data always produces the same entity scene files. If a baker produces different output from the same input, Unity writes different files every time it imports the subscene, even when you haven't changed anything. Any content update that you deliver to users then includes those files unnecessarily.

Use the [`EntitySceneImporterDeterminismChecker`](xref:Unity.Scenes.Editor.EntitySceneImporterDeterminismChecker) class to find out whether a subscene bakes deterministically.

## Implement the determinism check

The [`Check`](xref:Unity.Scenes.Editor.EntitySceneImporterDeterminismChecker.Check*) method takes the asset path of a scene, imports it twice, and compares the hash of the artifacts that the subscene importer wrote each time. If the two hashes match, everything that baking wrote is identical. The check takes roughly twice as long as a full import of the subscene.

The following example calls the `Check` method from a menu item. The example checks the subscene of the selected GameObject:

[!code-cs[Check the determinism of the selected subscene](../DocCodeSamples.Tests/BakingDeterminismExamples.cs#determinism-check-selected)]

Place the script in an [Editor folder](xref:um-special-folders).

## Read the result

The [`Check`](xref:Unity.Scenes.Editor.EntitySceneImporterDeterminismChecker.Check*) method returns a [`Result`](xref:Unity.Scenes.Editor.EntitySceneImporterDeterminismChecker.Result) struct with the following fields:

| **Field** | **Meaning** |
| --- | --- |
| `Success` | Indicates whether the check ran or not. If it's false, the `Error` field contains the reason why it didn't, for example, if the scene path doesn't resolve to an asset. |
| `Error` | The reason the check didn't run, when `Success` is false. |
| `IsDeterministic` | Whether both imports produced the same artifact hash. |
| `FirstHash`, `SecondHash`| The artifact hash of each import. |
| `Diagnostics` | A per-artifact comparison, which the `Check` method produces only when the two hashes differ. Otherwise, it's `null`. |

The `Check` method doesn't write its result to the Console window, so you need to write the code that processes the `Result` struct it returns.

## Find what differs between the two imports

When the two imports produce different hashes, the `Diagnostics` field lists each artifact that the subscene importer wrote, and whether it's the same in both imports. The field is a single line of text. The following example is wrapped for readability:

```
[0.entities] DIFFERS (35340b) diffBytes=3 firstDiffAt=0x4780
[entityheader] MATCH (1348b) [exportedtypes] MATCH (487b)
[globalusage] MATCH (32b) [weakassetrefs] MATCH (52b)
```

The name of each artifact that differs indicates which part of the baked data isn't deterministic:

| **Artifact** | **Contents** |
| --- | --- |
| `<section>.entities` | The entities and component data of one [scene section](streaming-scene-sections.md). The number is the section index, so `0.entities` is the first section. |
| `entityheader` | The scene header, which lists the sections and their bounds. |
| `exportedtypes` | The component types that the scene uses. |
| `globalusage` | The lighting and rendering settings that the scene uses, which Unity uses to decide which shader variants to include in a build. |
| `weakassetrefs` | The weak references to the assets that the scene uses. |

To compare the contents of the artifacts that differ, pass a directory path as the `artifactDumpDir` parameter:

```c#
var result = EntitySceneImporterDeterminismChecker.Check(scenePath, "Temp/DeterminismDumps");
```

If the two imports produce different hashes, the `Check` method creates a subdirectory named after the scene's asset path, for example `Temp/DeterminismDumps/Scenes_ExampleSubScene`. For each artifact that differs, it writes the version from the first import as `<artifact>.first` and the version from the second import as `<artifact>.second`. The `Diagnostics` field then ends with an `artifactDump` entry that contains the subdirectory path.

To find out which bytes changed and what they represent, open the `.first` and `.second` files in the **ImHex** editor, as described in [Binary debugging](entities-binary-debugging.md).

## Limitations of a single call to the Check method

Each call to the [`Check`](xref:Unity.Scenes.Editor.EntitySceneImporterDeterminismChecker.Check*) method runs both imports in the same Editor session, so it only detects non-deterministic behavior within that session. For example, a baker that depends on the IDs that Unity assigns to loaded objects produces different files after an Editor restart, but a single `Check` call doesn't detect this, because those IDs don't change within a session. To detect that kind of difference, [check determinism across Editor sessions](#check-determinism-across-editor-sessions).

A single `Check` call also has these limits:

* It compares two full imports. It doesn't compare incremental baking with full baking, which produce different output by design. For more information, refer to [Baking overview](baking-overview.md).
* It compares whole scenes. A mismatch indicates that something in the scene bakes differently, not which baker caused it.

## Check determinism across Editor sessions

To compare the artifacts that a scene produces in separate Editor sessions, record the hash of one import in each session and compare the records. The following example writes the hash of each subscene in the open scene to the Console window:

[!code-cs[Record the artifact hash of every subscene in the open scene](../DocCodeSamples.Tests/BakingDeterminismExamples.cs#determinism-record-hashes)]

Follow these steps:

1. Record the hash of each subscene, for example with the code above, and copy the hashes from the console to a file.

2. Close the Editor.

3. Delete the project's `Library` folder, and the `Assets/SceneDependencyCache` folder if the project has one, so that the next session imports everything again.

4. Open the project, record the hashes again, and compare them with the saved ones.

If a hash changes between sessions and `IsDeterministic` is true in both records, baking depends on something that differs per session, such as the ID of a loaded object.

## Common causes of non-deterministic baking

The following table contains common causes of non-deterministic baking and how to fix them.

| **Cause** | **Solution option** |
| --- | --- |
| A baker or baking system iterates a collection whose order isn't defined, such as `HashSet<T>` or `Dictionary<TKey, TValue>`. | Sort the items using a stable key before you write them. |
| A baker or baking system stores the `EntityId` of an object, or sorts objects by it. Unity assigns these IDs when it loads an object, so they differ after a restart. | Use the asset GUID and local file identifier instead. |
| A baker stores data in its own fields during one call to the `Bake` method, and reads that data in a later call. Unity creates one instance of each baker and calls its `Bake` method many times, in an order that isn't defined. | Write bakers that only read the authoring data passed to them. For more information, refer to [Baker overview](baking-baker-overview.md). |
| A baker or baking system reads the time, a random number, or a machine-specific path. | Derive the value from the authoring data instead. |

## Additional resources

* [Baking overview](baking-overview.md)
* [Baker overview](baking-baker-overview.md)
* [Binary debugging](entities-binary-debugging.md)
* [`EntitySceneImporterDeterminismChecker`](xref:Unity.Scenes.Editor.EntitySceneImporterDeterminismChecker)
