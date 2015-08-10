using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace TypesAnalyser {
  class Program {
    static void Main(string[] args) {
      var program = new Program();
      foreach (var arg in args) {
        Console.WriteLine("Analyzing " + arg);
        program.analyze(arg);
      }
      foreach (var type in program.usedTypes) {
        Console.WriteLine("used type: " + type);
      }
      Console.WriteLine("Done. Press any key to exit.");
      Console.Read();
    }

    readonly Dictionary<string, ExpandedType> _usedTypes = new Dictionary<string, ExpandedType>();
    public IEnumerable<KeyValuePair<string, ExpandedType>> usedTypes => _usedTypes;

    readonly HashSet<ExpandedMethod> analyzedMethods = new HashSet<ExpandedMethod>();

    uint currentIndent;
    void incIndent() { currentIndent += 2; }
    void decIndent() { currentIndent -= 2; }

    bool hasType(ExpandedType type) { return _usedTypes.ContainsKey(type.name); }

    void addType(ExpandedType type) {
      if (! _usedTypes.ContainsKey(type.name)) _usedTypes.Add(type.name, type);
    }

    void analyze(string fileName) {
      var module = ModuleDefinition.ReadModule(fileName);
      var entryPoints = module.Types.SelectMany(_ => UnityEntryPoint.create(_).asEnum);
      foreach (var entryPoint in entryPoints) {
        log(entryPoint.type, "entry");
        analyzeEntryPoint(entryPoint);
      }
    }

//    void analyze(TypeReference type, IDictionary<string, TypeDefinition> genericArguments) {
//      analyze(type.Resolve(), genericArguments);
//    }

    void analyzeEntryPoint(UnityEntryPoint entry) {
      if (hasType(entry.type)) return;
      incIndent();

      addType(entry.type);
      log(entry.type, "analyze");
      
      foreach (var method in entry.type.definition.Methods)
        analyze(ExpandedMethod.create(
          entry.type, method, ImmutableDictionary<GenericParameter, ExpandedType>.Empty
        ));
      decIndent();
    }

//    void analyze(MethodReference method) {
//      var resolved = method.Resolve();
//
//      var genericInstanceMethod = method as GenericInstanceMethod;
//      if (genericInstanceMethod != null) {
//        analyze(
//          resolved,
//          // TODO: merge with top generic parameters
//          genericArgumentsDict(resolved.GenericParameters, genericInstanceMethod.GenericArguments)
//        );
//      }
//      else {
//        analyze(resolved);
//      }
//    }

    void analyze(ExpandedMethod method) {
      if (analyzedMethods.Contains(method)) return;
      incIndent();

      analyzedMethods.Add(method);
      log("method: " + method);

      addType(method.returnType);
      foreach (var parameter in method.parameters) addType(parameter);
      
//      foreach (var parameter in method.Parameters) {
//        //        log("parameter: " + parameter + " " + parameter.ParameterType);
//        var pTypeRef = parameter.ParameterType;
//        var pType = pTypeRef.IsGenericParameter
//          ? genericArguments[pTypeRef.FullName]
//          : pTypeRef.Resolve();
//        analyze(pType, genericArguments);
//      }

      if (method.definition.Body != null) {
        var body = method.definition.Body;
        foreach (var instruction in body.Instructions) {
          if (
            instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli
            || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Newobj
          ) {
            var methodRef = (MethodReference)instruction.Operand;
            var expanded = ExpandedMethod.create(method.declaringType, methodRef, method.genericParameters);
            analyze(expanded);
          }
        }
      }
      else {
        log("!! method body null " + method);
      }
      decIndent();
    }

    static void indentLog(uint indentation) {
      var sb = new StringBuilder();
      for (var idx = 0u; idx < indentation; idx++) sb.Append(' ');
      Console.Write(sb.ToString());
    }

    void log(ExpandedType type, string msg) { log("[" + type.name + "] " + msg); }
    void log(string msg) {
      indentLog(currentIndent);
      Console.WriteLine(msg);
    }
  }
}
