using System;
using System.Linq;

namespace Business.Utilities
{
    /// <summary>
    /// Utility class for parsing location strings from PlantAnalysis.Location field
    /// Handles various location formats: "City, District", "District/City", "City", etc.
    /// </summary>
    public static class LocationParser
    {
        /// <summary>
        /// Parse city name from location string
        /// </summary>
        /// <param name="location">Location string from PlantAnalysis.Location</param>
        /// <returns>City name or "Unknown" if parsing fails</returns>
        public static string ParseCity(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return "Unknown";

            try
            {
                // Clean the location string
                var cleaned = location.Trim();

                // Format: "City, District" or "District, City"
                if (cleaned.Contains(","))
                {
                    var parts = cleaned.Split(',');
                    // Assume first part is city (most common format)
                    return parts[0].Trim();
                }

                // Format: "District/City" or "City/District"
                if (cleaned.Contains("/"))
                {
                    var parts = cleaned.Split('/');
                    // Assume last part is city
                    return parts[parts.Length - 1].Trim();
                }

                // Format: "District - City"
                if (cleaned.Contains("-"))
                {
                    var parts = cleaned.Split('-');
                    // Assume first part is city
                    return parts[0].Trim();
                }

                // Single value - assume it's the city
                return cleaned;
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Parse district name from location string
        /// </summary>
        /// <param name="location">Location string from PlantAnalysis.Location</param>
        /// <returns>District name or empty string if not available</returns>
        public static string ParseDistrict(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return string.Empty;

            try
            {
                // Clean the location string
                var cleaned = location.Trim();

                // Format: "City, District"
                if (cleaned.Contains(","))
                {
                    var parts = cleaned.Split(',');
                    if (parts.Length > 1)
                        return parts[1].Trim();
                }

                // Format: "District/City"
                if (cleaned.Contains("/"))
                {
                    var parts = cleaned.Split('/');
                    if (parts.Length > 1)
                        return parts[0].Trim();
                }

                // Format: "City - District"
                if (cleaned.Contains("-"))
                {
                    var parts = cleaned.Split('-');
                    if (parts.Length > 1)
                        return parts[1].Trim();
                }

                // No district information
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Parse both city and district from location string
        /// </summary>
        /// <param name="location">Location string</param>
        /// <returns>Tuple with (City, District)</returns>
        public static (string City, string District) Parse(string location)
        {
            return (ParseCity(location), ParseDistrict(location));
        }

        /// <summary>
        /// Normalize city name for grouping (handles case differences, extra spaces)
        /// </summary>
        /// <param name="cityName">Raw city name</param>
        /// <returns>Normalized city name</returns>
        public static string NormalizeCity(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                return "Unknown";

            // Trim and convert to title case for consistency
            var normalized = cityName.Trim();
            
            // Remove extra spaces
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");

            return normalized;
        }
    }
}
