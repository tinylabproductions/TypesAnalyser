using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  /* Method which is guaranteed to be non-generic and has all of its generic arguments expanded. */
  struct ExpandedMethod {
    public readonly MethodDefinition definition;
    public readonly ExpandedType returnType, declaringType;
    public readonly ReadOnlyCollection<ExpandedType> parameters;
    public readonly ImmutableDictionary<GenericParameter, ExpandedType> genericParameters;

    private readonly com.tinylabproductions.TLPLib.Functional.Lazy<string> _name;
    public string name => _name.get;

    public ExpandedMethod(
      ExpandedType returnType, ExpandedType declaringType, MethodDefinition definition, 
      ReadOnlyCollection<ExpandedType> parameters, 
      ImmutableDictionary<GenericParameter, ExpandedType> genericParameters
    ) {
      this.returnType = returnType;
      this.declaringType = declaringType;
      this.definition = definition;
      this.parameters = parameters;
      this.genericParameters = genericParameters;
      _name = F.lazy(() => {
        var sb = new StringBuilder();
        sb.Append(returnType);
        sb.Append(' ');
        sb.Append(declaringType);
        sb.Append("::");
        sb.Append(definition.Name);
        sb.Append(parameters.mkString(", ", "(", ")"));
        return sb.ToString();
      });
    }

    public override string ToString() { return name; }

    #region Equality

    public bool Equals(ExpandedMethod other) {
      return Equals(_name, other._name);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is ExpandedMethod && Equals((ExpandedMethod) obj);
    }

    public override int GetHashCode() {
      return (_name != null ? _name.GetHashCode() : 0);
    }

    #endregion

    public static ExpandedMethod create(
      ExpandedType declaringType, MethodReference reference, 
      ImmutableDictionary<GenericParameter, ExpandedType> callerGenericParameters
    ) {
      var definition = reference.Resolve();
      // Generic method, like A identity<A>(A a);
      if (reference.IsGenericInstance) {
        var gRef = reference as GenericInstanceMethod;
        Debug.Assert(gRef != null, "gRef != null");
        var dict = callerGenericParameters.AddRange(genericArgumentsDict(gRef));
        var exReturnType = ExpandedType.create(gRef.ReturnType, dict);
        var exParameters = gRef.Parameters.Select(p =>
          ExpandedType.create(p.ParameterType, dict)
        ).ToList().AsReadOnly();
        return new ExpandedMethod(
          exReturnType, declaringType, definition, exParameters, dict
        );
      }
      if (reference.DeclaringType.IsGenericInstance) {
        var refDeclType = (GenericInstanceType) reference.DeclaringType;
        var genericParameters = refDeclType.GenericArguments.Cast<GenericParameter>().
          Select(p => callerGenericParameters[p]).ToImmutableList();
        var refExDeclType = new ExpandedType(refDeclType.Resolve(), genericParameters);
        throw new Exception();
      }
      if (definition.HasGenericParameters || definition.IsGenericInstance)
        throw new Exception("Unhandled case: " + reference);
      // TODO: handle class constructor with generic arg
      // callerGenericParameters[((GenericInstanceType)reference.DeclaringType).GenericArguments[0] as GenericParameter]
      return new ExpandedMethod(
        ExpandedType.create(definition.ReturnType.Resolve()),
        declaringType, definition.Resolve(),
        definition.Parameters.Select(p =>
          ExpandedType.create(p.ParameterType.Resolve())
        ).ToList().AsReadOnly(), 
        callerGenericParameters
      );
    }

    /* Make a dict from generic parameter name to resolved type definition. */
    private static ImmutableDictionary<GenericParameter, ExpandedType> genericArgumentsDict(
      GenericInstanceMethod gRef
    ) {
      var genericParameters = gRef.ElementMethod.GenericParameters;
      var genericArguments = gRef.GenericArguments;

      if (genericParameters.Count != genericArguments.Count)
        throw new Exception("Expected generic parameter count " + genericParameters.Count +
                            " to be equal to generic argument count " + genericArguments.Count);

      var dict = ImmutableDictionary<GenericParameter, ExpandedType>.Empty;
      for (var idx = 0; idx < genericParameters.Count; idx++) {
        var param = genericParameters[idx];
        var arg = genericArguments[idx];
        dict = dict.Add(param, ExpandedType.create(arg.Resolve()));
      }
      return dict;
    }
  }
}
