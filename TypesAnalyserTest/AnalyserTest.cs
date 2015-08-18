using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using com.tinylabproductions.TLPLib.Extensions;
using Mono.Cecil;
using Mono.Collections.Generic;
using NUnit.Framework;
using TypesAnalyser;
using TypesAnalyserTest.Data;

namespace TypesAnalyserTest {
  [TestFixture]
  public class AnalyserTest {
    #region Helpers

    readonly AbstractTypeImplementations atis;
    readonly ImmutableDictionary<string, TypeDefinition> mainModuleTypes;
    readonly AnalyserLogger logger = new NoopLogger();

    public AnalyserTest() {
      var assembly = AssemblyDefinition.ReadAssembly(
        @"..\..\..\TestData\bin\debug\TestData.dll"
      );
      atis = AbstractTypeImplementations.create(Analyser.allTypes(assembly));
      mainModuleTypes = assembly.MainModule.Types.ToImmutableDictionary(td => td.FullName);
    }

    IEnumerable<MethodDefinition> entryMethod(string fullTypeName, string methodName) {
      var typeDef = mainModuleTypes[fullTypeName];
      var method = typeDef.Methods.First(m => m.Name == methodName);
      yield return method;
    }

    static string testMethodName(string name) { return name; }

    IEnumerable<MethodDefinition> analyserTestMethod(string name) {
      return entryMethod(TEP, testMethodName(name));
    }

    AnalyserTestData analyze(string name) {
      return AnalyserTestData.create(Analyser.analyze(analyserTestMethod(name), logger));
    }

    void assertAnalyze(
      string name, 
      IEnumerable<string> usedTypes, IEnumerable<string> analyzedMethods
    ) {
      var assertedUsedTypes = usedTypes.ToImmutableSortedSet().Add(VOID).Add(TEP);
      var assertedAnalyzedMethods = analyzedMethods.ToImmutableSortedSet()
        .Add($"{VOID} {TEP}::{testMethodName(name)}()");
      var data = analyze(name);

      var usedButNotExpectedTypes = data.usedTypes.Except(assertedUsedTypes);
      var expectedButNotUsedTypes = assertedUsedTypes.Except(data.usedTypes);
      var analyzedButNotExpectedMethods = data.analyzedMethods.Except(assertedAnalyzedMethods);
      var expectedButNotAnalyzedMethods = assertedAnalyzedMethods.Except(data.analyzedMethods);

      if (
        !usedButNotExpectedTypes.IsEmpty || !expectedButNotUsedTypes.IsEmpty
        || !analyzedButNotExpectedMethods.IsEmpty || !expectedButNotAnalyzedMethods.IsEmpty
      ) {
        Fn<ImmutableSortedSet<string>, string> toMsg = s => s.mkString(
          Environment.NewLine + "  ", 
          Environment.NewLine + "  ", Environment.NewLine + Environment.NewLine
        );
        var message =
          "Used but not expected types:" + toMsg(usedButNotExpectedTypes) +
          "Expected but not used types:" + toMsg(expectedButNotUsedTypes) +
          "Analyzed but not expected methods:" + toMsg(analyzedButNotExpectedMethods) +
          "Expected but not analyzed methods:" + toMsg(expectedButNotAnalyzedMethods);
        throw new AssertionException(message);
      }
    }

    void assertAnalyze(
      string name, string usedType, string analyzedMethod
    ) { assertAnalyze(name, new [] {usedType}, new [] {analyzedMethod}); }

    void assertAnalyze(
      string name, string usedType, IEnumerable<string> analyzedMethods
    ) { assertAnalyze(name, new [] {usedType}, analyzedMethods); }

