namespace TestData {
  struct Tuple1<A> {
    public readonly A value;

    public Tuple1(A value) { this.value = value; }

    public static Tuple1<A> a(A a) { return new Tuple1<A>(a); }

    public Tuple2<A, B> add<B>(B b) { return new Tuple2<A, B>(value, b); }

    public static A identity(A a) { return a; }
  }

  class Tuple1 {
    public static Tuple1<A> a<A>(A a) { return Tuple1<A>.a(a); }
    public static A identity<A>(A a) { return a; }
  }

  class Tuple1C<A> {
    public readonly A value;

    public Tuple1C(A value) { this.value = value; }
  }
}
