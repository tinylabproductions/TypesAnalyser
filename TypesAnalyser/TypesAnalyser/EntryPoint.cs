using System.Collections.Generic;
using System.Linq;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  public interface IEntryPoint {
    IEnumerable<MethodDefinition> entryMethods { get; }
  }

  public static class EntryPoint {
    public static Option<IEntryPoint> create(TypeDefinition type) { return UnityEntryPoint.create(type); }

    public static bool canBeEntryPoint(TypeDefinition type) { return !type.IsGenericInstance; }
    public static bool canBeEntryPoint(MethodDefinition type) { return !type.IsGenericInstance; }
  }

  public class UnityEntryPoint : IEntryPoint {
    public readonly ExpandedType type;

    public UnityEntryPoint(ExpandedType type) {
      this.type = type;
    }

    public override string ToString() {
      return $"UnityEntryPoint[{type}]";
    }

    static bool isEntryPoint(TypeDefinition type) {
      if (type.BaseType == null) return false;
      else {
        return EntryPoint.canBeEntryPoint(type) && (
                 type.BaseType.FullName == "UnityEngine.MonoBehaviour"
                 || isEntryPoint(type.BaseType.Resolve())
               );
      }
    }

    public static Option<IEntryPoint> create(TypeDefinition type) {
      return isEntryPoint(type).opt<IEntryPoint>(() => new UnityEntryPoint(
        ExpandedType.create(type, ExpandedType.EMPTY_GENERIC_LOOKUP)
      ));
    }

    public IEnumerable<MethodDefinition> entryMethods {
      // TODO: this probably needs to filter based on all unity callback names.
      get { return type.definition.Methods.Where(EntryPoint.canBeEntryPoint); }
    }
  }
}
