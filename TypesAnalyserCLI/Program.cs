using System;
using System.Threading;
using TypesAnalyser;

namespace TypesAnalyserCLI {
  class Program {
    static void Main(string[] args) {
      var t = new Thread(() => {
        foreach (var arg in args) {
          Console.WriteLine("Analyzing " + arg);
          var data = Analyser.analyze(arg, EntryPoint.create, new StdoutLogger());

          Console.WriteLine();
          Console.WriteLine("analyzed methods:");
          foreach (var method in data.analyzedMethods) {
            Console.WriteLine(method);
          }
          Console.WriteLine();
          Console.WriteLine("Used types:");
          foreach (var type in data.usedTypes) {
            Console.WriteLine(type);
          }
        }
        Console.WriteLine("Done. Press any key to exit.");
        Console.Read();
      }, 1024 * 1024 * 64);
      t.Start();
      t.Join();
    }
  }
}
