using System.Text.Json;

namespace CorporateKnowledgeBase.Web.Helpers
{
    /// <summary>
    /// Provides extension methods for the ISession interface to support storing and retrieving complex objects.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Serializes a complex object to a JSON string and stores it in the session.
        /// </summary>
        /// <typeparam name="T">The type of the object to store.</typeparam>
        /// <param name="session">The session instance.</param>
        /// <param name="key">The key to store the value against.</param>
        /// <param name="value">The object to store.</param>
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        /// <summary>
        /// Retrieves a JSON string from the session and deserializes it back to a complex object.
        /// </summary>
        /// <typeparam name="T">The type of the object to retrieve.</typeparam>
        /// <param name="session">The session instance.</param>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The deserialized object, or the default value for the type if the key is not found.</returns>
        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}