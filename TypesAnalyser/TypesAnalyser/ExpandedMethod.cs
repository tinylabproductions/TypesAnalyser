using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    private readonly com.tinylabproductions.TLPLib.Functional.Lazy<string> _name;
    public string name => _name.get;

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
      _name = F.lazy(() => {
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
      return Equals(definition, other.definition) && returnType.Equals(other.returnType) && declaringType.Equals(other.declaringType) && Equals(parameters, other.parameters);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is ExpandedMethod && Equals((ExpandedMethod) obj);
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = (definition != null ? definition.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ returnType.GetHashCode();
        hashCode = (hashCode * 397) ^ declaringType.GetHashCode();
        hashCode = (hashCode * 397) ^ (parameters != null ? parameters.GetHashCode() : 0);
        return hashCode;
      }
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
      var tpl = gRef == null
        ? F.t(callerAndDeclaringGenericParameters, ImmutableList<ExpandedType>.Empty)
        : gRef.locally(() => {
          var methodGenericArgs = genericArgumentsDict(
            gRef.GenericArguments, definition.GenericParameters,
            callerAndDeclaringGenericParameters
          );
          var elementMethodGenericArgs = genericArgumentsDict(
            gRef.GenericArguments, gRef.ElementMethod.GenericParameters,
            callerAndDeclaringGenericParameters
          );
          var _dict = callerAndDeclaringGenericParameters
            .SetItems(methodGenericArgs).SetItems(elementMethodGenericArgs);
          var _exParameters = definition.GenericParameters.Select(p =>
            ExpandedType.create(p, _dict)
          ).ToImmutableList();
          return F.t(_dict, _exParameters);
        });

      var dict = tpl._1;
      var exGenericArguments = tpl._2;
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
    private static ImmutableDictionary<GenericParameter, ExpandedType> genericArgumentsDict(
      IList<TypeReference> genericArguments,
      IList<GenericParameter> genericParameters,
      ImmutableDictionary<GenericParameter, ExpandedType> callerGenericParameters
    ) {
      if (genericParameters.Count != genericArguments.Count)
        throw new Exception("Expected generic parameter count " + genericParameters.Count +
                            " to be equal to generic argument count " + genericArguments.Count);

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
