using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using com.tinylabproductions.TLPLib.Extensions;
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

    public readonly bool isArray;

    private readonly string _name;
    public string name => _name;

    public ExpandedType(
      TypeDefinition definition, bool isArray,
      ImmutableList<ExpandedType> genericArguments,
      ImmutableDictionary<GenericParameter, ExpandedType> genericParametersToArguments
    ) : this() {
      Debug.Assert(definition != null, "definition != null");

      this.definition = definition;
      this.isArray = isArray;
      this.genericArguments = genericArguments;
      this.genericParametersToArguments = genericParametersToArguments;
      _name = definition.locally(() => {
        var sb = new StringBuilder();
        sb.Append(definition.FullName);
        if (!genericArguments.isEmpty()) sb.Append(genericArguments.mkString(", ", "<", ">"));
        if (isArray) sb.Append("[]");
        return sb.ToString();
      });
    }

    ExpandedType withIsArray(bool isArray) {
      return new ExpandedType(
        definition, isArray, genericArguments, genericParametersToArguments
      );
    }

    #region Equality

    public bool Equals(ExpandedType other) {
      return string.Equals(_name, other._name);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is ExpandedType && Equals((ExpandedType) obj);
    }

    public override int GetHashCode() {
      return (_name != null ? _name.GetHashCode() : 0);
    }

    public static bool operator ==(ExpandedType left, ExpandedType right) { return left.Equals(right); }
    public static bool operator !=(ExpandedType left, ExpandedType right) { return !left.Equals(right); }

    #endregion

    public override string ToString() { return _name; }

    public static ExpandedType create(
      TypeReference _reference, IImmutableDictionary<GenericParameter, ExpandedType> generics
    ) {
      var reference = _reference;
      var isArray = false;
      if (reference.IsByReference)
        reference = ((ByReferenceType) reference).ElementType;
      if (reference.IsArray) {
        isArray = true;
        reference = ((ArrayType) reference).ElementType;
      }
        
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

        return new ExpandedType(definition, isArray, parameters, dict);
      }
      if (reference.IsGenericParameter) {
        var gParam = (GenericParameter) reference;
        return generics[gParam].withIsArray(isArray);
      }
      return new ExpandedType(definition, isArray, EMPTY_GENERIC_PARAMETERS, EMPTY_GENERIC_LOOKUP);
    }

    public bool implements(ExpandedType iface) {
      if (definition.IsInterface) return false;
      var genericParametersToArguments = this.genericParametersToArguments;
      if (definition.Interfaces.Any(i => 
        create(i, genericParametersToArguments) == iface
      )) return true;
      if (definition.BaseType == null) return false;
      var baseType = create(definition.BaseType, genericParametersToArguments);
      return baseType == iface || baseType.implements(iface);
    }
  }
}
