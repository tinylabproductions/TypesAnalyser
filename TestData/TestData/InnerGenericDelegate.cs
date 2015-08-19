namespace TestData {
  class InnerGenericDelegate<TValue> {
    public delegate bool LessOrEqual(TValue lhs, TValue rhs);

    LessOrEqual _leq;

    public InnerGenericDelegate(LessOrEqual leq) { _leq = leq; }
  }

  class UsingInnerGenericDelegate<TValue> {
    InnerGenericDelegate<TValue>.LessOrEqual _leq;

    public UsingInnerGenericDelegate(InnerGenericDelegate<TValue>.LessOrEqual leq) { _leq = leq; }
  }
}
