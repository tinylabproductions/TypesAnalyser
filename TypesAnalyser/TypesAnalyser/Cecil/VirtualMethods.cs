using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace TypesAnalyser.Cecil {
  public static class VirtualMethods {
    // These are private in MetadataResolver so we get them out via reflection... Yay :/
    static readonly MethodInfo _areSameRT, _areSameParams;

    static VirtualMethods() {
      var mr = typeof(MetadataResolver);
      var t_tr = typeof(TypeReference);
      var t_cpd = typeof(Collection<ParameterDefinition>);
      Fn<Type[], MethodInfo> _getM = types => mr.GetMethod(
        "AreSame", BindingFlags.Static | BindingFlags.NonPublic,
        null, CallingConventions.Any, types, null
      );
      _areSameRT = _getM(new[] { t_tr, t_tr });
      _areSameParams = _getM(new[] { t_cpd, t_cpd });
    }

    public static bool AreSame(TypeReference a, TypeReference b) {
      return (bool) _areSameRT.Invoke(null, new object[] { a, b });
    }

    public static bool AreSame(Collection<ParameterDefinition> a, Collection<ParameterDefinition> b) {
      return (bool)_areSameParams.Invoke(null, new object[] { a, b });
    }

    /**
     * Matches implementation method by given reference method. 
     *
     * Copy from MetadataResolver.GetMethod, but supports explicit implementations.
     */
    public static MethodDefinition GetMethod(Collection<MethodDefinition> methods, MethodReference reference) {
      for (var index = 0; index < methods.Count; ++index) {
        var methodDefinition = methods[index];
        if (
          (
            methodDefinition.Name == reference.Name 
            // Find explicit implementations as well.
            || degenerifyMethodName(methodDefinition.Name) == $"{reference.DeclaringType.FullName}.{reference.Name}"
          ) && 
          methodDefinition.HasGenericParameters == reference.HasGenericParameters && 
          (
            !methodDefinition.HasGenericParameters 
            || methodDefinition.GenericParameters.Count == reference.GenericParameters.Count
          ) && (
            AreSame(methodDefinition.ReturnType, reference.ReturnType) 
            && methodDefinition.HasParameters == reference.HasParameters
          ) && (
            !methodDefinition.HasParameters && !reference.HasParameters 
            || AreSame(methodDefinition.Parameters, reference.Parameters)
          )
        ) return methodDefinition;
      }
      return null;
    }

    public static Option<ExpandedMethod> GetMethod(
      ExpandedType type, VirtualMethod virtualMethod
    ) {
      var virtMethRef = virtualMethod.method.definition;
      foreach (var method in type.methods) {
        var methodDefinition = method.definition;
        if (
          (
            methodDefinition.Name == virtMethRef.Name 
            // Find explicit implementations as well.
            || degenerifyMethodName(methodDefinition.Name) == $"{virtMethRef.DeclaringType.FullName}.{virtMethRef.Name}"
          ) && 
          methodDefinition.HasGenericParameters == virtMethRef.HasGenericParameters && 
          (
            !methodDefinition.HasGenericParameters 
            || methodDefinition.GenericParameters.Count == virtMethRef.GenericParameters.Count
          ) && (
//            AreSame(methodDefinition.ReturnType, virtMethRef.ReturnType) 
            method.returnType == virtualMethod.method.returnType
            && methodDefinition.HasParameters == virtMethRef.HasParameters
          ) && (
            !methodDefinition.HasParameters && !virtMethRef.HasParameters 
            || method.parameters.SequenceEqual(virtualMethod.method.parameters)
          )
        ) return F.some(method);
      }
      return F.none<ExpandedMethod>();
    }

    public static string degenerifyMethodName(string name) {
      var idx = name.LastIndexOf(">.", StringComparison.Ordinal);
      if (idx == -1) return name;

      var endIdx = idx;
      var commas = 0;
      var brackets = 1;
      for (idx--; brackets != 0 && idx >= 0; idx--) {
        var c = name[idx];
        switch (c) {
          case '>':
            brackets++;
            break;
          case '<':
            brackets--;
            break;
          case ',':
            commas++;
            break;
        }
      }
      return $"{name.Substring(0, idx + 1)}`{commas + 1}{name.Substring(endIdx + 1)}";
    }
  }
}
