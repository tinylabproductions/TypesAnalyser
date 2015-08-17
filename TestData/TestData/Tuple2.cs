namespace TestData {
  struct Tuple2<A, B> {
    public readonly A _1;
    public readonly B _2;

    public Tuple2(A a, B b) {
      _1 = a;
      _2 = b;
    }

    public static Tuple2<A, B> a(A a, B b) { return new Tuple2<A, B>(a, b); }
  }

  class Tuple2 {
    public static Tuple2<A, B> a<A, B>(A a, B b) { return Tuple2<A, B>.a(a, b); }
  }
}
