using System.Runtime.InteropServices;
internal partial class Woke
{
    // Prevent sleep during download
    [LibraryImport("kernel32.dll")]
    internal static partial uint SetThreadExecutionState(uint esFlags);
    const uint ES_CONTINUOUS = 0x80000000;
    const uint ES_SYSTEM_REQUIRED = 0x00000001;
    const uint ES_AWAYMODE_REQUIRED = 0x00000040;

    public Woke(){}

    public static void PreventSleep()
    {
        // Prevent sleep
        var state = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);// | ES_AWAYMODE_REQUIRED);
        if (state == 0) System.Console.WriteLine("Unable to override power management settings.");

        /*  Suggested by AI as possible solution to System.Net.Sockets.SocketException
        // Also disable network adapter power management programmatically
        try 
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/change standby-timeout-ac 0",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
        */
    }


    public static void ResumeSleepHabits()
    {
        var state = SetThreadExecutionState(ES_CONTINUOUS);
        if (state == 0) System.Console.WriteLine("Unable to restore power management settings.");
    }
}