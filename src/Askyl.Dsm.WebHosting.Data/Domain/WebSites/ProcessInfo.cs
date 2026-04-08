using System.Diagnostics;

namespace Askyl.Dsm.WebHosting.Data.Domain.WebSites;

public record ProcessInfo(Process Process)
{
    public int Id => Process.Id;

    public bool IsResponding => !Process.HasExited && Process.Responding;
}
