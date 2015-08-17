using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using TypesAnalyser;

namespace TypesAnalyserTest.Data {
  struct AnalyserTestData {
    public readonly AnalyzerData data;
    public readonly ImmutableSortedSet<string> usedTypes;
    public readonly ImmutableSortedSet<string> analyzedMethods;

    public AnalyserTestData(
      AnalyzerData data, 
      ImmutableSortedSet<string> usedTypes, ImmutableSortedSet<string> analyzedMethods
    ) {
      this.data = data;
      this.usedTypes = usedTypes;
      this.analyzedMethods = analyzedMethods;
    }

    public static AnalyserTestData create(AnalyzerData data) {
      return new AnalyserTestData(
        data,
        data.usedTypes.Select(t => t.name).ToImmutableSortedSet(),
        data.analyzedMethods.Select(t => t.name).ToImmutableSortedSet()
      );
    }

    public void assertUsed(string name) { Assert.Contains(name, usedTypes); }
    public void assertUsed(IEnumerable<string> names) { foreach (var name in names) assertUsed(name); }

    public void assertAnalysed(string name) { Assert.Contains(name, analyzedMethods); }
    public void assertAnalysed(IEnumerable<string> names) { foreach (var name in names) assertAnalysed(name); }
  }
}
