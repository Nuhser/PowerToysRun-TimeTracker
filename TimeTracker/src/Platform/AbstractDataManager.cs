using System;
using System.IO;
using System.Text.Json;
using Wox.Plugin.Logger;

namespace Community.Powertoys.Run.Plugin.TimeTracker.Platform;

public abstract class AbstractDataManager<DataHolder> where DataHolder : AbstractDataHolder, new()
{
    public static readonly string DATA_BASE_DIRECTORY_PATH =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
        @"\Microsoft\PowerToys\PowerToys Run\Settings\Plugins\Community.PowerToys.Run.Plugin.TimeTracker\";
    protected readonly JsonSerializerOptions JSON_SERIALIZER_OPTIONS = new() { WriteIndented = true };

    private DataHolder? data;
    public DataHolder? Data
    {
        get { return data; }
        set { data = value; }
    }
    public bool JsonBroken { get; set; }

    public abstract string GetDataFilePath();

    public AbstractDataManager()
    {
        FromJson(out data);
    }

    public void Refresh() {
        FromJson(out data);
    }

    protected void FromJson(out DataHolder? storage)
    {
        if (!File.Exists(GetDataFilePath()))
        {
            storage = new DataHolder();
            ToJson();
            
            JsonBroken = false;

            return;
        }

        try
        {
            string jsonString = File.ReadAllText(GetDataFilePath());
            storage = JsonSerializer.Deserialize<DataHolder>(jsonString);

            if (storage == null)
            {
                Log.Error(GetDataFilePath() + " was read but didn't contain any content.", GetType());
                JsonBroken = true;
                return;
            }

            JsonBroken = false;
            return;
        }
        catch (JsonException)
        {
            Log.Error(GetDataFilePath() + " couldn't be read or didn't contain a valid JSON.", GetType());

            storage = null;
            JsonBroken = true;

            return;
        }
    }

    public void Save() {
        ToJson();
    }

    protected void ToJson()
    {
        string jsonString = JsonSerializer.Serialize(data, JSON_SERIALIZER_OPTIONS);
        File.WriteAllText(GetDataFilePath(), jsonString);
    }
}