    const string
      TEP = "TestEntryPoints",
      TEP_GEN = "TestEntryPoints/<>c",
      // http://stackoverflow.com/questions/16401860/what-does-displayclass-name-mean-when-calling-lambda
      DISPLAY_CLASS = "c__DisplayClass",
      VOID = "System.Void",
      INT = "System.Int32",
      INT_A = "System.Int32[]",
      INT_PTR = "System.IntPtr",
      BOOL = "System.Boolean",
      DELEGATE = "System.Delegate",
      FLT = "System.Single",
      DBL = "System.Double",
      STR = "System.String",
      OBJ = "System.Object",
      EX = "System.Exception",
      MNS_EX = "System.MulticastNotSupportedException",
      SYS_EX = "System.SystemException",
      ARG_EX = "System.ArgumentException",
      TYPED_REF = "System.TypedReference",
      FUNC1 = "System.Func`1",
      FUNC2 = "System.Func`2",
      FUNC3 = "System.Func`3",
      OBJ_CTOR = "System.Void System.Object::.ctor()",
      STORE = "TestData.Store",
      INT_WRAPPER = "TestData.IntWrapper",
      INT_WRAPPER2 = "TestData.IntWrapper2",
      RECURSIVE = "TestData.Recursive`1",
      RECURSIVE_S = "TestData.Recursive",
      PRIV_INNER_STRUCT = "TestData.PrivateInnerStruct`1",
      PRIV_INNER_STRUCT_S = "TestData.PrivateInnerStruct`1/Inner",
      PUBLIC_INNER_RECURSIVE = "TestData.PublicInnerRecursive`1",
      PUBLIC_INNER_RECURSIVE_NODE = "TestData.PublicInnerRecursive`1/Node",
      TUPLE1_S = "TestData.Tuple1",
      TUPLE1 = "TestData.Tuple1`1",
      TUPLE1_C = "TestData.Tuple1C`1",
      TUPLE2_S = "TestData.Tuple2",
      TUPLE2 = "TestData.Tuple2`2",
      INONGENERIC = "TestData.INonGeneric",
      INONGENERIC2 = "TestData.INonGeneric2",
      IUNKNOWNIMPL = "TestData.INonGenericUnknownImplementer",
      IUNKNOWNIMPL_EX = "TestData.INonGenericExtender",
      CIRCULAR_IDENTITY = "TestData.CircularIdentity",
      CIRCULAR_IDENTITY2 = "TestData.CircularIdentity2",
      CIRCULAR_AC_IDENTITY = "TestData.CircularAbstractClassIdentity",
      NORMAL_IDENTITY = "TestData.NormalIdentity",
      LYING_IDENTITY = "TestData.LyingIdentity",
      RANDOM_IDENTITY = "TestData.RandomIdentity",
      AC_NONGENERIC = "TestData.ACNonGeneric",
      AC_NONGENERIC2 = "TestData.ACNonGeneric2",
      AC_CIRCULAR_IDENTITY = "TestData.ACCircularIdentity",
      AC_CIRCULAR_IDENTITY2 = "TestData.ACCircularIdentity2",
      AC_CIRCULAR_IF_IDENTITY = "TestData.ACCircularIfaceIdentity",
      AC_NORMAL_IDENTITY = "TestData.ACNormalIdentity",
      AC_LYING_IDENTITY = "TestData.ACLyingIdentity",
      AC_RANDOM_IDENTITY = "TestData.ACRandomIdentity",
      IGENERIC = "TestData.IGenericInterfaceCalling`1",
      IGENERIC2 = "TestData.IGenericInterfaceCalling2`1",
      GENERIC_IDENTITY = "TestData.GenericSimpleIdentity`1",
      GENERIC_CIRCULAR = "TestData.GenericCircularIdentity`1",
      GENERIC_CIRCULAR2 = "TestData.GenericCircularIdentity2`1",
      GENERIC_EXTRA_ARG_IDENTITY = "TestData.GenericExtraArgIdentity`2",
      IGEN_UNKNOWNIMPL = "TestData.IGenericUnknownImplementer`1",
      IGEN_UNKNOWNIMPL_EX = "TestData.IGenericExtender`1"
    ;

    #endregion

    #region Loads & Stores
    
    [Test]
    public void testAddition() {
      assertAnalyze("testAddition", 
        new [] {INT, DBL},
        ReadOnlyCollection<string>.Empty
      );
    }
    
    [Test]
    public void testStoreStatic() {
      assertAnalyze("testStoreStatic", 
        new [] {INT, DBL, STORE},
        ReadOnlyCollection<string>.Empty
      );
    }
    
    [Test]
    public void testFetchStatic() {
      assertAnalyze("testFetchStatic", 
        new [] {INT, DBL, STORE},
        ReadOnlyCollection<string>.Empty
      );
    }
    
    [Test]
    public void testStoreInstance() {
      assertAnalyze("testStoreInstance", 
        new [] {INT, DBL, STORE, OBJ},
        new [] {OBJ_CTOR, $"{VOID} {STORE}::.ctor()"}
      );
    }
    
    [Test]
    public void testFetchInstance() {
      assertAnalyze("testFetchInstance", 
        new [] {INT, DBL, STORE, OBJ},
        new [] {OBJ_CTOR, $"{VOID} {STORE}::.ctor()"}
      );
    }

    #endregion

    #region ref/out

    [Test]
    public void testRefMethod() {
      assertAnalyze("testRefMethod", 
        new [] {INT},
        new [] {$"{VOID} {STORE}::refMethod({INT})"}
      );
    }

    [Test]
    public void testGenRefMethod() {
      assertAnalyze("testGenRefMethod", 
        new [] {INT},
        new [] {$"{VOID} {STORE}::genRefMethod<{INT}>({INT}, {INT})"}
      );
    }

    [Test]
    public void testGenRefArrayMethod() {
      assertAnalyze("testGenRefArrayMethod", 
        new [] {INT, INT_A},
        new [] {$"{VOID} {STORE}::genRefArrMethod<{INT}>({INT_A}, {INT})"}
      );
    }
    
    [Test]
    public void testOutMethod() {
      assertAnalyze("testOutMethod", 
        new [] {INT},
        new [] {$"{VOID} {STORE}::outMethod({INT})"}
      );
    }
    
    [Test]
    public void testGenOutMethod() {
      assertAnalyze("testGenOutMethod", 
        new [] {INT},
        new [] {$"{VOID} {STORE}::genOutMethod<{INT}>({INT}, {INT})"}
      );
    }
    
