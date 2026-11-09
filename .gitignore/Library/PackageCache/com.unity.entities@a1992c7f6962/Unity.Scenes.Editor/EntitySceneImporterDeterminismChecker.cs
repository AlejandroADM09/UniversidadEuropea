using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;

namespace Unity.Scenes.Editor
{
    /// <summary>
    /// Utility for checking if SubScene imports are deterministic.
    /// </summary>
    public static class EntitySceneImporterDeterminismChecker
    {
        /// <summary>
        /// Contains the result of a determinism check.
        /// </summary>
        public struct Result
        {
            /// <summary>
            /// True if the check completed without errors.
            /// </summary>
            public bool Success;

            /// <summary>
            /// True if both imports produced identical artifact hashes.
            /// </summary>
            public bool IsDeterministic;

            /// <summary>
            /// The artifact hash from the first import.
            /// </summary>
            public Hash128 FirstHash;

            /// <summary>
            /// The artifact hash from the second import.
            /// </summary>
            public Hash128 SecondHash;

            /// <summary>
            /// Detailed byte-level comparison of artifact files when non-deterministic.
            /// </summary>
            public string Diagnostics;

            /// <summary>
            /// Error message if Success is false.
            /// </summary>
            public string Error;
        }

        /// <summary>
        /// Checks if a SubScene's import is deterministic by running two imports and comparing artifact hashes.
        /// </summary>
        /// <param name="scenePath">The asset path of the SubScene to check.</param>
        /// <param name="artifactDumpDir">Optional directory to write mismatched artifact files and ImHex patterns for offline inspection.</param>
        /// <returns>A Result containing the check outcome and artifact hashes.</returns>
        public static Result Check(string scenePath, string artifactDumpDir = null)
        {
            var sceneGuid = AssetDatabaseCompatibility.PathToGUID(scenePath);
            if (sceneGuid.Empty())
                return new Result { Error = $"Scene not found: {scenePath}" };

            var configGUID = SceneWithBuildConfigurationGUIDs.EnsureExistsFor(sceneGuid, default, true, out var createdNew);
            if (createdNew)
                AssetDatabase.Refresh();

            var artifactKey = new ArtifactKey(configGUID, typeof(SubSceneImporter));

            var firstHash = ForceProduceAndGetHash(artifactKey);
            if (!firstHash.isValid)
                return new Result { Error = "First import failed" };
            var firstArtifacts = CollectArtifactFiles(firstHash);

            var secondHash = ForceProduceAndGetHash(artifactKey);
            if (!secondHash.isValid)
                return new Result { Error = "Second import failed" };
            var secondArtifacts = CollectArtifactFiles(secondHash);

            string diagnostics = null;
            if (firstHash != secondHash)
            {
                var sceneDirName = SanitizePathForDirectory(scenePath);
                var sceneDumpDir = artifactDumpDir != null ? Path.Combine(artifactDumpDir, sceneDirName) : null;
                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                diagnostics = CompareArtifacts(firstArtifacts, secondArtifacts, sceneDumpDir, sceneName);
            }

            return new Result
            {
                Success = true,
                IsDeterministic = firstHash == secondHash,
                FirstHash = firstHash,
                SecondHash = secondHash,
                Diagnostics = diagnostics
            };
        }

        static Hash128 ForceProduceAndGetHash(ArtifactKey artifactKey)
        {
            AssetDatabaseExperimental.ForceProduceArtifact(artifactKey);
            return AssetDatabaseCompatibility.ProduceArtifact(artifactKey);
        }

        static Dictionary<string, byte[]> CollectArtifactFiles(Hash128 artifactHash)
        {
            var result = new Dictionary<string, byte[]>();
            if (!AssetDatabaseCompatibility.GetArtifactPaths(artifactHash, out var paths))
                return result;

            foreach (var path in paths)
            {
                // Artifact paths are "<hash>.<extension>" — strip the hash prefix to get a stable key for comparison.
                var fileName = Path.GetFileName(path);
                var dotIndex = fileName.IndexOf('.');
                var key = dotIndex >= 0 ? fileName.Substring(dotIndex + 1) : fileName;

                try
                {
                    result[key] = FileUtil.ReadAllBytes(path);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"CollectArtifactFiles: failed to read {path}: {e.Message}");
                }
            }
            return result;
        }

