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
        _ = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
    }


    public static void ResumeSleepHabits()
    {
        _ = SetThreadExecutionState(ES_CONTINUOUS);
    }
}