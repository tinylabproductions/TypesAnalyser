// ReSharper disable UnusedParameter.Local

namespace TestData {
  interface IGenericInterfaceCalling<A> { A identity(A a); }
  interface IGenericInterfaceCalling2<A> { A identity(A a); }

  class GenericSimpleIdentity<A> : IGenericInterfaceCalling<A> {
    public A identity(A a) { return a; }
  }

  class GenericExtraArgIdentity<A, B> : IGenericInterfaceCalling<A> {
    public A identity(A a) { return fromB(fromA(a)); }

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

  class IGenericExtender<A> : IGenericUnknownImplementer<A>, IGenericInterfaceCalling<A> {}
}
