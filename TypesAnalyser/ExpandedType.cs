using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  /* Type which is guaranteed to be non-generic and has all of its generic arguments expanded. */
  struct ExpandedType {
    public static readonly ImmutableList<ExpandedType> EMPTY_GENERIC_PARAMETERS =
      ImmutableList<ExpandedType>.Empty;

    public readonly TypeDefinition definition;
    public readonly ImmutableList<ExpandedType> genericParameters;

    private readonly com.tinylabproductions.TLPLib.Functional.Lazy<string> _name;
    public string name => _name.get;

    public ExpandedType(
      TypeDefinition definition,
      ImmutableList<ExpandedType> genericParameters
    ) : this() {
      this.definition = definition;
      this.genericParameters = genericParameters;
      _name = F.lazy(() => {
        var sb = new StringBuilder();
        sb.Append(definition.FullName);
        if (!genericParameters.isEmpty()) sb.Append(genericParameters.mkString(",", "<", ">"));
        return sb.ToString();
      });
    }

    #region Equality

    public bool Equals(ExpandedType other) {
      return Equals(_name, other._name);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is ExpandedType && Equals((ExpandedType) obj);
    }

    public override int GetHashCode() {
      return _name?.GetHashCode() ?? 0;
    }

    #endregion

    public override string ToString() { return _name.get; }

    public static Option<ExpandedType> a(TypeDefinition definition) {
      if (definition.IsGenericParameter || definition.IsGenericInstance) return F.none<ExpandedType>();
      return F.some(new ExpandedType(definition, EMPTY_GENERIC_PARAMETERS));
    }

    public static ExpandedType create(TypeDefinition definition) {
      return a(definition).
        getOrThrow(() => new Exception("Expected " + definition + " to be non-generic"));
    }

    public static ExpandedType create(
      TypeReference reference, IDictionary<GenericParameter, ExpandedType> generics
    ) {
      var definition = reference.Resolve();
      if (reference.IsGenericInstance) {
        var gRef = (GenericInstanceType) reference;
        var parameters = gRef.GenericArguments.Select(arg => 
          generics[(GenericParameter) arg]
        ).ToImmutableList();
        return new ExpandedType(definition, parameters);
      }
      if (reference.IsGenericParameter) {
        var gParam = (GenericParameter) reference;
        return generics[gParam];
      }
      return create(definition);
    }
  }
}
