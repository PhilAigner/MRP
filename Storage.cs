using System.Text.Json;

namespace MRP;

public class Storage<T>
{
    private readonly string filePath;

    public Storage(string path)
    {
        filePath = path;
    }

    public void Save(List<T> list)
    {
        string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public List<T> Load()
    {
        if (!File.Exists(filePath)) return new List<T>();
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }
}