    [Test]
    public void testGenOutArrayMethod() {
      assertAnalyze("testGenOutArrayMethod", 
        new [] {INT, INT_A},
        new [] {$"{VOID} {STORE}::genOutArrMethod<{INT}>({INT_A}, {INT})"}
      );
    }

    #endregion

    #region Non-Generic

    [Test]
    public void testStaticNonGeneric() {
      assertAnalyze("testStaticNonGeneric", INT, $"{INT} {INT_WRAPPER}::identity({INT})");
    }

    [Test]
    public void testStaticNonGenericChained() {
      assertAnalyze("testStaticNonGenericChained", 
        INT, 
        new [] {
          $"{INT} {INT_WRAPPER}::identity({INT})",
          $"{INT} {INT_WRAPPER2}::identity({INT})"
        }
      );
    }

    [Test]
    public void testInstanceNonGeneric() {
      assertAnalyze("testInstanceNonGeneric", 
        new [] {INT, OBJ, INT_WRAPPER}, 
        new [] {
          OBJ_CTOR,
          $"{VOID} {INT_WRAPPER}::.ctor({INT})"
        }
      );
    }

    [Test]
    public void testStaticReturningInstanceNonGeneric() {
      assertAnalyze("testStaticReturningInstanceNonGeneric", 
        new [] {INT, OBJ, INT_WRAPPER}, 
        new [] {
          OBJ_CTOR,
          $"{VOID} {INT_WRAPPER}::.ctor({INT})",
          $"{INT_WRAPPER} {INT_WRAPPER}::a({INT})"
        }
      );
    }

    [Test]
    public void testStaticReturningInstanceNonGenericChained() {
      assertAnalyze("testStaticReturningInstanceNonGenericChained", 
        new [] {INT, OBJ, INT_WRAPPER}, 
        new [] {
          OBJ_CTOR,
          $"{VOID} {INT_WRAPPER}::.ctor({INT})",
          $"{INT_WRAPPER} {INT_WRAPPER}::a({INT})",
          $"{INT_WRAPPER} {INT_WRAPPER2}::a({INT})"
        }
      );
    }

    [Test]
    public void testInstanceNonGenericChained() {
      assertAnalyze("testInstanceNonGenericChained", 
        new [] {INT, OBJ, INT_WRAPPER, INT_WRAPPER2}, 
        new [] {
          OBJ_CTOR,
          $"{VOID} {INT_WRAPPER}::.ctor({INT})",
          $"{VOID} {INT_WRAPPER2}::.ctor({INT})",
          $"{INT_WRAPPER2} {INT_WRAPPER}::intWrapper2()"
        }
      );
    }

    [Test]
    public void testInstanceCallingStatic() {
      assertAnalyze("testInstanceCallingStatic", 
        new [] {INT, OBJ, INT_WRAPPER, INT_WRAPPER2}, 
        new [] {
          OBJ_CTOR,
          $"{VOID} {INT_WRAPPER}::.ctor({INT})",
          $"{VOID} {INT_WRAPPER2}::.ctor({INT})",
          $"{INT_WRAPPER2} {INT_WRAPPER}::intWrapper2Static()",
          $"{INT_WRAPPER2} {INT_WRAPPER2}::a2({INT})"
        }
      );
    }

    #endregion

    #region Generic

    #region Delegates
    
    [Test]
    public void testDelegates() {
      const string name = "testDelegates", idx = "0_0";
      assertAnalyze(name, 
        new [] {INT, FLT, TEP_GEN, $"{FUNC3}<{INT}, {FLT}, {FLT}>"},
        new [] {
          $"{FLT} {FUNC3}<{INT}, {FLT}, {FLT}>::Invoke({INT}, {FLT})",
          $"{FLT} {TEP_GEN}::<{name}>b__{idx}({INT}, {FLT})"
        }
      );
    }
    
    [Test]
    public void testDynamicDelegates() {
      const string name = "testDynamicDelegates", idx = "1_0";
      assertAnalyze(name, 
        new [] {
          INT, FLT, OBJ, TEP_GEN,
          $"{FUNC2}<{INT}, {FUNC2}<{FLT}, {FLT}>>",
          $"{FUNC2}<{FLT}, {FLT}>",
          $"{TEP}/<>{DISPLAY_CLASS}{idx}",
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {TEP}/<>{DISPLAY_CLASS}{idx}::.ctor()",
          $"{FUNC2}<{FLT}, {FLT}> {TEP_GEN}::<{name}>b__{idx}({INT})",
          $"{FUNC2}<{FLT}, {FLT}> {FUNC2}<{INT}, {FUNC2}<{FLT}, {FLT}>>::Invoke({INT})",
          $"{FLT} {FUNC2}<{FLT}, {FLT}>::Invoke({FLT})",
          $"{FLT} {TEP}/<>{DISPLAY_CLASS}{idx}::<{name}>b__1({FLT})",
        }
      );
    }
    
