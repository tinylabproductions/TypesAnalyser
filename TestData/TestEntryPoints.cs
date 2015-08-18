using System;
using System.Collections.Generic;
using System.Linq;
using TestData;

// ReSharper disable UnusedVariable

static class TestEntryPoints {
  // Order of these methods in code is important, because the compiler generates 
  // names for delegate fields based on it.
  // Brittle, eh?

  #region Delegate entry points

  public static void testDelegates() {
    Func<int, float, float> f = (a, b) => a + b;
    var result = f(1, 2.4f);
  }

  public static void testDynamicDelegates() {
    Func<int, Func<float, float>> createF = a => b => a + b;
    var add1 = createF(1);
    var result = add1(2.4f);
  }

  public static void testClosureDelegates() {
    var a = 1;
    Func<Func<float, float>> createF = () => b => a + b;
    var add1 = createF();
    var add2 = createF();
    a = 2;
    var result = add1(2.4f);
  }
  
  static event Func<int, float> onFoo;
  public static void testEvents() {
    Func<int, float> handler = i => i;
    onFoo += handler;
    var x = onFoo(3);
    onFoo -= handler;
  }

  #endregion

  public static void testAddition() {
    var a = 3;
    var b = 4.5;
    var c = a + b;
  }

  public static void testStoreStatic() {
    Store.staticInt = 3;
    Store.staticDouble = 4;
  }

  public static void testFetchStatic() {
    var a = Store.staticInt;
    var b = Store.staticDouble;
  }

  public static void testStoreInstance() { var s = new Store {instanceInt = 3, instanceDouble = 4}; }

  public static void testFetchInstance() {
    var s = new Store();
    var a = s.instanceInt;
    var b = s.instanceDouble;
  }

  public static void testRefMethod() {
    var a = 3;
    Store.refMethod(ref a);
  }

  public static void testGenRefMethod() {
    var a = 3;
    Store.genRefMethod(ref a, 5);
  }

  public static void testGenRefArrayMethod() {
    var a = new [] {3};
    Store.genRefArrMethod(ref a, 5);
  }

  public static void testOutMethod() {
    int a;
    Store.outMethod(out a);
  }

  public static void testGenOutMethod() {
    int a;
    Store.genOutMethod(out a, 5);
  }

  public static void testGenOutArrayMethod() {
    int[] a;
    Store.genOutArrMethod(out a, 5);
  }

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

  public static void testNonGenericInterfaceUnbeknownstImplementation() {
    INonGeneric iface = new INonGenericExtender();
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

  public static void testGenericInterfaceSimple() {
    IGenericInterfaceCalling<int> iface = new GenericSimpleIdentity<int>();
    iface.identity(3);
  }

  public static void testGenericInterfaceExtraArg() {
    IGenericInterfaceCalling<int> iface = new GenericExtraArgIdentity<int, float>();
    iface.identity(3);
  }

  public static void testGenericInterfaceCircular() {
    IGenericInterfaceCalling<int> iface = new GenericCircularIdentity<int>();
    iface.identity(3);
  }

  public static void testGenericInterfaceUnbeknownstImplementation() {
    IGenericInterfaceCalling<int> iface = new IGenericExtender<int>();
    iface.identity(3);
  }

  public static void testMethodUniqueness() { var a = string.Concat("3", "4"); }

  public static void testGenericArrayViaStatic() { var x = Tuple1.arrIdentity(new[] {1}); }
  public static void testGenericArrayViaGenericStatic() { var x = Tuple1<int>.arrIdentity(new[] {1}); }

  public static void testPrivateInnerStruct() {
    new PrivateInnerStruct<int>().add(3);
  }

  public static void testPublicInnerRecursive() {
    var x = new PublicInnerRecursive<int>(3);
  }

  public static void testLinqSelect() {
    var intWrappers = new[] {new IntWrapper(3)};
    // 	Subject<Collider2D>[] observables = (from b in componentsInChildren
    //  select b.gameObject.AddComponent<Trigger2D>().triggerEnter).ToArray<Subject<Collider2D>>();
//    var b = intWrappers.Select(x => Tuple1.a(x.value)).ToArray();
    var b = (from x in intWrappers select Tuple1.a(x.value)).ToArray();
  }
}