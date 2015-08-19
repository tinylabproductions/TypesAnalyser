using NUnit.Framework;
using TypesAnalyser.Cecil;

namespace TypesAnalyserTest.Cecil {
  [TestFixture]
  class VirtualMethodsTest {
    [Test]
    public void testDegenerifyMethodName() {
      Assert.AreEqual(
        "TestData.IExplicitGenInterface`1.identity", 
        VirtualMethods.degenerifyMethodName("TestData.IExplicitGenInterface<A>.identity")
      );
      Assert.AreEqual(
        "TestData.IExplicitGenInterface`2.identity", 
        VirtualMethods.degenerifyMethodName("TestData.IExplicitGenInterface<A,B>.identity")
      );
      Assert.AreEqual(
        "TestData.IExplicitGenInterface`2.identity", 
        VirtualMethods.degenerifyMethodName("TestData.IExplicitGenInterface<A, B>.identity")
      );
      Assert.AreEqual(
        "TestData.IExplicitGenInterface`3.identity", 
        VirtualMethods.degenerifyMethodName("TestData.IExplicitGenInterface<A, B, C>.identity")
      );
      Assert.AreEqual(
        "TestData.IExplicitInterface.identity", 
        VirtualMethods.degenerifyMethodName("TestData.IExplicitInterface.identity")
      );
    }
  }
}
