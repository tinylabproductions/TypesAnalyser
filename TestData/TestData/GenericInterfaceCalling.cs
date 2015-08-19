// ReSharper disable UnusedParameter.Local

namespace TestData {
  interface IGenericInterfaceCalling<A> {
    A identity(A a);
    Tuple2<A, B> zip<B>(A a, B b);
  }
  interface IGenericInterfaceCalling2<A> { A identity(A a); }

  class GenericSimpleIdentity<A> : IGenericInterfaceCalling<A> {
    public A identity(A a) { return a; }
    public Tuple2<A, B> zip<B>(A a, B b) { return new Tuple2<A, B>(a, b); }
  }

  class GenericExtraArgIdentity<A, B> : IGenericInterfaceCalling<A> {
    public A identity(A a) { return fromB(fromA(a)); }
    public Tuple2<A, B1> zip<B1>(A a, B1 b) { return new Tuple2<A, B1>(a, b); }

    static B fromA(A a) { return default(B); }
    static A fromB(B b) { return default(A); }
  }

  class GenericCircularIdentity<A> : IGenericInterfaceCalling<A> {
    public A identity(A a) {
      if (Equals(a, default(A))) {
        IGenericInterfaceCalling2<A> id2 = new GenericCircularIdentity2<A>();
        return id2.identity(a);
      }
      else return a;
    }

    public Tuple2<A, B> zip<B>(A a, B b) { return new Tuple2<A, B>(a, b); }
  }

  class GenericCircularIdentity2<A> : IGenericInterfaceCalling2<A> {
    public A identity(A a) {
      if (Equals(a, default(A))) {
        IGenericInterfaceCalling<A> id = new GenericCircularIdentity<A>();
        return id.identity(a);
      }
      else return a;
    }
  }
  

  class IGenericUnknownImplementer<A> {
    public A identity(A value) { return value; }
  }

  class IGenericExtender<A> : IGenericUnknownImplementer<A>, IGenericInterfaceCalling<A> {
    public Tuple2<A, B> zip<B>(A a, B b) { return new Tuple2<A, B>(a, b); }
  }
}
