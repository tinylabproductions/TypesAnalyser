using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TypesAnalyser {
  public struct AnalyzerData {
    public readonly ImmutableHashSet<ExpandedType> usedTypes;
    public readonly ImmutableHashSet<ExpandedMethod> analyzedMethods;
    public readonly ImmutableHashSet<VirtualMethod> virtualMethodsToAnalyze, analyzedVirtualMethods;

    public AnalyzerData(
      ImmutableHashSet<ExpandedType> usedTypes, ImmutableHashSet<ExpandedMethod> analyzedMethods, 
      ImmutableHashSet<VirtualMethod> virtualMethodsToAnalyze, 
      ImmutableHashSet<VirtualMethod> analyzedVirtualMethods
    ) {
      this.usedTypes = usedTypes;
      this.analyzedMethods = analyzedMethods;
      this.virtualMethodsToAnalyze = virtualMethodsToAnalyze;
      this.analyzedVirtualMethods = analyzedVirtualMethods;
    }

    public bool hasType(ExpandedType type) { return usedTypes.Contains(type); }
    public bool hasMethod(ExpandedMethod method) { return analyzedMethods.Contains(method); }

    public AnalyzerData addType(ExpandedType type) {
      return hasType(type)
        ? this : new AnalyzerData(usedTypes.Add(type), analyzedMethods, virtualMethodsToAnalyze, analyzedVirtualMethods);
    }

    public AnalyzerData addMethod(ExpandedMethod method) {
      return hasMethod(method)
        ? this : new AnalyzerData(usedTypes, analyzedMethods.Add(method), virtualMethodsToAnalyze, analyzedVirtualMethods);
    }

    public AnalyzerData addVirtualMethod(VirtualMethod method) {
      return virtualMethodsToAnalyze.Contains(method) || analyzedVirtualMethods.Contains(method)
        ? this : new AnalyzerData(usedTypes, analyzedMethods, virtualMethodsToAnalyze.Add(method), analyzedVirtualMethods);
    }

    public AnalyzerData analyzedVirtualMethod(VirtualMethod virtualMethod) {
      return new AnalyzerData(
        usedTypes, analyzedMethods, 
        virtualMethodsToAnalyze.Remove(virtualMethod), analyzedVirtualMethods.Add(virtualMethod)
      );
    }
  }

  public static class Analyser {
    public static AnalyzerData analyze(
      string fileName, 
      Fn<TypeDefinition, Option<IEntryPoint>> entryPointLookuper,
      AnalyserLogger log
    ) {
      var assembly = AssemblyDefinition.ReadAssembly(fileName);
      return analyze(assembly, entryPointLookuper, log);
    }

    public static AnalyzerData analyze(
      AssemblyDefinition assembly,
      Fn<TypeDefinition, Option<IEntryPoint>> entryPointLookuper,
      AnalyserLogger log
    ) {
      var types = allTypes(assembly).ToImmutableList();
//      var abstractImplementations = AbstractTypeImplementations.create(types);

      var entryMethods = types.SelectMany(_ => entryPointLookuper(_).asEnum)
        .SelectMany(ep => ep.entryMethods);
      return analyze(entryMethods, log);
    }

    public static AnalyzerData analyze(
      IEnumerable<MethodDefinition> entryPoints,
      AnalyserLogger log
    ) {
      var data = new AnalyzerData(
        ImmutableHashSet<ExpandedType>.Empty, ImmutableHashSet<ExpandedMethod>.Empty,
        ImmutableHashSet<VirtualMethod>.Empty, ImmutableHashSet<VirtualMethod>.Empty
      );
      foreach (var entryMethod in entryPoints) {
        var exEntryMethod = ExpandedMethod.create(
          entryMethod, ExpandedType.EMPTY_GENERIC_LOOKUP
        );
        data = data.addType(exEntryMethod.declaringType);
        if (!data.hasMethod(exEntryMethod)) {
          log.log("Entry", exEntryMethod);
          data = analyze(data, exEntryMethod, log);
        }
      }

      // Once we know all used types, resolve the called virtual methods.
      // Iterate in a loop because expanding virtual method bodies might invoke additional
      // virtual methods.
      while (!data.virtualMethodsToAnalyze.IsEmpty) {
        foreach (var virtualMethod in data.virtualMethodsToAnalyze) {
          var declaring = virtualMethod.declaringType;
          foreach (var _usedType in data.usedTypes.Where(et => et.implements(declaring))) {
            var usedType = _usedType;
            MethodDefinition matching = null;
            while (matching == null) {
              matching = MetadataResolver.GetMethod(usedType.definition.Methods, virtualMethod.definition);
              if (matching == null) {
                if (usedType.definition.BaseType == null) throw new Exception(
                  "Can't find implementation for [" + virtualMethod + "] in [" + _usedType + "]!"
                );
                usedType = ExpandedType.create(
                  usedType.definition.BaseType,
                  usedType.genericParametersToArguments
                );
              }
            }
            
            var exMatching = ExpandedMethod.create(
              usedType, matching,
              ImmutableDictionary<GenericParameter, ExpandedType>.Empty
            );
            data = analyze(data, exMatching, log);
          }
          data = data.analyzedVirtualMethod(virtualMethod);
        }
      }
      return data;
    }

    public static IEnumerable<ModuleDefinition> allModules(AssemblyDefinition assembly) {
      return assembly.Modules.SelectMany(module => {
        var resolver = module.AssemblyResolver;
        return (new[] { module }).Concat(module.AssemblyReferences.SelectMany(reference => 
          resolver.Resolve(reference).Modules
        ));
      });
    }

    public static IEnumerable<TypeDefinition> allTypes(AssemblyDefinition assembly) {
      return allModules(assembly).SelectMany(m => m.Types);
    }

    static AnalyzerData analyze(
      AnalyzerData data, ExpandedMethod method, AnalyserLogger log
    ) {
      if (data.hasMethod(method)) return data;
      log.log("method", method);
      log.incIndent();

      data = data.addMethod(method);

      if (method.definition.IsConstructor) data = data.addType(method.declaringType);
      data = data.addType(method.returnType);
      data = method.parameters.Aggregate(data, (current, parameter) => current.addType(parameter));

      if (method.definition.IsVirtual && method.definition.Body == null) {
        // Because we don't know with what concrete types the implementation classes 
        // of virtual methods will be instantiated, we will need to do a separate 
        // analyzing stage once we know all the concrete types.
        data = data.addType(method.declaringType);
        data = data.addVirtualMethod(new VirtualMethod(method));
      }
      else if (method.definition.Body != null) {
        var body = method.definition.Body;
        foreach (var varDef in body.Variables) {
          var exVar = ExpandedType.create(varDef.VariableType, method.genericParameterToExType);
          data = data.addType(exVar);
        }
        foreach (var instruction in body.Instructions) {
          if (
            instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld
            || instruction.OpCode == OpCodes.Ldsfld
          ) {
            var fieldDef = (FieldReference) instruction.Operand;
            var fieldGenerics = method.genericParameterToExType;
            if (fieldDef.FieldType.IsNested) {
              if (fieldDef.FieldType.IsGenericParameter) {
                var generics = ExpandedMethod.genericArgumentsDict(
                  ((GenericInstanceType) fieldDef.DeclaringType).GenericArguments,
                  fieldDef.FieldType.DeclaringType.GenericParameters,
                  fieldGenerics
                );
                fieldGenerics = fieldGenerics.AddRange(generics);
              }
              if (fieldDef.FieldType.IsGenericInstance) {
                var generics = ExpandedMethod.genericArgumentsDict(
                  ((GenericInstanceType)fieldDef.FieldType).GenericArguments,
                  fieldDef.FieldType.GetElementType().GenericParameters,
                  fieldGenerics
                );
                fieldGenerics = fieldGenerics.AddRange(generics);
              }
            }
            var exFieldDef = ExpandedType.create(fieldDef.FieldType, fieldGenerics);
            var exDeclaringType = ExpandedType.create(fieldDef.DeclaringType, method.genericParameterToExType);
            data = data.addType(exFieldDef).addType(exDeclaringType);
          }
          else if (
            instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli
            || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Newobj
          ) {
            var methodRef = (MethodReference)instruction.Operand;
            if (instruction.OpCode == OpCodes.Newobj && methodRef.DeclaringType.Resolve().isDelegate()) {
              methodRef = (MethodReference) instruction.Previous.Operand;
            }
            var expanded = ExpandedMethod.create(methodRef, method.genericParameterToExType);
            data = analyze(data, expanded, log);
          }
        }
      }
      else if (! (method.definition.IsInternalCall || method.definition.IsPInvokeImpl)) {
        throw new Exception("Method body null when not expected: " + method);
      }
      log.decIndent();
      return data;
    }
  }
}
