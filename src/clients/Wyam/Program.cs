﻿using System;
using Wyam.Commands;
using Wyam.Common.Tracing;
using Wyam.Configuration.Preprocessing;
using Wyam.Core.Execution;
using Wyam.Tracing;

namespace Wyam
{
    /// <summary>
    /// The primary console entry point.
    /// </summary>
    public class Program
    {
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEvent;
            Program program = new Program();
            return program.Run(args);
        }

        private static void UnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs e)
        {
            // Exit with a error exit code
            Exception exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                Trace.Critical(exception.Message);
                Trace.Verbose(exception.ToString());
            }
            Environment.Exit((int)ExitCode.UnhandledError);
        }

        private int Run(string[] args)
        {
            // Add a default trace listener
            Trace.AddListener(new SimpleColorConsoleTraceListener { TraceOutputOptions = System.Diagnostics.TraceOptions.None });

            // Output version info
            Trace.Information($"Wyam version {Engine.Version}");

            // Make sure we're not running under Mono
            if (Type.GetType("Mono.Runtime") != null)
            {
                Trace.Critical("The Mono runtime is not supported. Please check the GitHub repository and issue tracker for information on .NET Core support for cross platform execution.");
                return (int)ExitCode.UnsupportedRuntime;
            }

            // Parse the command line
            Preprocessor preprocessor = new Preprocessor();
            Command command;
            try
            {
                bool hasParseArgsErrors;
                command = CommandParser.Parse(args, preprocessor, out hasParseArgsErrors);
                if (command == null)
                {
                    return hasParseArgsErrors ? (int)ExitCode.CommandLineError : (int)ExitCode.Normal;
                }
            }
            catch (Exception ex)
            {
                Trace.Error("Error while parsing command line: {0}", ex.Message);
                if (Trace.Level == System.Diagnostics.SourceLevels.Verbose)
                {
                    Trace.Error("Stack trace:{0}{1}", Environment.NewLine, ex.StackTrace);
                }
                return (int)ExitCode.CommandLineError;
            }

            // Run the command
            return (int)command.Run(preprocessor);
        }
    }
}
