using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace PalaceBuddy;

public class LocationLoader
{
    private string _trapsFilename;

    public LocationLoader()
    {
        _trapsFilename = Path.Combine(DalamudService.PluginInterface.AssemblyLocation.Directory?.FullName!, "Resources", "traps.csv.gz");
    }

    public Task<string> CheckDB()
    {
        var tcs = new TaskCompletionSource<string>();

        ThreadPool.QueueUserWorkItem(_ => {
            try
            {
                using var fileStream = File.Open(_trapsFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzStream);
                int n = 0;
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var data = line.Split(',');
                    if (data.Length < 4) continue;
                    int.Parse(data[0]);
                    float.Parse(data[1]);
                    float.Parse(data[2]);
                    float.Parse(data[3]);
                    ++n;
                }
                tcs.SetResult($"OK ({n} records)");
            }
            catch (Exception e)
            {
                DalamudService.Log.Error(e, "CheckDB");
                tcs.SetResult(e.Message);
            }
        });

        return tcs.Task;
    }

    public Task<List<Vector3>> GetLocationsForTerritory(int territoryType)
    {
        var tcs = new TaskCompletionSource<List<Vector3>>();

        ThreadPool.QueueUserWorkItem(_ => {
            try
            {
                var result = new List<Vector3>();
                using var fileStream = File.Open(_trapsFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzStream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var data = line.Split(',');
                    if (data.Length < 4) continue;
                    if (int.Parse(data[0]) == territoryType)
                    {
                        result.Add(new(
                            float.Parse(data[1]),
                            float.Parse(data[2]),
                            float.Parse(data[3])
                        ));
                    }
                }
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }
}
