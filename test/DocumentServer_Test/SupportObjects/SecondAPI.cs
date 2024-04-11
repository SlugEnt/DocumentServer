using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


    // Starts up an instance of Vault for development and testing purposes.
    public static void StartAPI(string hostname,
                                string connectionString)
    {
        if (IsInitialized)
            return;

        string apiArgs = " --override-hostname " + hostname + " --port " + 9898 + " --db \"" + connectionString + "\"";


        string apiBin  = "SlugEnt.DocumentServer.API.exe";
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


            if (_process.HasExited)
            {
                throw new Exception($"Process could not be started: {_process.StandardError}");
            }

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting SecondAPI: {0}", ex.ToString());
        }
    }


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