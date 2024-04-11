using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_DocumentServer.SupportObjects;

/// <summary>
/// Performs Unit Test first time setup of the Vault server.  
///   - Starts the vault server in Dev mode, with a known root token.
///   - Scans the startup process to look for the unseal key.
/// </summary>
public class SecondAPI : IDisposable
{
    private Process _process;
    private bool    _disposed;

    private static bool _startingUP = true;

    public SecondAPI() { }


    // Starts up an instance of Vault for development and testing purposes.
    public void StartAPI(string hostname)
    {
        string apiArgs = string.Join(" ",
                                     new List<string>
                                     {
                                         "--override-Hostname " + hostname,
                                     });


        string apiBin  = "SlugEnt.DocumentServer.API.exe";
        string apiPath = @"D:\A_Dev\SlugEnt\DocumentServer\src\DocumentServer.API\bin\Debug\net8.0";

        //string apiFullPath = Path.Join(, apiBin);

        //string apiFullPath = @"..\..\..\..\..\src\DocumentServer.API\bin\Debug\net8.0\" + apiBin;


        var startInfo = new ProcessStartInfo(apiBin)
        {
            UseShellExecute        = false,
            WindowStyle            = ProcessWindowStyle.Normal,
            CreateNoWindow         = false,
            RedirectStandardError  = true,
            RedirectStandardOutput = true,
            WorkingDirectory       = apiPath,
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
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting SecondAPI: {0}", ex.ToString());
        }
    }


    public void StopVaultServer()
    {
        _process.CloseMainWindow();
        _process.Kill();
    }



    /// <summary>
    /// Ensures the Vault process is stopped.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                _process.CloseMainWindow();
                _process.Kill();
            }
            catch { }

            _process.Dispose();
        }

        _disposed = true;
    }


    public void Dispose() { Dispose(true); }



    // Following methods are used to output API messages to the Debug window.


    static void CaptureOutput(object sender,
                              DataReceivedEventArgs e)
    {
        ShowOutput(e.Data, true);
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

        // If starting up then we need to look for some stuff.
        if (SecondAPI._startingUP)
        {
            if (data?.StartsWith("API Successfully Started") == true)
            {
                SecondAPI._startingUP = false;
                return;
            }
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
}