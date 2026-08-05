using Newtonsoft.Json;
using SigmabotSync.Domain.Config;
using System;
using System.IO;

namespace SigmabotSync.Infrastructure.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;

        /// <summary>Ruta absoluta del archivo JSON de configuración.</summary>
        public string SettingsFilePath => _settingsPath;

        /// <param name="settingsFilePath">Si es null, usa <c>settings.json</c> en el directorio base de la app (consola).</param>
        public SettingsService(string settingsFilePath = null)
        {
            _settingsPath = string.IsNullOrWhiteSpace(settingsFilePath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json")
                : settingsFilePath;
        }

        public AconexSettings Load()
        {
            if (!File.Exists(_settingsPath))
                return new AconexSettings(); // vacío por defecto

            var json = File.ReadAllText(_settingsPath);
            return JsonConvert.DeserializeObject<AconexSettings>(json);
        }

        public void Save(AconexSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
        }
    }
}
