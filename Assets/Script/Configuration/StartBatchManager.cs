using UnityEngine;
using System.Diagnostics;
using System.Threading;

using System.IO;
using Lavender.Systems;

public class StartBatchManager : MonoBehaviour
{
    uint pid = 0;
    private void Start() 
    {
        ConfigManager.EnsureInitialization();
        if (ConfigManager.config.batFileLocation != "")
            pid = StartExternalProcess.Start(ConfigManager.config.batFileLocation);
        UnityEngine.Debug.Log("Batch file with PID: " + pid);
        //Process.Start(Path.GetFullPath(ConfigManager.config.batFileLocation));

        var daemonProcess = Process.GetProcessesByName("amdaemon");
        foreach(var daemon in daemonProcess)
        {
            daemon.PriorityClass = ProcessPriorityClass.High;
        }

        Invoke("SetMercuryWindow", 2);

    }
    private void OnDestroy() 
    {
        if (pid != 0)
        {
            StartExternalProcess.KillProcess(pid);
            UnityEngine.Debug.Log("Batch file with PID: " + pid + " killed");
        }
    }

    void SetMercuryWindow()
    {
        ForceWindowed.SetClientSize(Process.GetProcessesByName("Mercury-Win64-Shipping")[0].ProcessName, 768, 1366);
    }
}
