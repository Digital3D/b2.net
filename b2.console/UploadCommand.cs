using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        /// <param name="largeFile"></param>
        /// <returns></returns>
        public async Task<int> Run(string[] args, bool largeFile = false)
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
                options.Destination = "/";

            if(!largeFile)
                return await UploadFiles(options);

            return await UploadLargeFile(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task<int> UploadFiles(Options options) {
            var b2 = new B2();
            b2.Login(options.AccountId, options.ApplicationKey, options.BucketName).Wait();
            b2.OnLargeFileUploadProgress += OnLargeFileUploadProgress;

            var scannedFiles = FilesystemScanner.Scan(options.Source, options.Recursive);
            Progress<StreamProgress> progress = new Progress<StreamProgress>();
            progress.ProgressChanged += Progress_ProgressChanged;

            var tasks = new Dictionary<Task, string>();

            foreach (var file in scannedFiles)
            {
                if (file.Length < 100 * (1000 * 1000))
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
                                    while (completedTask.Exception.InnerException != null)
                                        error += "\r\n" + completedTask.Exception.InnerException.Message;
                                }

                                Console.Error.WriteLine(path + ": " + error);
                            }
                            else
                                Console.WriteLine(path);

                            tasks.Remove(completedTask);
                        }
                    }

                    await b2.UploadFile(file.Info.FullName, file.RelativePath.ToUnixPath(), "application/octet-stream", null, progress);
                }
                else
                {
                    Console.WriteLine($"File too large than 100Mb detected, use UploadLargeFile instead of normal upload - file name => {file.Info.Name}");
                    await UpLargeFile(file, b2);
                }
            }
            
            return 0;
        }

        private async Task<int> UploadLargeFile(Options options)
        {
            var b2 = new B2();
            b2.Login(options.AccountId, options.ApplicationKey, options.BucketName).Wait();

            var scannedFiles = FilesystemScanner.Scan(options.Source, options.Recursive);

            foreach (var file in scannedFiles)
            {
                await UpLargeFile(file, b2);
            }

            return 0;
        }

        private async Task UpLargeFile(ScannedFileSystemInfo file, B2 b2)
        {
            if (file.Length >= 100 * (1000 * 1000))
            {
                Console.WriteLine("Wait...Upload large file => " +file.Info.Name);
                string result = await b2.UploadLargeFile(b2.BucketId, file.Info.Name, b2.ApiUrl, b2.AuthorizationToken);
                string fileId = Regex.Match(result, "fileId\": \"(.*?)\"").Groups[1].Value;
                string partUrl = await b2.UploadLargeFilePartUrl(fileId, b2.ApiUrl, b2.AuthorizationToken);
                string uploadUrl = Regex.Match(partUrl, "uploadUrl\": \"(.*?)\"").Groups[1].Value;
                string authorizationToken = Regex.Match(partUrl, "authorizationToken\": \"(.*?)\"").Groups[1].Value;
                ArrayList result3 = await b2.UploadPartOfFile(file.Info.FullName, uploadUrl, authorizationToken);
                await b2.LargeFileUploadFinished(fileId, b2.ApiUrl, b2.AuthorizationToken, result3);
                Console.WriteLine($"File {file.Info.Name} Uploaded!");
            }
        }

        private void OnLargeFileUploadProgress(long totalBytesSent, long localFileSize, string partNumber)
        {
            Console.WriteLine($"{((totalBytesSent / 1024) / 1024):0}Mb sent on {((localFileSize / 1024) / 1024):0}Mb - sha1:{partNumber}");
        }

        private void Progress_ProgressChanged(object sender, StreamProgress e)
        {
            Console.WriteLine($"{e.progress:0.00}% - {(e.bytesPerSecond * 1024):0.00}Mb");
        }
    }
}