    [Test]
    public void testClosureDelegates() {
      const string name = "testClosureDelegates", idx = "2_0";
      assertAnalyze(name, 
        new [] {
          INT, FLT, OBJ,
          $"{FUNC1}<{FUNC2}<{FLT}, {FLT}>>",
          $"{FUNC2}<{FLT}, {FLT}>",
          $"{TEP}/<>{DISPLAY_CLASS}{idx}",
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {TEP}/<>{DISPLAY_CLASS}{idx}::.ctor()",
          $"{FUNC2}<{FLT}, {FLT}> {TEP}/<>{DISPLAY_CLASS}{idx}::<{name}>b__0()",
          $"{FUNC2}<{FLT}, {FLT}> {FUNC1}<{FUNC2}<{FLT}, {FLT}>>::Invoke()",
          $"{FLT} {FUNC2}<{FLT}, {FLT}>::Invoke({FLT})",
          $"{FLT} {TEP}/<>{DISPLAY_CLASS}{idx}::<{name}>b__1({FLT})",
        }
      );
    }

    #endregion
    
    [Test]
    public void testEvents() {
      const string name = "testEvents", idx = "6_0";
      assertAnalyze(name, 
        new [] {
          INT, FLT, OBJ, ARG_EX, BOOL, DELEGATE, EX, 
          $"{FUNC2}<{INT}, {FLT}>", INT_PTR, MNS_EX, SYS_EX, STR, TYPED_REF, TEP_GEN
        },
        new [] {
  OBJ_CTOR,
  $"{BOOL} {DELEGATE}::InternalEqualTypes({OBJ}, {OBJ})",
  $"{BOOL} {OBJ}::Equals({OBJ})",
  $"{BOOL} {OBJ}::InternalEquals({OBJ}, {OBJ})",
  $"{DELEGATE} {DELEGATE}::Combine({DELEGATE}, {DELEGATE})",
  $"{DELEGATE} {DELEGATE}::CombineImpl({DELEGATE})",
  $"{DELEGATE} {DELEGATE}::Remove({DELEGATE}, {DELEGATE})",
  $"{DELEGATE} {DELEGATE}::RemoveImpl({DELEGATE})",
  $"{FUNC2}<{INT}, {FLT}> System.Threading.Interlocked::CompareExchange<{FUNC2}<{INT}, {FLT}>>({FUNC2}<{INT}, {FLT}>, {FUNC2}<{INT}, {FLT}>, {FUNC2}<{INT}, {FLT}>)",
  $"{INT_PTR} {INT_PTR}::op_Explicit({INT})",
  $"{FLT} {FUNC2}<{INT}, {FLT}>::Invoke({INT})",
  $"{FLT} {TEP_GEN}::<{name}>b__{idx}({INT})",
  $"{STR} System.Environment::GetResourceFromDefault({STR})",
  $"{STR} System.Environment::GetResourceString({STR})",
  $"{VOID} {ARG_EX}::.ctor({STR})",
  $"{VOID} {EX}::.ctor({STR})",
  $"{VOID} {EX}::set_HResult({INT})",
  $"{VOID} {EX}::SetErrorCode({INT})",
  $"{VOID} {INT_PTR}::.ctor({INT})",
  $"{VOID} {MNS_EX}::.ctor({STR})",
  $"{VOID} {SYS_EX}::.ctor({STR})",
  $"{VOID} System.Threading.Interlocked::_CompareExchange({TYPED_REF}, {TYPED_REF}, {OBJ})",
  $"{VOID} {TEP}::add_onFoo({FUNC2}<{INT}, {FLT}>)",
  $"{VOID} {TEP}::remove_onFoo({FUNC2}<{INT}, {FLT}>)",
        }
      );
    }

    [Test]
    public void testGenericStaticMethodInSimpleClass() {
      assertAnalyze("testGenericStaticMethodInSimpleClass", 
        new [] {INT, STR},
        new [] {
          $"{INT} {TUPLE1_S}::identity<{INT}>({INT})",
          $"{STR} {TUPLE1_S}::identity<{STR}>({STR})"
        }
      );
    }
    
    [Test]
    public void testStaticMethodInGenericClass() {
      assertAnalyze("testStaticMethodInGenericClass", 
        new [] {INT, STR},
        new [] {
          $"{INT} {TUPLE1}<{INT}>::identity({INT})",
          $"{STR} {TUPLE1}<{STR}>::identity({STR})"
        }
      );
    }
    
    [Test]
    public void testGenericStructCtor() {
      assertAnalyze("testGenericStructCtor", 
        new [] {INT, STR, $"{TUPLE1}<{INT}>", $"{TUPLE1}<{STR}>"},
        new [] {
          $"{VOID} {TUPLE1}<{INT}>::.ctor({INT})",
          $"{VOID} {TUPLE1}<{STR}>::.ctor({STR})"
        }
      );
    }
    
    [Test]
    public void testGenericClassCtor() {
      assertAnalyze("testGenericClassCtor", 
        new [] {INT, STR, OBJ, $"{TUPLE1_C}<{INT}>", $"{TUPLE1_C}<{STR}>"},
        new [] {
          OBJ_CTOR,
          $"{VOID} {TUPLE1_C}<{INT}>::.ctor({INT})",
          $"{VOID} {TUPLE1_C}<{STR}>::.ctor({STR})"
        }
      );
    }
    
