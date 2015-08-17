using System;
using TypesAnalyser;

namespace TypesAnalyserCLI {
  class Program {
    static void Main(string[] args) {
      foreach (var arg in args) {
        Console.WriteLine("Analyzing " + arg);
        var data = Analyser.analyze(arg, EntryPoint.create, new StdoutLogger());

        foreach (var method in data.analyzedMethods) {
          Console.WriteLine("analyzed method: " + method);
        }
        foreach (var type in data.usedTypes) {
          Console.WriteLine("used type: " + type);
        }
      }
      Console.WriteLine("Done. Press any key to exit.");
      Console.Read();
    }
  }
}