        static string CompareArtifacts(Dictionary<string, byte[]> first, Dictionary<string, byte[]> second, string dumpDir, string sceneName)
        {
            var sb = new System.Text.StringBuilder();
            var allKeys = new SortedSet<string>();
            foreach (var k in first.Keys) allKeys.Add(k);
            foreach (var k in second.Keys) allKeys.Add(k);

            foreach (var key in allKeys)
            {
                var hasFirst = first.TryGetValue(key, out var firstBytes);
                var hasSecond = second.TryGetValue(key, out var secondBytes);

                if (!hasFirst)
                {
                    sb.Append($" [{key}] ONLY_IN_SECOND ({secondBytes.Length}b)");
                    DumpSingleArtifact(dumpDir, key, "second", secondBytes);
                    continue;
                }
                if (!hasSecond)
                {
                    sb.Append($" [{key}] ONLY_IN_FIRST ({firstBytes.Length}b)");
                    DumpSingleArtifact(dumpDir, key, "first", firstBytes);
                    continue;
                }

                if (firstBytes.Length != secondBytes.Length)
                {
                    sb.Append($" [{key}] SIZE_DIFF first={firstBytes.Length}b second={secondBytes.Length}b");
                    DumpArtifactPair(dumpDir, key, firstBytes, secondBytes);
                    continue;
                }

                var diffCount = 0;
                var firstDiffOffset = -1;
                for (int i = 0; i < firstBytes.Length; i++)
                {
                    if (firstBytes[i] != secondBytes[i])
                    {
                        diffCount++;
                        if (firstDiffOffset < 0)
                            firstDiffOffset = i;
                    }
                }

                if (diffCount == 0)
                {
                    sb.Append($" [{key}] MATCH ({firstBytes.Length}b)");
                }
                else
                {
                    sb.Append($" [{key}] DIFFERS ({firstBytes.Length}b) diffBytes={diffCount} firstDiffAt=0x{firstDiffOffset:X}");
                    DumpArtifactPair(dumpDir, key, firstBytes, secondBytes);
                }
            }

            if (dumpDir != null && Directory.Exists(dumpDir))
            {
                CollectImHexPatterns(dumpDir, sceneName);
                sb.Append($" artifactDump={dumpDir}");
            }

            return sb.ToString();
        }

        static void CollectImHexPatterns(string dumpDir, string sceneName)
        {
            var imhexDir = Path.Combine(".", "Library", "GeneratedImHex");
            if (!Directory.Exists(imhexDir))
                return;

            try
            {
                foreach (var hexpat in Directory.GetFiles(imhexDir, $"{sceneName}*.hexpat"))
                {
                    var destPath = Path.Combine(dumpDir, Path.GetFileName(hexpat));
                    File.Copy(hexpat, destPath, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to collect ImHex patterns: {e.Message}");
            }
        }

        static string SanitizePathForDirectory(string assetPath)
        {
            var name = assetPath.Replace("Assets/", "");
            var ext = Path.GetExtension(name);
            if (ext.Length > 0)
                name = name.Substring(0, name.Length - ext.Length);
            return name.Replace('/', '_').Replace('\\', '_');
        }

        static void DumpSingleArtifact(string dumpDir, string key, string suffix, byte[] data)
        {
            if (dumpDir == null)
                return;

            try
            {
                Directory.CreateDirectory(dumpDir);
                File.WriteAllBytes(Path.Combine(dumpDir, $"{key}.{suffix}"), data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to dump artifact {key}.{suffix}: {e.Message}");
            }
        }

        static void DumpArtifactPair(string dumpDir, string key, byte[] first, byte[] second)
        {
            if (dumpDir == null)
                return;

            try
            {
                Directory.CreateDirectory(dumpDir);
                File.WriteAllBytes(Path.Combine(dumpDir, $"{key}.first"), first);
                File.WriteAllBytes(Path.Combine(dumpDir, $"{key}.second"), second);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to dump artifact pair for {key}: {e.Message}");
            }
        }

    }
}