    [Test]
    public void testGenericInstanceViaStaticMethodInGenericClass() {
      assertAnalyze("testGenericInstanceViaStaticMethodInGenericClass", 
        new [] {INT, STR, $"{TUPLE1}<{INT}>", $"{TUPLE1}<{STR}>"},
        new [] {
          $"{TUPLE1}<{INT}> {TUPLE1}<{INT}>::a({INT})",
          $"{VOID} {TUPLE1}<{INT}>::.ctor({INT})",
          $"{TUPLE1}<{STR}> {TUPLE1}<{STR}>::a({STR})",
          $"{VOID} {TUPLE1}<{STR}>::.ctor({STR})"
        }
      );
    }
    
    [Test]
    public void testGenericInstanceViaStaticGenericMethodInSimpleClass() {
      assertAnalyze("testGenericInstanceViaStaticGenericMethodInSimpleClass", 
        new [] {INT, STR, $"{TUPLE1}<{INT}>", $"{TUPLE1}<{STR}>"},
        new [] {
          $"{TUPLE1}<{INT}> {TUPLE1_S}::a<{INT}>({INT})",
          $"{TUPLE1}<{INT}> {TUPLE1}<{INT}>::a({INT})",
          $"{VOID} {TUPLE1}<{INT}>::.ctor({INT})",
          $"{TUPLE1}<{STR}> {TUPLE1_S}::a<{STR}>({STR})",
          $"{TUPLE1}<{STR}> {TUPLE1}<{STR}>::a({STR})",
          $"{VOID} {TUPLE1}<{STR}>::.ctor({STR})"
        }
      );
    }
    
    [Test]
    public void testGenericMethodInGenericClass() {
      assertAnalyze("testGenericMethodInGenericClass", 
        new [] {
          INT, STR,
          $"{TUPLE1}<{INT}>", $"{TUPLE1}<{STR}>",
          $"{TUPLE2}<{INT}, {STR}>", $"{TUPLE2}<{STR}, {INT}>"
        },
        new [] {
          $"{VOID} {TUPLE1}<{INT}>::.ctor({INT})",
          $"{VOID} {TUPLE2}<{INT}, {STR}>::.ctor({INT}, {STR})",
          $"{VOID} {TUPLE2}<{STR}, {INT}>::.ctor({STR}, {INT})",
          $"{TUPLE2}<{INT}, {STR}> {TUPLE1}<{INT}>::add<{STR}>({STR})",
          $"{VOID} {TUPLE1}<{STR}>::.ctor({STR})",
          $"{TUPLE2}<{STR}, {INT}> {TUPLE1}<{STR}>::add<{INT}>({INT})"
        }
      );
    }
    
    [Test]
    public void testRecursiveTypeSimple() {
      assertAnalyze("testRecursiveTypeSimple", 
        new [] {
          INT,
          $"{RECURSIVE}<{INT}>",
        },
        new [] {
          $"{VOID} {RECURSIVE}<{INT}>::.ctor({INT})",
          $"{RECURSIVE}<{INT}> {RECURSIVE}<{INT}>::a({INT})",
          $"{RECURSIVE}<{INT}> {RECURSIVE_S}::a<{INT}>({INT})",
        }
      );
    }
    
