using System;
using System.Linq;
using System.Reflection;

namespace TreeLiner
{
    /// <summary>
    /// Key factory by attribute <see cref="KeyAttribute"/>
    /// </summary>
    /// <remarks>
    /// By <see cref="KeyAttribute"/> if it exist, or property name
    /// </remarks>
    public class KeyAttributeFactory : IItemKeyFactory
    {
        /// <summary>
        /// Customizing key for using with <see cref="KeyAttributeFactory"/>
        /// </summary>
        public class KeyAttribute : Attribute
        {
            /// <summary>
            /// Customizing key for using with <see cref="KeyAttributeFactory"/>
            /// </summary>
            /// <param name="key">custom key</param>
            public KeyAttribute(string key)
            {
                Key = key;
            }

            /// <summary>
            /// Custom key
            /// </summary>
            public string Key { get; private set; }
        }

        /// <inheritdoc cref="IItemKeyFactory"/>
        public string GetKey(PropertyInfo prop)
        {
            var atr = prop.GetCustomAttributes(typeof(KeyAttribute), false).OfType<KeyAttribute>();
            if (atr.Any())
                return atr.First().Key;
            return prop.Name;
        }
    }
}
