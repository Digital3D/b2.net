using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using com.wibblr.utils;

namespace com.wibblr.b2.console
{
    public class DownloadCommand
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

            [Option('f', "filename", HelpText = "Filename to download")]
            public string FileName { get; set; }

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
                options.Destination = Environment.CurrentDirectory;

            return await DownloadFiles(options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task<int> DownloadFiles(Options options) {

            var b2 = new B2();
            b2.Login(options.AccountId, options.ApplicationKey, options.BucketName).Wait();

            string downloadUrl = b2.DownloadUrl;
            string bucketName = options.BucketName;
            string fileName = options.FileName;

            string accountAuthorizationToken = b2.AuthorizationToken;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create($"{downloadUrl}/file/{bucketName}/{fileName}");
            webRequest.Method = "GET";
            webRequest.Headers.Add("Authorization", accountAuthorizationToken);
            WebResponse response = (HttpWebResponse) await webRequest.GetResponseAsync();
            
            Stream responseStream = response.GetResponseStream();
            byte[] fileBytes;
            using (BinaryReader br = new BinaryReader(responseStream))
            {
                fileBytes = br.ReadBytes(500000);
                br.Close();
            }
            responseStream.Close();
            response.Close();
            string downloadsFolder = options.Destination;
            FileStream saveFile = new FileStream(downloadsFolder, FileMode.Create);
            BinaryWriter writeFile = new BinaryWriter(saveFile);
            try
            {
                writeFile.Write(fileBytes);
            }
            finally
            {
                saveFile.Close();
                writeFile.Close();
            }

            return 0;
        }
    }
}
