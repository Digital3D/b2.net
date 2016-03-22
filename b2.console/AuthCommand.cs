using System;

using CommandLine;
using CommandLine.Text;

using com.wibblr.b2;

namespace com.wibblr.b2.console
{
    /// <summary>
    /// 
    /// </summary>
    class AuthCommand
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

            [Option('d', "delete", HelpText = "Delete stored credentials file")]
            public bool Delete { get; set; }

            [Option('p', "path", HelpText = "Path to stored credentials file (defaults to ~/b2.net/credentials.json)")]
            public string Path { get; set; }

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
        public int Run(params string[] args)
        {
            var options = new Options();

            if (!Parser.Default.ParseArguments(args, options))
                throw new Exception(options.GetUsage());

            if (options.Delete)
            {
                if (options.AccountId != null || options.ApplicationKey != null)
                    throw new Exception("Delete option cannot be combined with account or appkey options.");

                Credentials.Delete(options.Path);
            }
            else if (options.AccountId != null || options.ApplicationKey != null)
            {
                if (options.AccountId == null || options.ApplicationKey == null)
                    throw new Exception("Both account and appkey options must be specified.");

                Credentials.Write(options.AccountId, options.ApplicationKey, options.Path);
            }
            else
            {
                var c = Credentials.Read(options.Path);
                Console.WriteLine($"Account Id: '{c.accountId}',  Application Key: '{c.applicationKey}'");           
            }
            return 0;
        }
    }
}
