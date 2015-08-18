using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  /* Type which is guaranteed to be non-generic and has all of its generic arguments expanded. */
  public struct ExpandedType : IEquatable<ExpandedType> {
    public static readonly ImmutableList<ExpandedType> EMPTY_GENERIC_PARAMETERS =
      ImmutableList<ExpandedType>.Empty;

    public static readonly ImmutableDictionary<GenericParameter, ExpandedType> EMPTY_GENERIC_LOOKUP =
      ImmutableDictionary<GenericParameter, ExpandedType>.Empty;

    public readonly TypeDefinition definition;

    /**
     * For types that take generic parameters, like:
     *
     * class Foo<A> {}
     *
     * This is the list of actual types used as generic arguments to those parameters.
     */
    public readonly ImmutableList<ExpandedType> genericArguments;

    /**
     * For types that take generic parameters, like:
     *
     * class Foo<A> {}
     *
     * This map resolves A to real type that is being used.
     */
    public readonly ImmutableDictionary<GenericParameter, ExpandedType> genericParametersToArguments;

    private readonly com.tinylabproductions.TLPLib.Functional.Lazy<string> _name;
    public string name => _name.get;

    public ExpandedType(
      TypeDefinition definition,
      ImmutableList<ExpandedType> genericArguments,
      ImmutableDictionary<GenericParameter, ExpandedType> genericParametersToArguments
    ) : this() {
      Debug.Assert(definition != null, "definition != null");

      this.definition = definition;
      this.genericArguments = genericArguments;
      this.genericParametersToArguments = genericParametersToArguments;
      _name = F.lazy(() => {
        var sb = new StringBuilder();
        sb.Append(definition.FullName);
        if (!genericArguments.isEmpty()) sb.Append(genericArguments.mkString(", ", "<", ">"));
        return sb.ToString();
      });
    }

    #region Equality

    public bool Equals(ExpandedType other) {
      return Equals(definition, other.definition) && Equals(genericArguments, other.genericArguments);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is ExpandedType && Equals((ExpandedType) obj);
    }

    public override int GetHashCode() {
      unchecked { return ((definition != null ? definition.GetHashCode() : 0) * 397) ^ (genericArguments != null ? genericArguments.GetHashCode() : 0); }
    }

    public static bool operator ==(ExpandedType left, ExpandedType right) { return left.Equals(right); }
    public static bool operator !=(ExpandedType left, ExpandedType right) { return !left.Equals(right); }

    #endregion

    public override string ToString() { return _name.get; }

    public static ExpandedType create(
      TypeReference _reference, IImmutableDictionary<GenericParameter, ExpandedType> generics
    ) {
      var reference = _reference.IsByReference ? ((ByReferenceType) _reference).ElementType : _reference;
      var definition = reference.Resolve();
      if (reference.IsGenericInstance) {
        var gRef = (GenericInstanceType) reference;
        var parameters = ImmutableList<ExpandedType>.Empty;
        var dict = ImmutableDictionary<GenericParameter, ExpandedType>.Empty;

        for (var idx = 0; idx < gRef.GenericArguments.Count; idx++) {
          var arg = gRef.GenericArguments[idx];
          var gArg = arg as GenericParameter;
          var exParam = gArg != null ? generics[gArg] : create(arg, generics);
          parameters = parameters.Add(exParam);
          dict = dict.Add(definition.GenericParameters[idx], exParam);
        }

        return new ExpandedType(definition, parameters, dict);
      }
      if (reference.IsGenericParameter) {
        var gParam = (GenericParameter) reference;
        return generics[gParam];
      }
      return new ExpandedType(definition, EMPTY_GENERIC_PARAMETERS, EMPTY_GENERIC_LOOKUP);
    }

    public bool implements(TypeDefinition iface) { return definition.implements(iface); }
  }
}
