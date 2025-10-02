using System.Diagnostics;

namespace Askyl.Dsm.WebHosting.Data.WebSites;

public class ProcessInfo(Process process, WebSiteConfiguration site)
{
    public int ProcessId { get; set; } = process.Id;

    public string ProcessName { get; set; } = process.ProcessName;

    public DateTime StartTime { get; set; } = process.StartTime;

    public TimeSpan CpuTime { get; set; }

    public long WorkingSetMemory { get; set; }

    public bool IsResponding { get; set; } = true;

    public WebSiteConfiguration Site { get; set; } = site;
}