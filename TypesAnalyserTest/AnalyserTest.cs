using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using com.tinylabproductions.TLPLib.Extensions;
using Mono.Cecil;
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

    void assertAnalyze(
      string name, 
      IEnumerable<string> usedTypes, IEnumerable<string> analyzedMethods
    ) {
      var assertedUsedTypes = usedTypes.ToImmutableSortedSet().Add(VOID).Add(TEP);
      var assertedAnalyzedMethods = analyzedMethods.ToImmutableSortedSet()
        .Add($"{VOID} {TEP}::{testMethodName(name)}()");
      var data = AnalyserTestData.create(Analyser.analyze(analyserTestMethod(name), logger));

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
      TD = "TestData",
      VOID = "System.Void",
      INT = "System.Int32",
      STR = "System.String",
      OBJ = "System.Object",
      OBJ_CTOR = "System.Void System.Object::.ctor()",
      INT_WRAPPER = "TestData.IntWrapper",
      INT_WRAPPER2 = "TestData.IntWrapper2",
      RECURSIVE = "TestData.Recursive`1",
      RECURSIVE_S = "TestData.Recursive",
      TUPLE1_S = "TestData.Tuple1",
      TUPLE1 = "TestData.Tuple1`1",
      TUPLE1_C = "TestData.Tuple1C`1",
      TUPLE2_S = "TestData.Tuple2",
      TUPLE2 = "TestData.Tuple2`2",
      INONGENERIC = "TestData.INonGeneric",
      INONGENERIC2 = "TestData.INonGeneric2",
      CIRCULAR_IDENTITY = "TestData.CircularIdentity",
      CIRCULAR_IDENTITY2 = "TestData.CircularIdentity2",
      NORMAL_IDENTITY = "TestData.NormalIdentity",
      LYING_IDENTITY = "TestData.LyingIdentity",
      RANDOM_IDENTITY = "TestData.RandomIdentity"
    ;

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

    #endregion

    #region Virtual dispatch

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
          INT, OBJ, INONGENERIC, INONGENERIC2, CIRCULAR_IDENTITY, CIRCULAR_IDENTITY2
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

    #endregion
  }
}
