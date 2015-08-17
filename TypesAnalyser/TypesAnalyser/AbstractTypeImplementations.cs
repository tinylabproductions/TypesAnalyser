using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using com.tinylabproductions.TLPLib.Extensions;
using Mono.Cecil;

namespace TypesAnalyser {
  public struct AbstractTypeImplementations {
    public readonly ImmutableDictionary<TypeDefinition, ImmutableHashSet<TypeDefinition>> typeImpls;
//    public readonly ImmutableDictionary<MethodDefinition, ImmutableHashSet<MethodDefinition>> methodImpls;

    public AbstractTypeImplementations(ImmutableDictionary<TypeDefinition, ImmutableHashSet<TypeDefinition>> typeImpls) {
      this.typeImpls = typeImpls;
//      this.methodImpls = methodImpls;
    }

    public static AbstractTypeImplementations create(IEnumerable<TypeDefinition> allTypes) {
      var typeImpls = allAbstractTypeImplementations(allTypes);
//      var methodImpls = typeImpls.Aggregate(
//
//      )

//        MetadataResolver.GetMethod(impl.Methods, method.definition)
      return new AbstractTypeImplementations(typeImpls);
    }

    /* All abstract types (abstract classes & interfaces) implemented by given type. */
    public static IEnumerable<TypeDefinition> allImplementedAbstractTypes(TypeDefinition typedef) {
      var ifaces = typedef.Interfaces.Select(r => r.Resolve());
      if (typedef.BaseType != null) {
        var baseT = typedef.BaseType.Resolve();
        var parent =
          baseT.Attributes.HasFlag(TypeAttributes.Abstract)
            ? new[] {baseT}
            : Enumerable.Empty<TypeDefinition>();
        return ifaces.Concat(parent).Concat(allImplementedAbstractTypes(baseT));
      }
      return ifaces;
    }

    /* Map from abstract type to a list of all concrete implementations of that type. */
    public static ImmutableDictionary<TypeDefinition, ImmutableHashSet<TypeDefinition>> 
      allAbstractTypeImplementations(IEnumerable<TypeDefinition> allTypes)
    {
      return allTypes.Aggregate(
        ImmutableDictionary<TypeDefinition, ImmutableHashSet<TypeDefinition>>.Empty,
        (dictionary, typedef) => {
          Act<TypeDefinition> addImplementer = abstractType => {
            var currentImplementers = dictionary.getOrElse(
              abstractType, () => ImmutableHashSet<TypeDefinition>.Empty
            );
            dictionary = dictionary.SetItem(abstractType, currentImplementers.Add(typedef));
          };

          foreach (var abstractType in allImplementedAbstractTypes(typedef))
            addImplementer(abstractType);

          return dictionary;
        }
      );
    }

//    /* Map from abstract type to a list of all concrete implementations of that type. */
//    public static ImmutableDictionary<MethodDefinition, ImmutableHashSet<MethodDefinition>> 
//      allAbstractMethodImplementations(IEnumerable<TypeDefinition> allTypes)
//    {
//      return allTypes.Aggregate(
//        ImmutableDictionary<MethodDefinition, ImmutableHashSet<MethodDefinition>>.Empty,
//        (dictionary, typedef) => {
//          Act<MethodDefinition> addImplementer = abstractMethod => {
//            var currentImplementers = dictionary.getOrElse(
//              abstractMethod, () => ImmutableHashSet<MethodDefinition>.Empty
//            );
//            dictionary = dictionary.SetItem(abstractMethod, currentImplementers.Add(typedef));
//          };
//
//          foreach (var abstractType in allImplementedAbstractTypes(typedef))
//            addImplementer(abstractType);
//
//          return dictionary;
//        }
//      );
//    }
  }
}
