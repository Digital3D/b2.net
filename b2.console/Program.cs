using System;
using System.Linq;

namespace com.wibblr.b2.console
{
    class Program
    {
        public static void Usage()
        {
            Console.WriteLine(@"Usage: b2 auth | upload | uploadLargeFile [source|file] [destination]
                                    or 'b2 auth|upload|uploadLargeFile help' for help with subcommands");
                                }

        public static void Main(string[] args)
        {
            Environment.Exit(new Program().Run(args));
        }

        public int Run(params string[] args)
        {
            int rc = 0;

            if (args.Length < 1)
            {
                Usage();
                rc = 1;
            }
            else
            {
                try
                {
                    var subArgs = args.Skip(1).ToArray();

                    if ("auth" == args[0])
                        rc = new AuthCommand().Run(subArgs);
                    else if ("upload" == args[0])
                        rc = new UploadCommand().Run(subArgs).GetAwaiter().GetResult();
                    else if ("uploadLargeFile" == args[0])
                        rc = new UploadCommand().Run(subArgs, true).GetAwaiter().GetResult();
                    else if ("help" == args[0])
                        Usage();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    while (e.InnerException != null)
                    {
                        Console.Error.WriteLine(e.InnerException.Message);
                        e = e.InnerException;
                    }

                    rc = 1;

                }
            }


#if DEBUG
            Console.ReadLine();
#endif

            return rc;
        }
    }
}
