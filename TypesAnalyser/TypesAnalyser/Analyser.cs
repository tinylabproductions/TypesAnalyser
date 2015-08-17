using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace TypesAnalyser {
  public struct AnalyzerData {
    public readonly ImmutableHashSet<ExpandedType> usedTypes;
    public readonly ImmutableHashSet<ExpandedMethod> analyzedMethods;
    public readonly ImmutableHashSet<MethodDefinition> calledVirtualMethods;

    public AnalyzerData(
      ImmutableHashSet<ExpandedType> usedTypes, ImmutableHashSet<ExpandedMethod> analyzedMethods, 
      ImmutableHashSet<MethodDefinition> calledVirtualMethods
    ) {
      this.usedTypes = usedTypes;
      this.analyzedMethods = analyzedMethods;
      this.calledVirtualMethods = calledVirtualMethods;
    }

    public bool hasType(ExpandedType type) { return usedTypes.Contains(type); }
    public bool hasMethod(ExpandedMethod method) { return analyzedMethods.Contains(method); }

    public AnalyzerData addType(ExpandedType type) {
      return hasType(type)
        ? this : new AnalyzerData(usedTypes.Add(type), analyzedMethods, calledVirtualMethods);
    }

    public AnalyzerData addMethod(ExpandedMethod method) {
      return hasMethod(method)
        ? this : new AnalyzerData(usedTypes, analyzedMethods.Add(method), calledVirtualMethods);
    }

    public AnalyzerData addVirtualMethod(MethodDefinition method) {
      return calledVirtualMethods.Contains(method)
        ? this : new AnalyzerData(usedTypes, analyzedMethods, calledVirtualMethods.Add(method));
    }
  }

  public interface AnalyserLogger {
    void incIndent();
    void decIndent();
    void log(string prefix, ExpandedMethod method);
    void log(ExpandedType type, string msg);
    void log(string msg);
  }

  public class StdoutLogger : AnalyserLogger {
    uint indent;

    public void incIndent() { indent += 2; }
    public void decIndent() { indent -= 2; }
    public void log(string prefix, ExpandedMethod method) { log(prefix + " " + method); }
    public void log(ExpandedType type, string msg) { log("[" + type.name + "] " + msg); }
    public void log(string msg) {
      indentLog(indent);
      Console.WriteLine(msg);
    }

    static void indentLog(uint indentation) {
      var sb = new StringBuilder();
      for (var idx = 0u; idx < indentation; idx++) sb.Append(' ');
      Console.Write(sb.ToString());
    }
  }

  public class NoopLogger : AnalyserLogger {
    public void incIndent() {}
    public void decIndent() {}
    public void log(string prefix, ExpandedMethod method) {}
    public void log(ExpandedType type, string msg) {}
    public void log(string msg) {}
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
        ImmutableHashSet<MethodDefinition>.Empty
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
      log.incIndent();

      data = data.addMethod(method);
      log.log("method", method);

      if (method.definition.IsConstructor) data = data.addType(method.declaringType);
      data = data.addType(method.returnType);
      data = method.parameters.Aggregate(data, (current, parameter) => current.addType(parameter));

      if (method.definition.Body != null) {
        var body = method.definition.Body;
        foreach (var instruction in body.Instructions) {
          if (
            instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli
            || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Newobj
          ) {
            var methodRef = (MethodReference)instruction.Operand;
            var expanded = ExpandedMethod.create(methodRef, method.genericParameterToExType);
            data = analyze(data, expanded, log);
          }
        }
      }
      else if (method.definition.IsVirtual) {
        // Because we don't know with what concrete types the implementation classes 
        // of virtual methods will be instantiated, we will need to do a separate 
        // analyzing stage once we know all the concrete types.
        data = data.addVirtualMethod(method.definition);
      }
      else if (! method.definition.IsInternalCall) {
        throw new Exception("Method body null when not expected: " + method);
      }
      log.decIndent();
      return data;
    }
  }
}
