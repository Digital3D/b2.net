using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using com.wibblr.utils;

namespace com.wibblr.b2.console
{
    public class UploadCommand
    {
        /// <summary>
        /// 
        /// </summary>
        internal class Options
        {
            [Option('a', "account", HelpText = "Account ID to use")]
            public string AccountId { get; set; }

            [Option('k', "appkey", HelpText = "Application Key to use")]
            public string ApplicationKey { get; set; }

            [Option('b', "bucket", HelpText = "Bucket name to use", DefaultValue = "wibblr")]
            public string BucketName { get; set; }

            [Option('r', "recursive", HelpText = "Copy files recursively, if the source is a directory", DefaultValue = true)]
            public bool Recursive { get; set; }

            [Option('j', "parallel", HelpText = "Maximum number of simultaneous network connections", DefaultValue = 4)]
            public int SimultaneousConnections { get; set; }

            [ValueOption(0)]
            public string Source { get; set; }

            [ValueOption(1)]
            public string Destination { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public int Run(string[] args)
        {
            var options = new Options();

            if (!Parser.Default.ParseArguments(args, options))
                throw new Exception(options.GetUsage());

            if (options.AccountId == null || options.ApplicationKey != null)
            {
                var c = Credentials.Read();
                if (options.AccountId == null)
                    options.AccountId = c.accountId;

                if (options.ApplicationKey == null)
                    options.ApplicationKey = c.applicationKey;
            }

            if (options.SimultaneousConnections < 1 || options.SimultaneousConnections > 8)
                throw new Exception("Invalid option 'parallel' - must be between 1 and 8");

            if (options.Source == null)
                throw new Exception("No source specified");

            if (options.Destination == null)
                throw new Exception("No destination specified");

            return UploadFiles(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        internal int UploadFiles(Options options) {
            var b2 = new B2();
            b2.Login(options.AccountId, options.ApplicationKey, options.BucketName).Wait();

            var scannedFiles = FilesystemScanner.Scan(options.Source, options.Recursive);

            var tasks = new Dictionary<Task, string>();

            foreach (var file in scannedFiles)
            {
                while (tasks.Count >= options.SimultaneousConnections)
                {
                    var tempTasks = tasks.Keys.ToArray();
                    var i = Task.WaitAny(tempTasks);
                    if (i >= 0)
                    {
                        var completedTask = tempTasks[i];
                        var path = tasks[completedTask];
                        if (completedTask.IsFaulted || completedTask.IsCanceled)
                        {
                            var error = "Unknown error";
                            if (completedTask.Exception != null)
                            {
                                error = completedTask.Exception.Message;
                                if (completedTask.Exception.InnerException != null)
                                    error = completedTask.Exception.InnerException.Message;
                            }
                            Console.Error.WriteLine(path + ": " + error);
                        }
                        else
                            Console.WriteLine(path);

                        tasks.Remove(completedTask);
                    }
                }
                var task = b2.UploadFile(file.Info.FullName, file.RelativePath.ToUnixPath());
                tasks[task] = file.Info.FullName;
            }

            //wait when finish
            foreach (KeyValuePair<Task, string> pair in tasks)
            {
                pair.Key.Wait();
            }
            
            return 0;
        }
    }
}
