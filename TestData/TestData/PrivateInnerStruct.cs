// ReSharper disable NotAccessedField.Local
namespace TestData {
  class PrivateInnerStruct<A> {
    private struct Inner {
      public A value;
    }

    readonly Inner[] list = new Inner[1];

    public void add(A a) { list[0].value = a; }
  }
}
