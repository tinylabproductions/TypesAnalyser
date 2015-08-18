using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  /* Method which is guaranteed to be non-generic and has all of its generic arguments expanded. */
  public struct ExpandedMethod : IEquatable<ExpandedMethod> {
    public readonly MethodDefinition definition;
    public readonly ExpandedType returnType, declaringType;
    public readonly ImmutableList<ExpandedType> genericArguments, parameters;
    public readonly ImmutableDictionary<GenericParameter, ExpandedType> genericParameterToExType;

    private readonly string _name;
    public string name => _name;

    public ExpandedMethod(
      ExpandedType returnType, ExpandedType declaringType, MethodDefinition definition,
      ImmutableList<ExpandedType> genericArguments, ImmutableList<ExpandedType> parameters, 
      ImmutableDictionary<GenericParameter, ExpandedType> genericParameterToExType
    ) {
      this.returnType = returnType;
      this.declaringType = declaringType;
      this.definition = definition;
      this.genericArguments = genericArguments;
      this.parameters = parameters;
      this.genericParameterToExType = genericParameterToExType;
      _name = declaringType.locally(() => {
        var sb = new StringBuilder();
        sb.Append(returnType);
        sb.Append(' ');
        sb.Append(declaringType);
        sb.Append("::");
        sb.Append(definition.Name);
        if (!genericArguments.IsEmpty) sb.Append(genericArguments.mkString(", ", "<", ">"));
        sb.Append(parameters.mkString(", ", "(", ")"));
        return sb.ToString();
      });
    }

    public override string ToString() { return name; }

    #region Equality

    public bool Equals(ExpandedMethod other) {
      return string.Equals(_name, other._name);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is ExpandedMethod && Equals((ExpandedMethod) obj);
    }

    public override int GetHashCode() {
      return (_name != null ? _name.GetHashCode() : 0);
    }

    public static bool operator ==(ExpandedMethod left, ExpandedMethod right) { return left.Equals(right); }
    public static bool operator !=(ExpandedMethod left, ExpandedMethod right) { return !left.Equals(right); }

    #endregion

    public static ExpandedMethod create(
      MethodReference reference,
      ImmutableDictionary<GenericParameter, ExpandedType> callerGenericParameters
    ) {
      var declaringType = ExpandedType.create(reference.DeclaringType, callerGenericParameters);
      return create(declaringType, reference, callerGenericParameters);
    }

    // Optimization for when we already know the expanded declaring type.
    public static ExpandedMethod create(
      ExpandedType declaringType, MethodReference reference, 
      ImmutableDictionary<GenericParameter, ExpandedType> callerGenericParameters
    ) {
      var callerAndDeclaringGenericParameters = 
        callerGenericParameters.SetItems(declaringType.genericParametersToArguments);

      var definition = reference.Resolve();

      // Generic method, like A identity<A>(A a);
      var gRef = reference as GenericInstanceMethod;
      var gRefTpl = gRef == null
        ? F.t(ImmutableDictionary<GenericParameter, ExpandedType>.Empty, ImmutableList<ExpandedType>.Empty)
        : gRef.locally(() => {
          var methodGenericArgs = genericArgumentsDict(
            gRef.GenericArguments, definition.GenericParameters,
            callerAndDeclaringGenericParameters
          );
          var elementMethodGenericArgs = genericArgumentsDict(
            gRef.GenericArguments, gRef.ElementMethod.GenericParameters,
            callerAndDeclaringGenericParameters
          );
          var _dict = methodGenericArgs.SetItems(elementMethodGenericArgs);
          var _exParameters = definition.GenericParameters.Select(p =>
            ExpandedType.create(p, _dict)
          ).ToImmutableList();
          return F.t(_dict, _exParameters);
        });

      // Delegates have their arguments messed up somehow. The parameters take 
      // form of !!0, instead of A.
      var declGRef = reference.DeclaringType as GenericInstanceType;
      var declGRefGenericArgs = declGRef == null
        ? ImmutableDictionary<GenericParameter, ExpandedType>.Empty
        : genericArgumentsDict(
            declGRef.GenericArguments, declGRef.ElementType.GenericParameters,
            callerAndDeclaringGenericParameters
          );

      var dict = callerAndDeclaringGenericParameters.SetItems(gRefTpl._1).SetItems(declGRefGenericArgs);
      var exGenericArguments = gRefTpl._2;
      var exParameters = reference.Parameters.Select(p => {
        var gp = p.ParameterType as GenericParameter;
        return gp == null
          ? ExpandedType.create(p.ParameterType, dict)
          : dict[gp];
      }).ToImmutableList();
      var exReturnType = ExpandedType.create(definition.ReturnType, dict);

      return new ExpandedMethod(
        exReturnType, declaringType, reference.Resolve(), 
        exGenericArguments, exParameters, dict
      );
    }

    /* Make a dict from generic parameter name to resolved type definition. */
    public static ImmutableDictionary<GenericParameter, ExpandedType> genericArgumentsDict(
      IList<TypeReference> genericArguments,
      IList<GenericParameter> genericParameters,
      ImmutableDictionary<GenericParameter, ExpandedType> callerGenericParameters
    ) {
      Debug.Assert(
        genericParameters.Count == genericArguments.Count,
        "Expected generic parameter count " + genericParameters.Count +
        " to be equal to generic argument count " + genericArguments.Count
      );

      var dict = ImmutableDictionary<GenericParameter, ExpandedType>.Empty;
      for (var idx = 0; idx < genericParameters.Count; idx++) {
        var param = genericParameters[idx];
        var arg = genericArguments[idx];
        dict = dict.Add(param, ExpandedType.create(arg, callerGenericParameters));
      }
      return dict;
    }
  }
}
