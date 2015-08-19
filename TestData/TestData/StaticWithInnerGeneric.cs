using System;

namespace TestData {
  static class StaticWithInnerGeneric {
    public static Val<int> val;

    public class Val<A> {}
  }
}
