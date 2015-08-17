﻿using System.Linq;
using Mono.Cecil;

namespace TypesAnalyser {
  public static class TypeDefinitionExts {
    public static bool implements(this TypeDefinition type, TypeDefinition iface) {
      if (type.Interfaces.Any(i => i == iface)) return true;
      var baseTRef = type.BaseType;
      return baseTRef != null && baseTRef.Resolve().implements(iface);
    }
  }
}