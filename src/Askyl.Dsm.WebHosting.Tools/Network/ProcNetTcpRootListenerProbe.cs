using System.Net;
using Askyl.Dsm.WebHosting.Constants.DSM.System;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// Reads /proc/net/tcp, which any user can read. IPv4 only: DSM is reached on 127.0.0.1, and an IPv6 address
/// never matches an IPv4 row. Anything it cannot read or parse counts as not root-owned, so the answer fails
/// closed (and is always false off Linux).
/// </summary>
public sealed class ProcNetTcpRootListenerProbe(IFileReader fileReader) : IRootListenerProbe
{
    // Columns of a row: sl, local_address, rem_address, st, queues, timer, retransmits, uid
    const int LocalAddressColumn = 1;

    const int StateColumn = 3;

    const int UidColumn = 7;

    const string RootUid = "0";

    public bool IsRootListening(IPAddress loopbackAddress, int port)
    {
        try
        {
            if (!fileReader.FileExists(SystemDefaults.ProcNetTcpPath))
            {
                return false;
            }

            var bytes = loopbackAddress.GetAddressBytes();
            var wildcard = new string('0', bytes.Length * 2);
            var exact = Convert.ToHexString([.. bytes.Chunk(4).SelectMany(word => word.Reverse())]);
            var portHex = port.ToString("X4");

            return fileReader.ReadAllLines(SystemDefaults.ProcNetTcpPath).Any(row => row.Split(' ', StringSplitOptions.RemoveEmptyEntries) is { Length: > UidColumn } columns
                && columns[StateColumn] == SystemDefaults.ProcNetListenState
                && columns[UidColumn] == RootUid
                && columns[LocalAddressColumn].Split(':') is [var address, var listenPort]
                && listenPort == portHex
                && (address == wildcard || address == exact));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
