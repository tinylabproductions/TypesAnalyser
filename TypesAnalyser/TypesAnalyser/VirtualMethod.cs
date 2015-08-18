using System;
using Mono.Cecil;

namespace TypesAnalyser {
  public struct VirtualMethod : IEquatable<VirtualMethod> {
    public readonly ExpandedType declaringType;
    public readonly MethodDefinition definition;

    public VirtualMethod(ExpandedType declaringType, MethodDefinition definition) {
      this.declaringType = declaringType;
      this.definition = definition;
    }

    public VirtualMethod(ExpandedMethod method) : this(method.declaringType, method.definition) {}

    #region Equality

    public bool Equals(VirtualMethod other) {
      return Equals(definition, other.definition);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is VirtualMethod && Equals((VirtualMethod) obj);
    }

    public override int GetHashCode() {
      return (definition != null ? definition.GetHashCode() : 0);
    }

    public static bool operator ==(VirtualMethod left, VirtualMethod right) { return left.Equals(right); }
    public static bool operator !=(VirtualMethod left, VirtualMethod right) { return !left.Equals(right); }

    #endregion

    public override string ToString() { return $"[{definition} in {declaringType}]"; }
  }
}
