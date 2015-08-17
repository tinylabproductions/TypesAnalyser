namespace TestData {
  struct Recursive<A> {
    public readonly A value;

    public Recursive(A value) { this.value = value; }

    public static Recursive<A> a(A a) { return new Recursive<A>(a); }

    public Recursive<Tuple1<A>> wrap() { return Recursive.a(Tuple1.a(value)); }
    public Recursive<Tuple2<A, B>> wrap2<B>(B b) { return Recursive.a(Tuple2.a(value, b)); }
  }

  static class Recursive {
    public static Recursive<A> a<A>(A a) { return Recursive<A>.a(a); }

    public static Recursive<Tuple2<A, B>> wrap2_ext<A, B>(this Recursive<A> r, B b) { return r.wrap2(b); }
  }
}
