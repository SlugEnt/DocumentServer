using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using SlugEnt.FluentResults;

namespace Test_DocumentServer.SupportObjects;

/// <summary>
/// Performs Unit Test first time setup of the Vault server.  
///   - Starts the vault server in Dev mode, with a known root token.
///   - Scans the startup process to look for the unseal key.
/// </summary>
public static class SecondAPI
{
    private static Process _process;
    private static bool    _disposed;

    private static bool _startingUP = true;

    static SecondAPI() { }

    public static bool IsInitialized { get; private set; }

    public static string Hostname { get; private set; }
    public static int Port { get; private set; } = 9898;
    public static string ConnectionString { get; private set; }
    public static string NodeKey { get; private set; }


    /// <summary>
    /// Number of times we have retried the start logic.
    /// </summary>
    public static int RetryCounter { get; private set; } = 0;


    // Starts up an instance of the DocumentServer API for development and testing purposes.
    public static Result StartAPI(string hostname,
                                  string connectionString,
                                  string nodeKey)
    {
        if (IsInitialized)
        {
            StartUpResult = Result.Ok();
            return StartUpResult;
        }

        Hostname         = hostname;
        ConnectionString = connectionString;
        NodeKey          = nodeKey;


        string apiArgs = " --nodekey " + NodeKey + " --override-hostname " + Hostname + " --port " + Port + " --db \"" + ConnectionString + "\"" + " --logtofile";

        string apiName = "SlugEnt.DocumentServer.API";
        string apiBin  = apiName + ".exe";
        string apiPath = @"D:\A_Dev\SlugEnt\DocumentServer\src\DocumentServer.API\bin\Debug\net8.0";

        var startInfo = new ProcessStartInfo(apiBin)
        {
            UseShellExecute        = false,
            WindowStyle            = ProcessWindowStyle.Normal,
            CreateNoWindow         = false,
            RedirectStandardError  = true,
            RedirectStandardOutput = true,
            WorkingDirectory       = apiPath,
            Arguments              = apiArgs,
        };

        try
        {
            // Startup the API
            startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

            // Build the process
            _process = new Process
            {
                StartInfo = startInfo
            };


            _process.OutputDataReceived += (sender,
                                            eventArgs) => CaptureOutput(sender, eventArgs);
            _process.ErrorDataReceived += (sender,
                                           eventArgs) => CaptureError(sender, eventArgs);


            if (!_process.Start())
            {
                throw new Exception($"Process did not start successfully: {_process.StandardError}");
            }

            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            Debug.WriteLine("SecondAPI - Process ID: " + _process.Id);

            while (SecondAPI._startingUP == true && _process.HasExited == false)
            {
                Thread.Sleep(10);
            }


            // If the process has exited then something went wrong.
            if (_process.HasExited)
            {
                // Lets see if a process from a previous run is still running.  If so, kill it.
                Process[] allApis = Process.GetProcessesByName(apiName);
                if (allApis.Length == 0)
                    throw new Exception($"Process could not be started for unknown reason: {_process.StandardError}");

                foreach (Process app in allApis)
                {
                    app.Kill(true);
                }

                // Recheck to make sure they all were killed
                allApis = Process.GetProcessesByName(apiName);
                if (allApis.Length > 0)
                {
                    string[] processes    = new string[allApis.Length];
                    string   processesAll = String.Join(", ", processes);
                    throw new ApplicationException("There are " + apiName + " 's running from prior runs.  Attemped to kill them to no avail.  There process id's are: " +
                                                   processesAll);
                }
                else
                {
                    RetryCounter++;
                    return StartAPI(hostname, connectionString, nodeKey);
                }
            }

            IsInitialized = true;
            StartUpResult = Result.Ok();
            return StartUpResult;
        }
        catch (Exception ex)
        {
            StartUpResult = Result.Fail(new Error("StartApi Failed: " + ex.ToString()));
            return StartUpResult;
        }
    }


    public static Result StartUpResult { get; private set; }


    /// <summary>
    /// Stops the Second API Server
    /// </summary>
    public static void StopSecondAPI()
    {
        _process.CloseMainWindow();
        _process.Kill();
    }



    // Following methods are used to output API messages to the Debug window.


    static void CaptureOutput(object sender,
                              DataReceivedEventArgs e)
    {
        ShowOutput(e.Data, true);

        // If starting up then we need to look for some stuff.
        if (SecondAPI._startingUP)
        {
            if (e.Data?.StartsWith("API Successfully Started") == true)
            {
                SecondAPI._startingUP = false;
            }
        }
    }


    static void CaptureError(object sender,
                             DataReceivedEventArgs e)
    {
        ShowOutput(e.Data, false);
    }


    static void ShowOutput(string data,
                           bool stdOutput)
    {
        string cat;
        if (stdOutput)
        {
            cat = "SecondAPI: ";
        }
        else
        {
            cat = "SecondAPIErr:";
        }


        // Write the line to the Debug window.
        Debug.WriteLine(data, cat);

        /*
        // Now look for successful start message.
        if (data?.StartsWith("==> Vault server started!") == true)
        {
            VaultServerInstance._startingUP = false;
            return;
        }

        if (data?.StartsWith("Unseal Key:") == true)
        {
            VaultServerRef.unSealKey = data.Substring("Unseal Key:".Length + 1);
        }
        */
    }
}