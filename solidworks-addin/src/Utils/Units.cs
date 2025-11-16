namespace TextToCad.SolidWorksAddin.Utils
{
    /// <summary>
    /// Unit conversion utilities for CAD dimensions.
    /// SolidWorks API typically expects dimensions in meters, but users think in millimeters.
    /// </summary>
    /// <remarks>
    /// CRITICAL: SolidWorks API dimension parameters are in METERS by default.
    /// 
    /// When calling SolidWorks API methods like:
    /// - IFeatureManager.FeatureExtrusion2() - depth parameter is in METERS
    /// - ISketchManager.CreateCenterLine() - coordinates are in METERS
    /// - IModelDocExtension.SelectByID2() - coordinates are in METERS
    /// 
    /// Always convert user input (typically in mm) to meters before passing to API.
    /// 
    /// Example:
    ///   User wants 50mm extrusion depth
    ///   API call: FeatureExtrusion2(..., Units.MmToM(50), ...)
    /// </remarks>
    public static class Units
    {
        /// <summary>
        /// Convert millimeters to meters.
        /// Use this when passing dimensions to SolidWorks API methods.
        /// </summary>
        /// <param name="mm">Dimension in millimeters</param>
        /// <returns>Dimension in meters</returns>
        /// <example>
        /// double depthInMeters = Units.MmToM(50);  // 0.05 m
        /// feature.FeatureExtrusion2(..., depthInMeters, ...);
        /// </example>
        public static double MmToM(double mm)
        {
            return mm / 1000.0;
        }

        /// <summary>
        /// Convert meters to millimeters.
        /// Use this when reading dimensions from SolidWorks API for display.
        /// </summary>
        /// <param name="m">Dimension in meters</param>
        /// <returns>Dimension in millimeters</returns>
        /// <example>
        /// double depthFromAPI = feature.GetDepth();  // Returns meters
        /// double depthInMm = Units.MToMm(depthFromAPI);
        /// Console.WriteLine($"Depth: {depthInMm} mm");
        /// </example>
        public static double MToMm(double m)
        {
            return m * 1000.0;
        }

        /// <summary>
        /// Common metric unit for reference.
        /// 1 millimeter in meters.
        /// </summary>
        public const double OneMm = 0.001;

        /// <summary>
        /// Common metric unit for reference.
        /// 1 centimeter in meters.
        /// </summary>
        public const double OneCm = 0.01;

        /// <summary>
        /// Common metric unit for reference.
        /// 1 meter in millimeters.
        /// </summary>
        public const double OneM = 1000.0;
    }
}
