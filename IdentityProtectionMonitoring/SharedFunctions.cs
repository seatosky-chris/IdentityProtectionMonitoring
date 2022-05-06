using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring
{
    public static class SharedFunctions
    {
        /// <summary>
        /// Returns an environment variable from Azure
        /// </summary>
        /// <param name="name">The name or key of the environment variable to grab.</param>
        /// <returns>The value of the environment variable.</returns>
        public static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// Checks whether or not an object has a specific property
        /// </summary>
        /// <param name="objectToCheck">The object to check.</param>
        /// <param name="propertyName">The property to look for.</param>
        /// <returns>A boolean, true or false.</returns>
        public static bool HasProperty(this object objectToCheck, string propertyName)
        {
            var type = objectToCheck.GetType();
            return type.GetProperty(propertyName) != null;
        }
    }
}
