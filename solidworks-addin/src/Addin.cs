using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Main Add-In class implementing ISwAddin interface.
    /// This is the entry point for the SolidWorks Add-In.
    /// 
    /// CRITICAL: The GUID below must match:
    /// - AssemblyInfo.cs [assembly: Guid(...)]
    /// - register_addin.bat and unregister_addin.bat scripts
    /// </summary>
    [ComVisible(true)]
    [Guid("D8A3F12B-ABCD-4A87-8123-9876ABCDEF01")]
    [ProgId("TextToCad.SolidWorksAddin")]
    public class Addin : ISwAddin
    {
        #region Private Fields

        /// <summary>
        /// Reference to the SolidWorks application
        /// </summary>
        private ISldWorks swApp;

        /// <summary>
        /// Add-in cookie (unique identifier assigned by SolidWorks)
        /// </summary>
        private int addinID;

        /// <summary>
        /// Task pane host managing the UI
        /// </summary>
        private TaskPaneHost taskPaneHost;

        #endregion

        #region ISwAddin Implementation

        /// <summary>
        /// Called when SolidWorks loads the add-in.
        /// This is where we initialize our add-in and create the Task Pane.
        /// </summary>
        /// <param name="ThisSW">SolidWorks application object</param>
        /// <param name="Cookie">Add-in ID assigned by SolidWorks</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            try
            {
                Logger.Info("========================================");
                Logger.Info("Text-to-CAD Add-In Loading...");
                Logger.Info("========================================");

                // Store references
                swApp = (ISldWorks)ThisSW;
                addinID = Cookie;

                Logger.Info($"Connected to SolidWorks (Cookie: {Cookie})");
                Logger.Info($"SolidWorks Version: {swApp.RevisionNumber()}");

                // Test API connection
                TestApiConnectionAsync();

                // Create and show Task Pane
                taskPaneHost = new TaskPaneHost(swApp, addinID);
                Logger.Info("Task Pane created successfully");

                Logger.Info("Text-to-CAD Add-In loaded successfully!");
                Logger.Info("========================================");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to connect to SolidWorks", ex);
                
                // Show error to user
                string errorMsg = $"Text-to-CAD Add-In failed to load:\n\n{ex.Message}\n\n" +
                                 $"Check log file at:\n{Logger.GetLogFilePath()}";
                System.Windows.Forms.MessageBox.Show(
                    errorMsg,
                    "Text-to-CAD Load Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );

                return false;
            }
        }

        /// <summary>
        /// Called when SolidWorks unloads the add-in.
        /// Clean up resources here.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool DisconnectFromSW()
        {
            try
            {
                Logger.Info("========================================");
                Logger.Info("Text-to-CAD Add-In Unloading...");
                Logger.Info("========================================");

                // Dispose task pane
                if (taskPaneHost != null)
                {
                    taskPaneHost.Dispose();
                    taskPaneHost = null;
                    Logger.Info("Task Pane disposed");
                }

                // Release SolidWorks reference
                swApp = null;

                Logger.Info("Text-to-CAD Add-In unloaded successfully");
                Logger.Info("========================================");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during add-in disconnect", ex);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Test connection to the FastAPI backend asynchronously
        /// </summary>
        private async void TestApiConnectionAsync()
        {
            try
            {
                bool connected = await ApiClient.TestConnectionAsync();
                if (connected)
                {
                    Logger.Info($"✓ Backend API is reachable at {ApiClient.GetBaseUrl()}");
                }
                else
                {
                    Logger.Warning($"✗ Backend API is not reachable at {ApiClient.GetBaseUrl()}");
                    Logger.Warning("Please ensure the FastAPI server is running:");
                    Logger.Warning("  cd backend");
                    Logger.Warning("  .venv\\Scripts\\Activate.ps1");
                    Logger.Warning("  uvicorn main:app --reload");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not test API connection: {ex.Message}");
            }
        }

        #endregion

        #region COM Registration

        /// <summary>
        /// COM registration function.
        /// Called by RegAsm.exe when registering the add-in.
        /// </summary>
        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            try
            {
                // Get add-in registry key
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
                Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

                string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
                
                // Create keys in both HKLM and HKCU
                Microsoft.Win32.RegistryKey addinkey = hklm.CreateSubKey(keyname);
                addinkey.SetValue(null, 0);
                addinkey.SetValue("Description", "Text-to-CAD: Natural Language to CAD Add-In");
                addinkey.SetValue("Title", "Text-to-CAD");

                addinkey = hkcu.CreateSubKey(keyname);
                addinkey.SetValue(null, 0);
                addinkey.SetValue("Description", "Text-to-CAD: Natural Language to CAD Add-In");
                addinkey.SetValue("Title", "Text-to-CAD");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"COM Registration failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// COM unregistration function.
        /// Called by RegAsm.exe when unregistering the add-in.
        /// </summary>
        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
                Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

                string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";

                hklm.DeleteSubKey(keyname, false);
                hkcu.DeleteSubKey(keyname, false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"COM Unregistration failed: {ex.Message}");
            }
        }

        #endregion
    }
}
