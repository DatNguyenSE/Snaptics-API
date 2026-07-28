using System.IO;
using System.Text.Json;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using Microsoft.AspNetCore.Hosting;

namespace BLL.Service
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly string _configFilePath;
        private MaintenanceConfigDto _currentConfig;
        private readonly object _lock = new object();

        public MaintenanceService(IWebHostEnvironment env)
        {
            _configFilePath = Path.Combine(env.ContentRootPath, "maintenance.json");
            _currentConfig = LoadConfig();
        }

        public MaintenanceConfigDto GetConfig()
        {
            lock (_lock)
            {
                return _currentConfig;
            }
        }

        public void UpdateConfig(MaintenanceConfigDto config)
        {
            lock (_lock)
            {
                _currentConfig = config;
                SaveConfig(config);
            }
        }

        public bool IsMaintenanceMode()
        {
            lock (_lock)
            {
                return _currentConfig.IsMaintenance;
            }
        }

        private MaintenanceConfigDto LoadConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                var defaultConfig = new MaintenanceConfigDto { IsMaintenance = false };
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<MaintenanceConfigDto>(json) ?? new MaintenanceConfigDto();
            }
            catch
            {
                return new MaintenanceConfigDto { IsMaintenance = false };
            }
        }

        private void SaveConfig(MaintenanceConfigDto config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);
        }
    }
}
