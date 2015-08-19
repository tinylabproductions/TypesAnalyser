namespace TestData {
  interface IExplicitInterface {
    int getInt();
  }

  class ExplicitInterfaceImpl : IExplicitInterface {
    int IExplicitInterface.getInt() { return 3; }
  }

  interface IExplicitGenInterface<A> {
    A identity(A a);
  }

  class ExplicitInterfaceGenImpl<A> : IExplicitGenInterface<A> {
    A IExplicitGenInterface<A>.identity(A a) { return a; }
  }
}
