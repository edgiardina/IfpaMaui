using System.Text.Json;

namespace Ifpa.Extensions
{
    public static class DebugExtensions
    {

        /// <summary>
        /// Dumps the object as a json string
        /// Can be used for logging object contents.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="obj">The object to dump. Can be null</param>
        /// <param name="indent">To indent the result or not</param>
        /// <returns>A string representing the object content</returns>
        public static string Dump<T>(this T obj, bool indent = false)
        {
            if (obj is Exception ex)
            {
                return DumpException(ex, indent);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = indent,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(obj, options);
        }

        /// <summary>
        /// Dumps the exception object to JSON, including relevant fields like message, stack trace, and inner exceptions.
        /// </summary>
        /// <param name="ex">The exception to dump.</param>
        /// <param name="indent">To indent the result or not</param>
        /// <returns>A string representing the exception content</returns>
        private static string DumpException(Exception ex, bool indent = false)
        {
            var exceptionDetails = new
            {
                ex.Message,
                ex.StackTrace,
                ex.Source,
                ex.HResult,
                ex.HelpLink,
                InnerException = ex.InnerException != null ? DumpException(ex.InnerException, indent) : null
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = indent,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(exceptionDetails, options);
        }
    }
}
