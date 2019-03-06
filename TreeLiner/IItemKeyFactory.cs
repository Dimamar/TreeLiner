using System.Reflection;

namespace TreeLiner
{
    /// <summary>
    /// Key factory
    /// </summary>
    public interface IItemKeyFactory
    {
        /// <summary>
        /// Get locally unique key by property info
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        string GetKey(PropertyInfo prop);
    }
}
