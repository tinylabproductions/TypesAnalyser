using System;

namespace TestData {
  class DelegateInMethod<A> {
    public static Action<A> get() { return a => { }; }
  }
}
