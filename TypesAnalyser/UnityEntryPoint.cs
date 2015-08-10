using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  struct UnityEntryPoint {
    public readonly ExpandedType type;

    public UnityEntryPoint(ExpandedType type) {
      this.type = type;
    }

    public override string ToString() {
      return $"UnityEntryPoint[{type}]";
    }

    static bool isEntryPoint(TypeDefinition type) {
      return type.FullName == "UnityEngine.MonoBehaviour" ||
        (type.BaseType != null && isEntryPoint(type.BaseType.Resolve()));
    }

    public static Option<UnityEntryPoint> create(TypeDefinition type) {
      return isEntryPoint(type).opt(() => new UnityEntryPoint(ExpandedType.create(type)));
    }
  }
}
