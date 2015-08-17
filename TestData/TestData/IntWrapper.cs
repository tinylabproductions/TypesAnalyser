namespace TestData {
  class IntWrapper {
    public readonly int value;

    public IntWrapper(int value) { this.value = value; }

    public static IntWrapper a(int value) { return new IntWrapper(value); }

    public static int identity(int value) { return value; }

    public IntWrapper2 intWrapper2() { return new IntWrapper2(value); }
    public IntWrapper2 intWrapper2Static() { return IntWrapper2.a2(value); }
  }

  class IntWrapper2 {
    public readonly int value;

    public IntWrapper2(int value) { this.value = value; }

    public static IntWrapper a(int value) { return IntWrapper.a(value); }
    public static IntWrapper2 a2(int value) { return new IntWrapper2(value); }

    public static int identity(int value) { return IntWrapper.identity(value); }
  }
}
