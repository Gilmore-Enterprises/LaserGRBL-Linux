#pragma warning disable SYSLIB0011 // BinaryFormatter used for existing .lps project file format
using System;
using System.Collections.Generic;
// System.Drawing removed — image ops use raw bytes (Phase 3 uses ICoreBitmap).
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LaserGRBL;

namespace Tools
{
    public static class Project
    {
        #region Private fields

        /// <summary>
        /// Formatter
        /// </summary>
        private static readonly BinaryFormatter Formatter = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };

        #endregion

        #region Public properties

        /// <summary>
        /// Holds the actual project settings
        /// </summary>
        public static List<Dictionary<string, object>> ProjectSettings = new List<Dictionary<string, object>>();

        #endregion

        #region Methods

        /// <summary>
        /// Add new settings
        /// </summary>
        /// <param name="settings">Dictionary holding the new settings</param>
        public static void AddSettings(Dictionary<string, object> settings)
        {
            if (settings == null) return;

            // Add image to dictionary
            settings.Add("ImageName", Path.GetFileName(Settings.GetObject<string>("Core.LastOpenFile", null)));
            settings.Add("ImageBase64", ConvertImageToBase64(Settings.GetObject<string>("Core.LastOpenFile", null)));

            ProjectSettings.Add(settings);
        }

        /// <summary>
        /// Clear all project settings (has to be done if file new file is opened or last file is reloaded)
        /// </summary>
        public static void ClearSettings()
        {
            ProjectSettings.Clear();
        }

        /// <summary>
        /// Store project to file
        /// </summary>
        /// <param name="filename">Filepath where project should be stored</param>
        public static void StoreSettings(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return;

            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Formatter.Serialize(fs, ProjectSettings);
                fs.Close();
            }
        }

        /// <summary>
        /// Loads a project from a file
        /// </summary>
        /// <param name="filename">Filepath to the project</param>
        /// <returns>Settings of the project</returns>
        public static List<Dictionary<string, object>> LoadProject(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return new List<Dictionary<string, object>>();
            if (!File.Exists(filename)) return new List<Dictionary<string, object>>();

            List<Dictionary<string, object>> project;

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                project = (List<Dictionary<string, object>>)Formatter.Deserialize(fs);
                fs.Close();
            }

            return project;
        }

        /// <summary>
        /// Converts an image to base64 string
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns></returns>
        private static string ConvertImageToBase64(string imagePath)
        {
            // Phase 3: re-implemented with SkiaSharp ICoreBitmap.
            return "";
        }

        /// <summary>
        /// Save base64 image to file
        /// </summary>
        /// <param name="base64Image">Base64 image string</param>
        /// <param name="imagePath">Path where to store the image</param>
        public static void SaveImage(string base64Image, string imagePath)
        {
            // Phase 3: save raw PNG bytes to file (re-implemented with SkiaSharp).
            System.IO.File.WriteAllBytes(imagePath, Convert.FromBase64String(base64Image));
        }

        #endregion
    }
}