using AwsDoc4.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AwsDoc4.Models
{
    public class AppConfig
    {
        public static string Path => System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".conf");

        [JsonPropertyName("theme")]
        [JsonInclude]
        public ThemeConfig? Theme { get; private set; }

        public void SetTheme(ThemeConfig conf)
        {
            this.Theme = conf;
        }

        public void Save()
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            };
            options.MakeReadOnly();
            System.IO.File.WriteAllText(Path, JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true }));
        }

        public static AppConfig Load()
        {
            if (!System.IO.File.Exists(Path))
            {
                return new AppConfig();
            }
            var json = System.IO.File.ReadAllText(Path);
            return JsonSerializer.Deserialize<AppConfig>(json)!;
        }
    }
}
