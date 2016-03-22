using System;
using System.IO;

namespace com.wibblr.b2.console
{
    public class ConsoleCapture : IDisposable
    {
        StringWriter stdOut = new StringWriter();
        StringWriter stdError = new StringWriter();

        public ConsoleCapture()
        {
            Console.SetOut(stdOut);
            Console.SetError(stdError);
        }

        public void Dispose()
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()).WithAutoFlush());
            Console.SetError(new StreamWriter(Console.OpenStandardError()).WithAutoFlush());
        }

        public string StandardOutput { get { return stdOut.ToString(); } }

        public string StandardError { get { return stdError.ToString(); } }
    }
}
