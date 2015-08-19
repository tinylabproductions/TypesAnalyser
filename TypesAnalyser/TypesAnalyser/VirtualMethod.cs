using System;

namespace TypesAnalyser {
  public struct VirtualMethod : IEquatable<VirtualMethod> {
    public readonly ExpandedMethod method;

    public VirtualMethod(ExpandedMethod method) { this.method = method; }

    #region Equality

    public bool Equals(VirtualMethod other) {
      return method.Equals(other.method);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      return obj is VirtualMethod && Equals((VirtualMethod) obj);
    }

    public override int GetHashCode() {
      return method.GetHashCode();
    }

    public static bool operator ==(VirtualMethod left, VirtualMethod right) { return left.Equals(right); }
    public static bool operator !=(VirtualMethod left, VirtualMethod right) { return !left.Equals(right); }

    #endregion

    public override string ToString() { return $"VirtualMethod[{method}]"; }
  }
}
