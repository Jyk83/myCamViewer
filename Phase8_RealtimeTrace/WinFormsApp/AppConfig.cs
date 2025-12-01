using System;
using System.IO;

namespace CamViewerPOC
{
    /// <summary>
    /// Application configuration management
    /// </summary>
    public class AppConfig
    {
        private static AppConfig instance = null;
        private static readonly object lockObj = new object();
        private const string ConfigFileName = "CamViewerConfig.json";
        
        public static AppConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObj)
                    {
                        if (instance == null)
                        {
                            instance = new AppConfig();
                            instance.Load();
                        }
                    }
                }
                return instance;
            }
        }
        
        // Configuration properties
        public string LastFolderPath { get; set; }
        public string RenderSettingsPath { get; set; }
        
        private AppConfig()
        {
            // Default values
            LastFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            RenderSettingsPath = Path.Combine(GetConfigDirectory(), "RenderSettings.json");
        }
        
        /// <summary>
        /// Get configuration file directory
        /// </summary>
        private string GetConfigDirectory()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CamViewerPOC");
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            return appDataPath;
        }
        
        /// <summary>
        /// Get full config file path
        /// </summary>
        private string GetConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), ConfigFileName);
        }
        
        /// <summary>
        /// Load configuration from file
        /// </summary>
        public void Load()
        {
            try
            {
                string filePath = GetConfigFilePath();
                System.Diagnostics.Debug.WriteLine("[AppConfig] Loading from: " + filePath);
                
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("[AppConfig] File not found, creating default");
                    // Create default config file
                    Save();
                    return;
                }
                
                string json = File.ReadAllText(filePath);
                System.Diagnostics.Debug.WriteLine("[AppConfig] Loaded JSON: " + json);
                
                // Simple JSON parsing (avoiding external dependencies)
                if (json.Contains("\"LastFolderPath\""))
                {
                    // Find "LastFolderPath": "value"
                    int keyIndex = json.IndexOf("\"LastFolderPath\"");
                    int colonIndex = json.IndexOf(":", keyIndex);
                    int startIdx = json.IndexOf("\"", colonIndex) + 1; // First quote after colon
                    int endIdx = json.IndexOf("\"", startIdx); // Closing quote
                    
                    if (startIdx > colonIndex && endIdx > startIdx)
                    {
                        string rawPath = json.Substring(startIdx, endIdx - startIdx);
                        // Unescape backslashes (JSON uses \\ for single \)
                        LastFolderPath = rawPath.Replace("\\\\", "\\");
                        System.Diagnostics.Debug.WriteLine("[AppConfig] Parsed LastFolderPath: " + LastFolderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AppConfig] Failed to load: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("[AppConfig] Stack trace: " + ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void Save()
        {
            try
            {
                string filePath = GetConfigFilePath();
                System.Diagnostics.Debug.WriteLine("[AppConfig] Saving to: " + filePath);
                
                string json = string.Format("{{\n  \"LastFolderPath\": \"{0}\",\n  \"RenderSettingsPath\": \"{1}\"\n}}",
                    LastFolderPath.Replace("\\", "\\\\"),
                    RenderSettingsPath.Replace("\\", "\\\\"));
                
                System.Diagnostics.Debug.WriteLine("[AppConfig] JSON content: " + json);
                File.WriteAllText(filePath, json);
                System.Diagnostics.Debug.WriteLine("[AppConfig] Saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AppConfig] Failed to save: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("[AppConfig] Stack trace: " + ex.StackTrace);
            }
        }
    }
}
