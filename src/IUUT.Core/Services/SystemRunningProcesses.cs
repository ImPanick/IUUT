using System.Diagnostics;

namespace IUUT.Core.Services;

/// <summary>
/// Default <see cref="IRunningProcesses"/> backed by <see cref="Process.GetProcesses()"/>.
/// Reads names defensively — a process that exits mid-enumeration is skipped, never thrown.
/// </summary>
public sealed class SystemRunningProcesses : IRunningProcesses
{
    /// <inheritdoc />
    public IReadOnlyList<string> GetProcessNames()
    {
        var names = new List<string>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                names.Add(process.ProcessName);
            }
            catch (InvalidOperationException)
            {
                // The process exited between snapshot and read — ignore it.
            }
            finally
            {
                process.Dispose();
            }
        }

        return names;
    }
}
