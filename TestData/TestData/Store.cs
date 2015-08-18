namespace TestData {
  class Store {
    public static int staticInt;
    public static double staticDouble;

    public int instanceInt;
    public double instanceDouble;

    public static void refMethod(ref int i) { i += 3; }
    public static void genRefMethod<A>(ref A i, A i2) { i = i2; }
    public static void outMethod(out int i) { i = 3; }
    public static void genOutMethod<A>(out A i, A i2) { i = i2; }
  }
}
