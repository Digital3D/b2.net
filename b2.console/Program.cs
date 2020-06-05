using System;
using System.Linq;

namespace com.wibblr.b2.console
{
    class Program
    {
        public static void Usage()
        {
            Console.WriteLine("Usage: 1) b2 auth -a [AppKey] -k [SecretKey] => create your authentication file to %appdata%/Roaming/b2/credentials.json");
            Console.WriteLine("       2) b2 upload | uploadLargeFile [directory | single file] [destination or / for root of bucket] -b [BucketName] => Upload directory or file in bucket");
            Console.WriteLine("       3) b2 auth -d => to delete your credentials from %appdata%/Roaming/b2/credentials.json");
            Console.WriteLine("or b2 auth|upload|uploadLargeFile help' for help with subcommands");
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
                    {
                        rc = new AuthCommand().Run(subArgs);
                        if(rc == 0) Console.WriteLine("OK");
                    }
                    else if ("upload" == args[0])
                    {
                        rc = new UploadCommand().Run(subArgs).GetAwaiter().GetResult();
                        if (rc == 0) Console.WriteLine("OK");
                    }
                    else if ("download" == args[0])
                    {
                        rc = new UploadCommand().Run(subArgs).GetAwaiter().GetResult();
                        if (rc == 0) Console.WriteLine("OK");
                    }
                    else if ("uploadLargeFile" == args[0])
                    {
                        rc = new UploadCommand().Run(subArgs, true).GetAwaiter().GetResult();
                        if (rc == 0) Console.WriteLine("OK");
                    }
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
