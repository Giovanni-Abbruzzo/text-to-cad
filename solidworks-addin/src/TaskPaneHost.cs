using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Manages the SolidWorks Task Pane and hosts the WinForms UserControl.
    /// The Task Pane appears as a dockable panel in the SolidWorks UI.
    /// </summary>
    public class TaskPaneHost : IDisposable
    {
        #region Private Fields

        private ISldWorks swApp;
        private ITaskpaneView taskPaneView;
        private TaskPaneControl taskPaneControl;
        private int addinID;
        private bool disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Create and display the Task Pane
        /// </summary>
        /// <param name="solidWorksApp">SolidWorks application reference</param>
        /// <param name="cookieID">Add-in ID</param>
        /// <param name="addin">Add-in instance for API access</param>
        public TaskPaneHost(ISldWorks solidWorksApp, int cookieID, Addin addin = null)
        {
            swApp = solidWorksApp ?? throw new ArgumentNullException(nameof(solidWorksApp));
            addinID = cookieID;

            CreateTaskPane(addin);
        }

        #endregion

        #region Task Pane Creation

        /// <summary>
        /// Create the Task Pane and add the WinForms control
        /// </summary>
        private void CreateTaskPane(Addin addin)
        {
            try
            {
                Logger.Info("Creating Task Pane...");

                // Create the WinForms control that will be hosted in the Task Pane
                taskPaneControl = new TaskPaneControl();
                
                // Set the add-in reference for Sprint SW-2 test utilities
                if (addin != null)
                {
                    taskPaneControl.SetAddin(addin);
                    Logger.Info("Add-in reference set for TaskPaneControl");
                }

                // Get the icon path (using a default icon for now)
                // TODO: Add custom icon file and update path
                string iconPath = "";  // Empty string uses default SolidWorks icon

                // Create the Task Pane view
                // Parameters:
                // 1. Icon path (empty for default)
                // 2. Title shown in Task Pane header
                taskPaneView = swApp.CreateTaskpaneView2(iconPath, "Text-to-CAD");

                if (taskPaneView == null)
                {
                    throw new Exception("Failed to create Task Pane view");
                }

                // Add the WinForms control to the Task Pane
                // The control must be a UserControl derived class
                // Note: AddControl for SolidWorks requires specific parameters
                // Try using the full assembly path
                string assemblyPath = taskPaneControl.GetType().Assembly.Location;
                string controlTypeName = taskPaneControl.GetType().FullName;
                
                Logger.Info($"Adding control: Assembly={assemblyPath}, Type={controlTypeName}");
                
                object result = taskPaneView.AddControl(assemblyPath, controlTypeName);

                if (result == null)
                {
                    Logger.Error("AddControl returned null - trying alternative approach");
                    
                    // Alternative: Create the control instance and get its handle
                    taskPaneView.DisplayWindowFromHandle(taskPaneControl.Handle.ToInt32());
                    Logger.Info("Using DisplayWindowFromHandle approach");
                }
                else
                {
                    Logger.Info("Control added successfully via AddControl");
                    // Make the Task Pane visible
                    taskPaneView.DisplayWindowFromHandle(taskPaneControl.Handle.ToInt32());
                }

                Logger.Info("Task Pane created and displayed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create Task Pane", ex);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the Task Pane
        /// </summary>
        public void Show()
        {
            if (taskPaneView != null)
            {
                taskPaneView.DisplayWindowFromHandle(taskPaneControl.Handle.ToInt32());
                Logger.Debug("Task Pane shown");
            }
        }

        /// <summary>
        /// Hide the Task Pane
        /// </summary>
        public void Hide()
        {
            if (taskPaneView != null)
            {
                // Note: SolidWorks Task Panes don't have a direct Hide method
                // Users can close it manually via the X button
                Logger.Debug("Task Pane hide requested (user must close manually)");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Logger.Info("Disposing Task Pane...");

                        // Dispose the WinForms control
                        if (taskPaneControl != null)
                        {
                            taskPaneControl.Dispose();
                            taskPaneControl = null;
                        }

                        // Delete the Task Pane view
                        if (taskPaneView != null)
                        {
                            taskPaneView.DeleteView();
                            taskPaneView = null;
                        }

                        Logger.Info("Task Pane disposed successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error disposing Task Pane", ex);
                    }
                }

                disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~TaskPaneHost()
        {
            Dispose(false);
        }

        #endregion
    }
}