    [Test]
    public void testRecursiveTypeWrap() {
      assertAnalyze("testRecursiveTypeWrap", 
        new [] {
          INT, STR,
          $"{RECURSIVE}<{INT}>",
          $"{RECURSIVE}<{TUPLE1}<{INT}>>",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>>",
          $"{TUPLE1}<{INT}>",
          $"{TUPLE2}<{INT}, {STR}>"
        },
        new [] {
          $"{VOID} {RECURSIVE}<{INT}>::.ctor({INT})",
          $"{RECURSIVE}<{INT}> {RECURSIVE}<{INT}>::a({INT})",
          $"{RECURSIVE}<{INT}> {RECURSIVE_S}::a<{INT}>({INT})",
          $"{RECURSIVE}<{TUPLE1}<{INT}>> {RECURSIVE}<{INT}>::wrap()",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE}<{INT}>::wrap2<{STR}>({STR})",

          $"{VOID} {RECURSIVE}<{TUPLE1}<{INT}>>::.ctor({TUPLE1}<{INT}>)",
          $"{RECURSIVE}<{TUPLE1}<{INT}>> {RECURSIVE}<{TUPLE1}<{INT}>>::a({TUPLE1}<{INT}>)",
          $"{RECURSIVE}<{TUPLE1}<{INT}>> {RECURSIVE_S}::a<{TUPLE1}<{INT}>>({TUPLE1}<{INT}>)",

          $"{VOID} {RECURSIVE}<{TUPLE2}<{INT}, {STR}>>::.ctor({TUPLE2}<{INT}, {STR}>)",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE}<{TUPLE2}<{INT}, {STR}>>::a({TUPLE2}<{INT}, {STR}>)",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE_S}::a<{TUPLE2}<{INT}, {STR}>>({TUPLE2}<{INT}, {STR}>)",

          $"{VOID} {TUPLE1}<{INT}>::.ctor({INT})",
          $"{TUPLE1}<{INT}> {TUPLE1}<{INT}>::a({INT})",
          $"{TUPLE1}<{INT}> {TUPLE1_S}::a<{INT}>({INT})",

          $"{VOID} {TUPLE2}<{INT}, {STR}>::.ctor({INT}, {STR})",
          $"{TUPLE2}<{INT}, {STR}> {TUPLE2}<{INT}, {STR}>::a({INT}, {STR})",
          $"{TUPLE2}<{INT}, {STR}> {TUPLE2_S}::a<{INT}, {STR}>({INT}, {STR})",
        }
      );
    }
    
    [Test]
    public void testRecursiveTypeWrapExtensionMethod() {
      assertAnalyze("testRecursiveTypeWrapExtensionMethod", 
        new [] {
          INT, STR,
          $"{RECURSIVE}<{INT}>",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>>",
          $"{TUPLE2}<{INT}, {STR}>"
        },
        new [] {
          $"{VOID} {RECURSIVE}<{INT}>::.ctor({INT})",
          $"{RECURSIVE}<{INT}> {RECURSIVE}<{INT}>::a({INT})",
          $"{RECURSIVE}<{INT}> {RECURSIVE_S}::a<{INT}>({INT})",

          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE}<{INT}>::wrap2<{STR}>({STR})",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE_S}::wrap2_ext<{INT}, {STR}>({RECURSIVE}<{INT}>, {STR})",

          $"{VOID} {RECURSIVE}<{TUPLE2}<{INT}, {STR}>>::.ctor({TUPLE2}<{INT}, {STR}>)",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE}<{TUPLE2}<{INT}, {STR}>>::a({TUPLE2}<{INT}, {STR}>)",
          $"{RECURSIVE}<{TUPLE2}<{INT}, {STR}>> {RECURSIVE_S}::a<{TUPLE2}<{INT}, {STR}>>({TUPLE2}<{INT}, {STR}>)",

          $"{VOID} {TUPLE2}<{INT}, {STR}>::.ctor({INT}, {STR})",
          $"{TUPLE2}<{INT}, {STR}> {TUPLE2}<{INT}, {STR}>::a({INT}, {STR})",
          $"{TUPLE2}<{INT}, {STR}> {TUPLE2_S}::a<{INT}, {STR}>({INT}, {STR})",
        }
      );
    }

    [Test]
    public void testGenericArrayViaStatic() {
      assertAnalyze("testGenericArrayViaStatic",
        new[] {INT_A},
        new[] {$"{INT_A} {TUPLE1_S}::arrIdentity<{INT}>({INT_A})"}
      );
    }

    [Test]
    public void testGenericArrayViaGenericStatic() {
      assertAnalyze("testGenericArrayViaGenericStatic",
        new[] {INT_A},
        new[] {$"{INT_A} {TUPLE1}<{INT}>::arrIdentity({INT_A})"}
      );
    }

    [Test]
    public void testPrivateInnerStruct() {
      assertAnalyze("testPrivateInnerStruct",
        new[] {
          INT, OBJ,
          $"{PRIV_INNER_STRUCT}<{INT}>",
          $"{PRIV_INNER_STRUCT_S}<{INT}>", $"{PRIV_INNER_STRUCT_S}<{INT}>[]"
        },
        new[] {
          OBJ_CTOR,
          $"{VOID} {PRIV_INNER_STRUCT}<{INT}>::.ctor()",
          $"{VOID} {PRIV_INNER_STRUCT}<{INT}>::add({INT})",
        }
      );
    }

    [Test]
    public void testPublicInnerRecursive() {
      assertAnalyze("testPublicInnerRecursive",
        new[] {
          INT, OBJ,
          $"{PUBLIC_INNER_RECURSIVE}<{INT}>",
          $"{PUBLIC_INNER_RECURSIVE_NODE}<{INT}>",
        },
        new[] {
          OBJ_CTOR,
          $"{VOID} {PUBLIC_INNER_RECURSIVE}<{INT}>::.ctor({INT})",
          $"{VOID} {PUBLIC_INNER_RECURSIVE_NODE}<{INT}>::.ctor()",
        }
      );
    }

    #endregion

    #region Virtual dispatch

    #region Non-Generic interfaces

    [Test]
    public void testNonGenericInterfaceCalling() {
      assertAnalyze("testNonGenericInterfaceCalling", 
        new [] {
          INT, OBJ, INONGENERIC, NORMAL_IDENTITY, LYING_IDENTITY
        },
        new [] {
          OBJ_CTOR,
          $"{INT} {INONGENERIC}::identity({INT})",
          $"{VOID} {NORMAL_IDENTITY}::.ctor()",
          $"{INT} {NORMAL_IDENTITY}::identity({INT})",
          $"{VOID} {LYING_IDENTITY}::.ctor()",
          $"{INT} {LYING_IDENTITY}::identity({INT})",
        }
      );
    }

    [Test]
    public void testNonGenericInterfaceChainedCalling() {
      assertAnalyze("testNonGenericInterfaceChainedCalling", 
        new [] {
          INT, OBJ, INONGENERIC, INONGENERIC2, NORMAL_IDENTITY, LYING_IDENTITY, RANDOM_IDENTITY
        },
        new [] {
          OBJ_CTOR,
          $"{INT} {INONGENERIC}::identity({INT})",
          $"{INT} {INONGENERIC2}::identity({INT})",
          $"{VOID} {NORMAL_IDENTITY}::.ctor()",
          $"{INT} {NORMAL_IDENTITY}::identity({INT})",
          $"{VOID} {LYING_IDENTITY}::.ctor()",
          $"{INT} {LYING_IDENTITY}::identity({INT})",
          $"{VOID} {RANDOM_IDENTITY}::.ctor()",
          $"{INT} {RANDOM_IDENTITY}::identity({INT})",
        }
      );
    }

    [Test]
    public void testNonGenericInterfaceCircularCalling() {
      assertAnalyze("testNonGenericInterfaceCircularCalling", 
        new [] {
          INT, OBJ, BOOL,
          INONGENERIC, INONGENERIC2, CIRCULAR_IDENTITY, CIRCULAR_IDENTITY2
        },
        new [] {
          OBJ_CTOR,
          $"{INT} {INONGENERIC}::identity({INT})",
          $"{INT} {INONGENERIC2}::identity({INT})",
          $"{VOID} {CIRCULAR_IDENTITY}::.ctor()",
          $"{INT} {CIRCULAR_IDENTITY}::identity({INT})",
          $"{VOID} {CIRCULAR_IDENTITY2}::.ctor()",
          $"{INT} {CIRCULAR_IDENTITY2}::identity({INT})",
        }
      );
    }

    [Test]
    public void testNonGenericInterfaceUnbeknownstImplementation() {
      assertAnalyze("testNonGenericInterfaceUnbeknownstImplementation", 
        new [] {
          INT, OBJ, INONGENERIC, IUNKNOWNIMPL, IUNKNOWNIMPL_EX
        },
        new [] {
          OBJ_CTOR,
          $"{INT} {INONGENERIC}::identity({INT})",
          $"{INT} {IUNKNOWNIMPL}::identity({INT})",
          $"{VOID} {IUNKNOWNIMPL}::.ctor()",
          $"{VOID} {IUNKNOWNIMPL_EX}::.ctor()",
        }
      );
    }

    #endregion

    #region Non-Generic abstract classes

    [Test]
    public void testNonGenericAbstractClassCalling() {
      assertAnalyze("testNonGenericAbstractClassCalling", 
        new [] {
          INT, OBJ, AC_NONGENERIC, AC_NORMAL_IDENTITY, AC_LYING_IDENTITY
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {AC_NONGENERIC}::.ctor()",
          $"{INT} {AC_NONGENERIC}::identity({INT})",
          $"{VOID} {AC_NORMAL_IDENTITY}::.ctor()",
          $"{INT} {AC_NORMAL_IDENTITY}::identity({INT})",
          $"{VOID} {AC_LYING_IDENTITY}::.ctor()",
          $"{INT} {AC_LYING_IDENTITY}::identity({INT})",
        }
      );
    }

    [Test]
    public void testNonGenericAbstractClassChainedCalling() {
      assertAnalyze("testNonGenericAbstractClassChainedCalling", 
        new [] {
          INT, OBJ, AC_NONGENERIC, AC_NONGENERIC2, AC_NORMAL_IDENTITY, AC_LYING_IDENTITY, AC_RANDOM_IDENTITY
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {AC_NONGENERIC}::.ctor()",
          $"{VOID} {AC_NONGENERIC2}::.ctor()",
          $"{INT} {AC_NONGENERIC}::identity({INT})",
          $"{INT} {AC_NONGENERIC2}::identity({INT})",
          $"{VOID} {AC_NORMAL_IDENTITY}::.ctor()",
          $"{INT} {AC_NORMAL_IDENTITY}::identity({INT})",
          $"{VOID} {AC_LYING_IDENTITY}::.ctor()",
          $"{INT} {AC_LYING_IDENTITY}::identity({INT})",
          $"{VOID} {AC_RANDOM_IDENTITY}::.ctor()",
          $"{INT} {AC_RANDOM_IDENTITY}::identity({INT})",
        }
      );
    }

    [Test]
    public void testNonGenericAbstractClassCircularCalling() {
      assertAnalyze("testNonGenericAbstractClassCircularCalling", 
        new [] {
          INT, OBJ, BOOL,
          AC_NONGENERIC, AC_NONGENERIC2, AC_CIRCULAR_IDENTITY, AC_CIRCULAR_IDENTITY2
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {AC_NONGENERIC}::.ctor()",
          $"{VOID} {AC_NONGENERIC2}::.ctor()",
          $"{INT} {AC_NONGENERIC}::identity({INT})",
          $"{INT} {AC_NONGENERIC2}::identity({INT})",
          $"{VOID} {AC_CIRCULAR_IDENTITY}::.ctor()",
          $"{INT} {AC_CIRCULAR_IDENTITY}::identity({INT})",
          $"{VOID} {AC_CIRCULAR_IDENTITY2}::.ctor()",
          $"{INT} {AC_CIRCULAR_IDENTITY2}::identity({INT})",
        }
      );
    }

    #endregion

    [Test]
    public void testNonGenericAbstractClassCircularCallingInterface() {
      assertAnalyze("testNonGenericAbstractClassCircularCallingInterface", 
        new [] {
          INT, OBJ, BOOL,
          INONGENERIC, AC_NONGENERIC, CIRCULAR_AC_IDENTITY, AC_CIRCULAR_IF_IDENTITY
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {AC_NONGENERIC}::.ctor()",
          $"{INT} {INONGENERIC}::identity({INT})",
          $"{INT} {AC_NONGENERIC}::identity({INT})",
          $"{VOID} {CIRCULAR_AC_IDENTITY}::.ctor()",
          $"{INT} {CIRCULAR_AC_IDENTITY}::identity({INT})",
          $"{VOID} {AC_CIRCULAR_IF_IDENTITY}::.ctor()",
          $"{INT} {AC_CIRCULAR_IF_IDENTITY}::identity({INT})",
        }
      );
    }

    #region Generic interfaces

    [Test]
    public void testGenericInterfaceSimple() {
      assertAnalyze("testGenericInterfaceSimple", 
        new [] {
          INT, OBJ, $"{IGENERIC}<{INT}>", $"{GENERIC_IDENTITY}<{INT}>"
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {GENERIC_IDENTITY}<{INT}>::.ctor()",
          $"{INT} {IGENERIC}<{INT}>::identity({INT})",
          $"{INT} {GENERIC_IDENTITY}<{INT}>::identity({INT})",
        }
      );
    }

    [Test]
    public void testGenericInterfaceExtraArg() {
      assertAnalyze("testGenericInterfaceExtraArg", 
        new [] {
          INT, FLT, OBJ,
          $"{IGENERIC}<{INT}>", $"{GENERIC_EXTRA_ARG_IDENTITY}<{INT}, {FLT}>"
        },
        new [] {
          OBJ_CTOR,
          $"{VOID} {GENERIC_EXTRA_ARG_IDENTITY}<{INT}, {FLT}>::.ctor()",
          $"{INT} {IGENERIC}<{INT}>::identity({INT})",
          $"{INT} {GENERIC_EXTRA_ARG_IDENTITY}<{INT}, {FLT}>::identity({INT})",
          $"{FLT} {GENERIC_EXTRA_ARG_IDENTITY}<{INT}, {FLT}>::fromA({INT})",
          $"{INT} {GENERIC_EXTRA_ARG_IDENTITY}<{INT}, {FLT}>::fromB({FLT})",
        }
      );
    }

    [Test]
    public void testGenericInterfaceCircular() {
      assertAnalyze("testGenericInterfaceCircular", 
        new [] {
          INT, OBJ, BOOL,
          $"{IGENERIC}<{INT}>", $"{IGENERIC2}<{INT}>",
          $"{GENERIC_CIRCULAR}<{INT}>", $"{GENERIC_CIRCULAR2}<{INT}>"
        },
        new [] {
          OBJ_CTOR,
          $"{BOOL} {OBJ}::Equals({OBJ})",
          $"{BOOL} {OBJ}::Equals({OBJ}, {OBJ})",
          $"{BOOL} {OBJ}::InternalEquals({OBJ}, {OBJ})",
          $"{VOID} {GENERIC_CIRCULAR}<{INT}>::.ctor()",
          $"{VOID} {GENERIC_CIRCULAR2}<{INT}>::.ctor()",
          $"{INT} {IGENERIC}<{INT}>::identity({INT})",
          $"{INT} {IGENERIC2}<{INT}>::identity({INT})",
          $"{INT} {GENERIC_CIRCULAR}<{INT}>::identity({INT})",
          $"{INT} {GENERIC_CIRCULAR2}<{INT}>::identity({INT})",
        }
      );
    }
    
    [Test]
    public void testGenericInterfaceUnbeknownstImplementation() {
      assertAnalyze("testGenericInterfaceUnbeknownstImplementation", 
        new [] {
          INT, OBJ, $"{IGENERIC}<{INT}>",
          $"{IGEN_UNKNOWNIMPL}<{INT}>", $"{IGEN_UNKNOWNIMPL_EX}<{INT}>"
        },
        new [] {
          OBJ_CTOR,
          $"{INT} {IGENERIC}<{INT}>::identity({INT})",
          $"{INT} {IGEN_UNKNOWNIMPL}<{INT}>::identity({INT})",
          $"{VOID} {IGEN_UNKNOWNIMPL}<{INT}>::.ctor()",
          $"{VOID} {IGEN_UNKNOWNIMPL_EX}<{INT}>::.ctor()",
        }
      );
    }

    #endregion

    #endregion

    [Test]
    public void testMethodUniqueness() {
      var data = analyze("testMethodUniqueness");
      Assert.AreEqual(
        data.usedTypes.Count, data.data.usedTypes.Count,
        "all used types should be unique by their expanded name"
      );
      Assert.AreEqual(
        data.analyzedMethods.Count, data.data.analyzedMethods.Count,
        "all analyzed methods should be unique by their expanded name"
      );
    }

    [Test]
    public void testLinqSelect() {
      assertAnalyze("testLinqSelect",
        new[] {INT},
        new[] {$"{TUPLE1}<{INT}>"}
      );
    }
  }
}
