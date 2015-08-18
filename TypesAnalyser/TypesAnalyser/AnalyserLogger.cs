using System;
using System.IO;
using System.Text;

namespace TypesAnalyser {
  public interface AnalyserLogger {
    void incIndent();
    void decIndent();
    void log(string prefix, ExpandedMethod method);
    void log(ExpandedType type, string msg);
    void log(string msg);
  }

  public abstract class PrintingLogger : AnalyserLogger {
    uint indent;

    public void incIndent() { indent += 2; }
    public void decIndent() { indent -= 2; }
    public void log(string prefix, ExpandedMethod method) { log(prefix + " " + method); }
    public void log(ExpandedType type, string msg) { log("[" + type.name + "] " + msg); }
    public void log(string msg) {
      indentLog(indent);
      writeLine(msg);
    }

    void indentLog(uint indentation) {
      var sb = new StringBuilder();
      for (var idx = 0u; idx < indentation; idx++) sb.Append(' ');
      write(sb.ToString());
    }

    abstract protected void write(string msg);
    abstract protected void writeLine(string msg);
  }

  public class StdoutLogger : PrintingLogger {
    protected override void write(string msg) { Console.Write(msg); }
    protected override void writeLine(string msg) { Console.WriteLine(msg); }
  }

  public class StreamWriterLogger : PrintingLogger {
    readonly StreamWriter writer;

    public StreamWriterLogger(StreamWriter writer) { this.writer = writer; }

    protected override void write(string msg) { writer.Write(msg); }
    protected override void writeLine(string msg) { writer.WriteLine(msg); }
  }

  public class NoopLogger : AnalyserLogger {
    public void incIndent() {}
    public void decIndent() {}
    public void log(string prefix, ExpandedMethod method) {}
    public void log(ExpandedType type, string msg) {}
    public void log(string msg) {}
  }
}
