using TestData;

// ReSharper disable UnusedVariable

static class TestEntryPoints {
  public static void testStaticNonGeneric() {
    var intV = IntWrapper.identity(3);
  }

  public static void testStaticNonGenericChained() {
    var intV = IntWrapper2.identity(3);
  }

  public static void testInstanceNonGeneric() {
    var intW = new IntWrapper(3);
  }

  public static void testStaticReturningInstanceNonGeneric() {
    var intW = IntWrapper.a(3);
  }

  public static void testStaticReturningInstanceNonGenericChained() {
    var intW = IntWrapper2.a(3);
  }

  public static void testInstanceNonGenericChained() {
    var intW = new IntWrapper(3).intWrapper2();
  }

  public static void testInstanceCallingStatic() {
    var intW = new IntWrapper(3).intWrapper2Static();
  }

  public static void testGenericStaticMethodInSimpleClass() {
    var intId = Tuple1.identity(3);
    var strId = Tuple1.identity("3");
  }

  public static void testStaticMethodInGenericClass() {
    var intId = Tuple1<int>.identity(3);
    var strId = Tuple1<string>.identity("3");
  }

  public static void testGenericStructCtor() {
    var intT = new Tuple1<int>(3);
    var strT = new Tuple1<string>("3");
  }

  public static void testGenericClassCtor() {
    var intT = new Tuple1C<int>(3);
    var strT = new Tuple1C<string>("3");
  }

  public static void testGenericInstanceViaStaticMethodInGenericClass() {
    var intT = Tuple1<int>.a(3);
    var strT = Tuple1<string>.a("3");
  }

  public static void testGenericInstanceViaStaticGenericMethodInSimpleClass() {
    var intT = Tuple1.a(3);
    var strT = Tuple1.a("3");
  }

  public static void testGenericMethodInGenericClass() {
    var intT = new Tuple1<int>(3).add("4");
    var strT = new Tuple1<string>("3").add(4);
  }

  public static void testRecursiveTypeSimple() {
    var r = Recursive.a(3);
  }

  public static void testRecursiveTypeWrap() {
    var r = Recursive.a(3);
    var w1 = r.wrap();
    var w2 = r.wrap2("3");
  }

  public static void testRecursiveTypeWrapExtensionMethod() {
    var r = Recursive.a(3);
    var w2 = r.wrap2_ext("3");
  }

  public static void testNonGenericInterfaceCalling() {
    INonGeneric iface = new NormalIdentity();
    iface.identity(3);
    iface = new LyingIdentity();
    iface.identity(3);
  }

  public static void testNonGenericInterfaceChainedCalling() {
    INonGeneric2 iface = new RandomIdentity();
    iface.identity(3);
  }

  public static void testNonGenericInterfaceCircularCalling() {
    INonGeneric iface = new CircularIdentity();
    iface.identity(3);
  }

  public static void testNonGenericAbstractClassCalling() {
    ACNonGeneric iface = new ACNormalIdentity();
    iface.identity(3);
    iface = new ACLyingIdentity();
    iface.identity(3);
  }

  public static void testNonGenericAbstractClassChainedCalling() {
    ACNonGeneric2 iface = new ACRandomIdentity();
    iface.identity(3);
  }

  public static void testNonGenericAbstractClassCircularCalling() {
    ACNonGeneric iface = new ACCircularIdentity();
    iface.identity(3);
  }

  public static void testNonGenericAbstractClassCircularCallingInterface() {
    ACNonGeneric iface = new ACCircularIfaceIdentity();
    iface.identity(3);
  }
}