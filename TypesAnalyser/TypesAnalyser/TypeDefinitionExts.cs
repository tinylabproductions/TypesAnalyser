using System.Linq;
using Mono.Cecil;

namespace TypesAnalyser {
  public static class TypeDefinitionExts {
    public static bool isDelegate(this TypeDefinition type) {
      if (type.BaseType == null) return false;
      return type.BaseType.FullName == "System.Delegate" || type.BaseType.Resolve().isDelegate();
    }
  }
}
