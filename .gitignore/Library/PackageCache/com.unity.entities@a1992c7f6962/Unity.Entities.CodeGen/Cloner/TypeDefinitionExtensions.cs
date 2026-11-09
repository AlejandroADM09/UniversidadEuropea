using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.Entities.CodeGen.Cloner
{
    public static class TypeDefinitionExtensions
    {
        public static (List<MethodDefinition> Original, List<(MethodDefinition Definition, string Source)> Rewritten)
            GetOriginalAndRewrittenMethods(this TypeDefinition typeDefinition)
        {
            var original = new List<MethodDefinition>();
            var rewritten = new List<(MethodDefinition, string)>();

            foreach (MethodDefinition method in typeDefinition.Methods)
            {
                var patchedAttribute =
                    method.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == nameof(DOTSCompilerPatchedMethodAttribute));

                if (patchedAttribute != null)
                {
                    rewritten.Add((method, patchedAttribute.ConstructorArguments.First().Value.ToString()));
                }
                else
                {
                    original.Add(method);
                }
            }

            return (original, rewritten);
        }

        static string GetPropertyName(PropertyDefinition propertyDefinition) => $"{propertyDefinition.DeclaringType.ToString().Replace('/', '.')}.{propertyDefinition.Name}";
        public static (Dictionary<string, PropertyDefinition> OriginalLookup, List<(PropertyDefinition Definition, string Source)> Rewritten)
            GetOriginalAndRewrittenProperties(this TypeDefinition typeDefinition)
        {
            var originalLookup = new Dictionary<string, PropertyDefinition>();
            var rewritten = new List<(PropertyDefinition, string)>();

            foreach (var property in typeDefinition.Properties)
            {
                var patchedAttribute =
                    property.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == nameof(DOTSCompilerPatchedPropertyAttribute));

                if (patchedAttribute != null)
                {
                    rewritten.Add((property, patchedAttribute.ConstructorArguments.First().Value.ToString()));
                }
                else
                {
                    originalLookup[GetPropertyName(property)] = property;
                }
            }

            return (originalLookup, rewritten);
        }

        public static void UpdateOriginalMethod(
            this TypeDefinition typeDef,
            Dictionary<string, MethodDefinition> originalMethodIdsToDefinitions,
            (MethodDefinition Definition, string ConstructorArgument) rewrittenMethodIn,
            DisplayClassGraph displayClassGraph)
        {
            var originalMethod = originalMethodIdsToDefinitions[rewrittenMethodIn.ConstructorArgument];
            var rewrittenMethod = rewrittenMethodIn.Definition;

            // If we are using a display class in a method but not in the source, we need to remove the display class usage
            // (otherwise Mono will get confused when trying to debug multiple sequence points that point to the same source file location)
            // Similarly we need to remove all local functions declared in the original method.
            var (displayClassesUsedInOriginal, localFunctionsUsedInOriginal) =
                GetDisplayClassAndLocalFunctionUsages(originalMethod, true);
            var (displayClassesUsedInRewritten, _) = GetDisplayClassAndLocalFunctionUsages(rewrittenMethod, false);

            // Roslyn emits a method's closure frames as sibling nested types that reference each other, and only the
            // ones the body names directly are found above. Expanding to whole clusters stops a removal leaving the
            // rest behind pointing at types that no longer exist (UUM-86534).
            displayClassGraph.ExpandToFullClusters(displayClassesUsedInOriginal);
            displayClassGraph.ExpandToFullClusters(displayClassesUsedInRewritten);

            foreach (var originalDisplayClass in displayClassesUsedInOriginal)
            {
                // Any body with a capture-free lambda reaches the shared cache through its singleton field, but the
                // cache belongs to the type - removing it strands every other method that caches a lambda in it.
                if (DisplayClassGraph.IsSharedLambdaCacheClass(originalDisplayClass))
                    continue;

                if (!displayClassesUsedInRewritten.Contains(originalDisplayClass) &&
                    originalMethod.DeclaringType.NestedTypes.Contains(originalDisplayClass))
                {
                    originalMethod.DeclaringType.NestedTypes.Remove(originalDisplayClass);

                    // Roslyn appends the captured frame after a local function's own parameters, so a frame taken by
                    // a leftover method can sit in any position, not just the first.
                    for (var i = typeDef.Methods.Count - 1; i >= 0; i--)
                    {
                        if (typeDef.Methods[i].Parameters.Any(
                                parameter => parameter.ParameterType.GetElementType() == originalDisplayClass))
                            typeDef.Methods.RemoveAt(i);
                    }
                }
            }
            foreach (var localFunction in localFunctionsUsedInOriginal)
                typeDef.Methods.Remove(localFunction);

            originalMethod.Body = rewrittenMethod.Body;
            typeDef.Methods.Remove(rewrittenMethod);

            var sequencePoints = originalMethod.DebugInformation.SequencePoints;
            sequencePoints.Clear();

            foreach (var sp in rewrittenMethod.DebugInformation.SequencePoints)
                sequencePoints.Add(sp);

            originalMethod.DebugInformation.Scope = rewrittenMethod.DebugInformation.Scope;

            if (rewrittenMethod.HasGenericParameters && originalMethod.HasGenericParameters)
            {
                originalMethod.GenericParameters.Clear();
                foreach (var genericParam in rewrittenMethod.GenericParameters)
                {
                    originalMethod.GenericParameters.Add(genericParam);
                }
            }
        }

        // Check both field and variable usages for display classes
        // Also scan method IL for display class or local function usages
        // (need to use heuristic since there is nothing that marks a method as a local function or display class in IL)
        static (HashSet<TypeDefinition>, HashSet<MethodDefinition>)
            GetDisplayClassAndLocalFunctionUsages(MethodDefinition method, bool alsoGetLocalFunctions)
        {
            var displayClassesUsedInMethod = new HashSet<TypeDefinition>();
            HashSet<MethodDefinition> localFunctionUsagesInMethod = null;
            if (alsoGetLocalFunctions)
                localFunctionUsagesInMethod = new HashSet<MethodDefinition>();

            // A local function can be called from another local function instead of from the method itself, and its
            // body can be the only place a closure frame is used, so keep scanning until nothing new turns up.
            var methodsToScan = new Stack<MethodDefinition>();
            methodsToScan.Push(method);

            while (methodsToScan.Count > 0)
            {
                var methodToScan = methodsToScan.Pop();
                if (!methodToScan.HasBody)
                    continue;

                foreach (var local in methodToScan.Body.Variables.Where(local => local.VariableType.IsDisplayClassCandidate()))
                    displayClassesUsedInMethod.Add(local.VariableType.Resolve());

                foreach (var instruction in methodToScan.Body.Instructions)
                {
                    if (instruction.IsLoadField() || instruction.IsLoadStaticField())
                    {
                        var fieldOperand = (FieldReference)instruction.Operand;
                        if (fieldOperand.FieldType.IsDisplayClassCandidate())
                            displayClassesUsedInMethod.Add(fieldOperand.FieldType.Resolve());
                    }
                    else if (instruction.OpCode == OpCodes.Newobj)
                    {
                        var methodOperand = (MethodReference)instruction.Operand;
                        if (methodOperand.DeclaringType.IsDisplayClassCandidate())
                            displayClassesUsedInMethod.Add(methodOperand.DeclaringType.Resolve());
                    }
                    else if (alsoGetLocalFunctions &&
                             (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt))
                    {
                        var methodOperand = (MethodReference)instruction.Operand;
                        if (methodOperand.DeclaringType.FullName == method.DeclaringType.FullName)
                        {
                            var calledMethod = methodOperand.Resolve();
                            if (calledMethod != null && calledMethod.IsLocalFunctionCandidate() &&
                                localFunctionUsagesInMethod.Add(calledMethod))
                            {
                                methodsToScan.Push(calledMethod);
                            }
                        }
                    }
                }
            }

            return (displayClassesUsedInMethod, localFunctionUsagesInMethod);
        }
    }

    // Undirected adjacency between the closure frames nested in one type, built once per type because the Cloner
    // patches every method of a type in a row. Frames are only ever removed and each method's frames form their own
    // cluster, so one graph stays valid for the whole run over a type.
    public sealed class DisplayClassGraph
    {
        readonly Dictionary<TypeDefinition, HashSet<TypeDefinition>> m_Neighbours =
            new Dictionary<TypeDefinition, HashSet<TypeDefinition>>();

        public DisplayClassGraph(TypeDefinition declaringType)
        {
            foreach (var nestedType in declaringType.NestedTypes)
            {
                if (!IsClusterableDisplayClass(nestedType))
                    continue;

                foreach (var field in nestedType.Fields)
                    AddEdge(nestedType, field.FieldType);

                foreach (var method in nestedType.Methods)
                {
                    if (!method.HasBody)
                        continue;

                    foreach (var local in method.Body.Variables)
                        AddEdge(nestedType, local.VariableType);

                    foreach (var instruction in method.Body.Instructions)
                    {
                        switch (instruction.Operand)
                        {
                            case FieldReference fieldOperand:
                                AddEdge(nestedType, fieldOperand.DeclaringType);
                                AddEdge(nestedType, fieldOperand.FieldType);
                                break;
                            case MethodReference methodOperand:
                                AddEdge(nestedType, methodOperand.DeclaringType);
                                break;
                            case TypeReference typeOperand:
                                AddEdge(nestedType, typeOperand);
                                break;
                        }
                    }
                }
            }
        }

        // Frames point at each other both ways - an inner frame holds its parent, the lambda creating an inner frame
        // lives on the outer one - so edges are undirected. The name is checked before resolving, which would
        // otherwise be paid for every type an ordinary method touches.
        void AddEdge(TypeDefinition displayClass, TypeReference typeReference)
        {
            var elementType = typeReference?.GetElementType();
            if (elementType == null || !elementType.IsDisplayClassCandidate())
                return;

            var referencedType = elementType.Resolve();
            if (referencedType == null || referencedType == displayClass || !IsClusterableDisplayClass(referencedType))
                return;

            AddNeighbour(displayClass, referencedType);
            AddNeighbour(referencedType, displayClass);
        }

        void AddNeighbour(TypeDefinition from, TypeDefinition to)
        {
            if (!m_Neighbours.TryGetValue(from, out var neighboursOfFrom))
                m_Neighbours[from] = neighboursOfFrom = new HashSet<TypeDefinition>();
            neighboursOfFrom.Add(to);
        }

        // Grows the set to the full clusters its frames belong to, so a frame is never removed while something that
        // references it is left behind.
        public void ExpandToFullClusters(HashSet<TypeDefinition> displayClasses)
        {
            if (m_Neighbours.Count == 0)
                return;

            var toVisit = new Stack<TypeDefinition>(displayClasses.Where(IsClusterableDisplayClass));
            while (toVisit.Count > 0)
            {
                if (!m_Neighbours.TryGetValue(toVisit.Pop(), out var connectedDisplayClasses))
                    continue;

                foreach (var connectedDisplayClass in connectedDisplayClasses)
                    if (displayClasses.Add(connectedDisplayClass))
                        toVisit.Push(connectedDisplayClass);
            }
        }

        static bool IsClusterableDisplayClass(TypeDefinition type) =>
            type.IsDisplayClassCandidate() && !IsSharedLambdaCacheClass(type);

        // Roslyn caches the capture-free lambdas of a whole type in a single "<>c", so it outlives any one method
        // and can neither join a method's cluster nor be removed along with one. The similar looking "<>c__N`1" is
        // per method - it exists only because a generic method needs a generic cache - and is treated like a frame.
        public static bool IsSharedLambdaCacheClass(TypeDefinition type) => type.Name == "<>c";
    }
}
