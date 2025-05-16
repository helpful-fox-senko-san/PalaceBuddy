using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PalaceBuddy;

public class LocationLoader
{
    private string _dbFileName;

    public LocationLoader()
    {
        var file1 = System.IO.Path.Combine(DalamudService.PluginInterface.ConfigDirectory.FullName, "..", "PalacePal", "palace-pal.data.sqlite3");
        var file2 = System.IO.Path.Combine(DalamudService.PluginInterface.ConfigDirectory.FullName, "..", "Palace Pal", "palace-pal.data.sqlite3");
        _dbFileName = (System.IO.Path.Exists(file2) && !System.IO.Path.Exists(file1)) ? file2 : file1;
    }

    public Task<string> CheckDB()
    {
        var tcs = new TaskCompletionSource<string>();

        ThreadPool.QueueUserWorkItem(_ => {
            try
            {
                using var conn = new SqliteConnection($"Data Source={_dbFileName};Mode=ReadOnly");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT COUNT(1) FROM Locations", conn);
                var query = cmd.ExecuteReader();
                if (query.Read())
                    tcs.SetResult($"OK ({query.GetInt64(0)} records)");
                else
                    tcs.SetResult("No result?");
            }
            catch (Exception e)
            {
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
                using var conn = new SqliteConnection($"Data Source={_dbFileName};Mode=ReadOnly");
                conn.Open();
                using var cmd = new SqliteCommand("SELECT DISTINCT X, Y, Z FROM Locations WHERE TerritoryType = @TerritoryType", conn);
                cmd.Parameters.AddWithValue("@TerritoryType", (long)territoryType);
                var query = cmd.ExecuteReader();
                while (query.Read())
                    result.Add(new Vector3(query.GetFloat(0), query.GetFloat(1), query.GetFloat(2)));
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
