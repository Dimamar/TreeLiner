using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace TreeLiner
{
    /// <summary>
    /// Parser
    /// </summary>
    public static class TreeParser
    {
        /// <summary>
        /// Convert to tree
        /// </summary>
        /// <param name="item">source</param>
        /// <param name="root">root Node</param>
        /// <param name="descriptor"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static Node Convert(object item, Node root, IItemKeyFactory descriptor, long parentId)
        {
            Node node = root;

            var itemType = item.GetType();

            if (IsSimple(itemType))
            {
                var sVal = itemType.IsEnum ? (int)item : item;
                node.Val = sVal;
                return node;
            }
            else
            {
                root.RefValue = new WeakReference(item);
            }

            var properties = itemType.GetProperties().Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
            {
                var propValue = property.GetValue(item, null);
                if (IsSimple(property.PropertyType))
                { // Add simple type element
                    node.Childs.Add(SimpleToNode(descriptor, property, propValue, node, property.PropertyType, parentId));
                    continue;
                }

                if (IsList(property.PropertyType))
                { // Add collection
                    IList propItems = propValue as IList;
                    var typeItem = property.PropertyType.GetGenericArguments()[0];
                    if (IsSimple(typeItem))
                    {
                        foreach (var subItem in propItems)
                        {// Add simple type element collection
                            node.Childs.Add(SimpleToNode(descriptor, property, subItem, node, typeItem, parentId));
                        }
                    }
                    else
                    {
                        foreach (var subItem in propItems)
                        { // Add constructed type element collection
                            var itemNode = new Node() { Name = descriptor.GetKey(property), Parrent = node, RepairId = parentId };
                            Convert(subItem, itemNode, descriptor, parentId);
                            node.Childs.Add(itemNode);
                        }
                    }
                    continue;
                }

                // Add constructed type element
                if (propValue == null)
                    continue;
                var newNode = new Node() { Name = descriptor.GetKey(property), Parrent = node, RepairId = parentId };
                Convert(propValue, newNode, descriptor, parentId);
                node.Childs.Add(newNode);
            }

            return node;
        }

        /// <summary>
        /// Update changed tree
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tree"></param>
        /// <param name="descriptor"></param>
        /// <param name="parentId"></param>
        /// <param name="newNodes"></param>
        /// <param name="lessNodes"></param>
        /// <param name="changedNodes"></param>
        /// <returns></returns>
        public static bool UpdateTree(object data, Node tree, KeyAttributeFactory descriptor, int parentId,
            List<Node> newNodes, List<Node> lessNodes, List<Node> changedNodes)
        {
            bool result = true;

            Node node = tree;
            var itemType = data.GetType();

            if (IsSimple(itemType))
            {
                if (CompareSimple(node, data))
                    return result;
                lessNodes.Remove(node);
                result = false;
                changedNodes.Add(node);
                return result;
            }

            lessNodes.Remove(node);

            var properties = itemType.GetProperties().Where(p => p.CanRead && p.CanWrite);
            foreach (var property in properties)
            {
                var propValue = property.GetValue(data, null);
                var newPropName = descriptor.GetKey(property);
                var oldPropNode = node.Childs.FirstOrDefault(el => el.Name == newPropName);
                if (IsSimple(property.PropertyType))
                { // simple type analyzing
                    if (oldPropNode == null)
                    {
                        result = false;
                        var newSimplePropNode = SimpleToNode(descriptor, property, propValue, node, property.PropertyType,
                            parentId);
                        newNodes.Add(newSimplePropNode);
                        node.Childs.Add(newSimplePropNode);
                    }
                    else if (CompareSimple(oldPropNode, propValue))
                    {
                        lessNodes.Remove(oldPropNode);
                    }
                    else
                    {
                        result = false;
                        var sVal = property.PropertyType.IsEnum ? (int)propValue : propValue;
                        oldPropNode.Val = sVal;
                        lessNodes.Remove(oldPropNode);
                        changedNodes.Add(oldPropNode);
                    }
                    continue;
                }

                if (IsList(property.PropertyType))
                { // collection analyzing
                    IList propItems = propValue as IList;
                    var typeItem = property.PropertyType.GetGenericArguments()[0];
                    var listItemsName = descriptor.GetKey(property);
                    var oldList = node.Childs.Where(el => el.Name == listItemsName).ToList();
                    if (oldList.Count != propItems.Count)
                        result = false;
                    if (IsSimple(typeItem))
                    {
                        foreach (var subItem in propItems)
                        {// simple type collection element analyzing
                            var oldListEl = oldList.FirstOrDefault(el => el.Val == subItem);
                            if (oldListEl != null)
                                continue;
                            result = false;
                            var newPropNode = SimpleToNode(descriptor, property, subItem, node, property.PropertyType,
                                parentId);
                            newNodes.Add(newPropNode);
                            node.Childs.Add(newPropNode);
                        }
                    }
                    else
                    {
                        foreach (var subItem in propItems)
                        { // complex type collection element analyzing
                            var oldItemNode = oldList.FirstOrDefault(el =>
                            {
                                if (el.RefValue == null)
                                    return false;
                                if (el.RefValue.Target == null && subItem != null)
                                    return false;
                                return subItem == el.RefValue.Target;
                            });
                            if (oldItemNode == null)
                            {
                                result = false;
                                var itemNode = new Node()
                                {
                                    Name = descriptor.GetKey(property),
                                    Parrent = node,
                                    RepairId = parentId
                                };
                                Convert(subItem, itemNode, descriptor, parentId);
                                node.Childs.Add(itemNode);
                                newNodes.AddRange(NodeLiner.ToSetNodes(itemNode));
                            }
                            else
                            {
                                result &= UpdateTree(subItem, oldItemNode, descriptor, parentId, newNodes, lessNodes,
                                    changedNodes);
                            }
                        }
                    }
                    continue;
                }

                // complex type element analyzing
                if (oldPropNode == null)
                {
                    if (propValue == null)
                        continue;
                    result = false;
                    var propNode = new Node() { Name = newPropName, Parrent = node, RepairId = parentId };
                    Convert(propValue, propNode, descriptor, parentId);
                    node.Childs.Add(propNode);
                    newNodes.AddRange(NodeLiner.ToSetNodes(propNode));
                }
                else
                {
                    result &= UpdateTree(propValue, oldPropNode, descriptor, parentId, newNodes, lessNodes, changedNodes);
                }
            }
            return result;
        }

        /// <summary>
        /// Break links
        /// </summary>
        /// <param name="deletedNodes"></param>
        public static void DeleteLinksNodes(IEnumerable<Node> deletedNodes)
        {
            foreach (var deletedNode in deletedNodes)
            {
                deletedNode.Parrent.Childs.Remove(deletedNode);
            }
        }

        /// <summary>
        /// Convert 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="res"></param>
        /// <param name="targetType"></param>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public static bool TryParse(Node root, out object res, Type targetType, IItemKeyFactory descriptor)
        {
            try
            {
                object oldRes = null;
                if (root.Val == null && root.RefValue != null)
                {
                    oldRes = root.RefValue.Target;
                }
                else if (root.Val != null)
                {
                    oldRes = root.Val;
                }
                if (oldRes != null && targetType.IsAssignableFrom(oldRes.GetType()))
                {
                    res = oldRes;
                    return true;
                }

                res = targetType.Assembly.CreateInstance(targetType.FullName);
                var properties = targetType.GetProperties().Where(p => p.CanRead && p.CanWrite);
                foreach (var property in properties)
                {
                    var key = descriptor.GetKey(property);
                    var item = root.Childs.FirstOrDefault(node => node.Name == key);
                    if (item == null)
                        continue;
                    if (IsSimple(property.PropertyType))
                    {// разбор простых типов
                        var sVal = GetSimpleValue(item);
                        var sType = property.PropertyType;
                        if (sType.IsEnum)
                        {
                            if (sType.IsEnumDefined(sVal))
                                property.SetValue(res, Enum.ToObject(sType, sVal), null);
                        }
                        else
                            property.SetValue(res, sVal, null);
                        continue;
                    }

                    if (IsList(property.PropertyType))
                    {// разбор коллекции
                        var propNodes = root.Childs.Where(node => node.Name == key);

                        var typeItem = property.PropertyType.GetGenericArguments()[0];

                        var listType = typeof(List<>).MakeGenericType(typeItem);
                        var paramListValue = (IList)Activator.CreateInstance(listType);
                        foreach (var propNode in propNodes)
                        {
                            if (IsSimple(typeItem))
                            {
                                var sVal = GetSimpleValue(propNode);
                                if (typeItem.IsEnum)
                                {
                                    if (typeItem.IsEnumDefined(sVal))
                                        paramListValue.Add(Enum.ToObject(typeItem, sVal));
                                }
                                else
                                    paramListValue.Add(sVal); //наполнение коллекции простых типов
                            }
                            else
                            {
                                object itemElement;
                                if (!TryParse(propNode, out itemElement, typeItem, descriptor))
                                    continue;
                                paramListValue.Add(itemElement); //наполнение коллекции сложных типов
                            }
                        }
                        property.SetValue(res, paramListValue, null);
                        continue;
                    }

                    object itemTarget; // разбор сложного типа
                    if (!TryParse(item, out itemTarget, property.PropertyType, descriptor))
                        continue;

                    property.SetValue(res, itemTarget, null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                res = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Сравнение значения ноды простого типа с новым значенем
        /// </summary>
        /// <param name="node">нода</param>
        /// <param name="rlValue">новое значение</param>
        /// <returns></returns>
        private static bool CompareSimple(Node node, object rlValue)
        {
            if (rlValue == null)
                return node.Val == null;
            //if (oldPropNode.TypeVal == (int) DbType.DateTime)
            //{
            //    var rdata = (DateTime) rlValue;
            //    var ldata = (DateTime) oldPropNode.Val;
            //    return ldata == rdata;
            //}
            if (rlValue.GetType().IsEnum)
                return node.Val.Equals((int)rlValue);
            return node.Val.Equals(rlValue);
        }

        /// <summary>
        /// Формирование ноды из свойства простого типа
        /// </summary>
        /// <param name="descriptor">Вычеслитель ключей</param>
        /// <param name="property">Свойство</param>
        /// <param name="val">зачение</param>
        /// <param name="parrent">родительская нода</param>
        /// <param name="typeItem">Тип значения</param>
        /// <param name="repairId">Id проверки</param>
        /// <returns></returns>
        private static Node SimpleToNode(IItemKeyFactory descriptor, PropertyInfo property, object val, Node parrent, Type typeItem, long repairId)
        {
            var type = GetTypeValue(typeItem);
            //if (type == DbType.DateTime)
            //    val = ((DateTime) val).ToBinary();
            var res = typeItem.IsEnum ? (int)val : val;
            return new Node()
            {
                Name = descriptor.GetKey(property),
                Val = res,
                Parrent = parrent,
                TypeVal = (int)type,
                RepairId = repairId,
            };
        }

        /// <summary>
        /// Получить значение их ноды простого типа
        /// </summary>
        /// <param name="item">нода</param>
        /// <returns></returns>
        private static object GetSimpleValue(Node item)
        {
            //if (item.TypeVal == (int)DbType.DateTime)
            //    return DateTime.FromBinary((long)item.Val);
            return item.Val;
        }

        /// <summary>
        /// Check Tipe for simple
        /// </summary>
        /// <param name="type">тип</param>
        /// <returns></returns>
        private static bool IsSimple(Type type)
        {
            return type == typeof(int) ||
                type == typeof(double) ||
                type == typeof(double?) ||
                type == typeof(bool) ||
                type == typeof(string) ||
                type == typeof(DateTime) ||
                type.IsEnum;
        }

        private static bool IsList(Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }

        private static readonly Dictionary<Type, DbType> TypeMap = new Dictionary<Type, DbType>
        {
            {typeof(byte), DbType.Byte},
            {typeof(sbyte), DbType.SByte},
            {typeof(short), DbType.Int16},
            {typeof(ushort), DbType.UInt16},
            {typeof(int), DbType.Int32},
            {typeof(uint), DbType.UInt32},
            {typeof(long), DbType.Int64},
            {typeof(ulong), DbType.UInt64},
            {typeof(float), DbType.Single},
            {typeof(double), DbType.Double},
            {typeof(double?), DbType.Double},
            {typeof(decimal), DbType.Decimal},
            {typeof(bool), DbType.Boolean},
            {typeof(string), DbType.String},
            {typeof(char), DbType.StringFixedLength},
            {typeof(Guid), DbType.Guid},
            {typeof(DateTime),  DbType.DateTime},
            {typeof(DateTimeOffset), DbType.DateTimeOffset},
            {typeof(TimeSpan), DbType.Time},
            //[typeof(byte[])] = DbType.Binary,
            //[typeof(byte?)] = DbType.Byte,
            //[typeof(sbyte?)] = DbType.SByte,
            //[typeof(short?)] = DbType.Int16,
            //[typeof(ushort?)] = DbType.UInt16,
            //[typeof(int?)] = DbType.Int32,
            //[typeof(uint?)] = DbType.UInt32,
            //[typeof(long?)] = DbType.Int64,
            //[typeof(ulong?)] = DbType.UInt64,
            //[typeof(float?)] = DbType.Single,
            //[typeof(double?)] = DbType.Double,
            //[typeof(decimal?)] = DbType.Decimal,
            //[typeof(bool?)] = DbType.Boolean,
            //[typeof(char?)] = DbType.StringFixedLength,
            //[typeof(Guid?)] = DbType.Guid,
            //[typeof(DateTime?)] = DbType.DateTime,
            //[typeof(DateTimeOffset?)] = DbType.DateTimeOffset,
            //[typeof(TimeSpan?)] = DbType.Time,
            //[typeof(object)] = DbType.Object
        };

        private static DbType GetTypeValue(Type val)
        {
            if (val.IsEnum)
                return DbType.Int32;
            if (!TypeMap.ContainsKey(val))
                throw new KeyNotFoundException(String.Format("For type {0} not found DbType", val));
            return TypeMap[val];
        }


        private static string ValueToString(Node node)
        {
            if (node.TypeVal == (int)DbType.DateTime)
                return node.Val == null ? "NULL" : String.Format(CultureInfo.InvariantCulture, "'{0}'", ((DateTime)node.Val).ToBinary());
            return node.Val == null ? "NULL" : String.Format(CultureInfo.InvariantCulture, "'{0}'", node.Val);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <param name="typeVal"></param>
        /// <returns></returns>
        private static object ParceValue(string val, int typeVal)
        {
            var t = (DbType)typeVal;
            object res = val;

            try
            {
                switch (t)
                {
                    case DbType.Byte:
                        res = Byte.Parse(val);
                        break;
                    case DbType.Boolean:
                        res = Boolean.Parse(val);
                        break;
                    case DbType.DateTime:
                        res = DateTime.FromBinary(Int64.Parse(val));
                        break;
                    case DbType.Decimal:
                        res = Decimal.Parse(val);
                        break;
                    case DbType.Double:
                        res = Double.Parse(val);
                        break;
                    case DbType.Guid:
                        res = Guid.Parse(val);
                        break;
                    case DbType.Int16:
                        res = Int16.Parse(val);
                        break;
                    case DbType.Int32:
                        res = Int32.Parse(val);
                        break;
                    case DbType.Int64:
                        res = Int64.Parse(val);
                        break;
                    case DbType.SByte:
                        res = SByte.Parse(val);
                        break;
                    case DbType.Single:
                        res = Single.Parse(val);
                        break;
                    case DbType.String:
                        break;
                    case DbType.Time:
                        res = TimeSpan.Parse(val);
                        break;
                    case DbType.UInt16:
                        res = UInt16.Parse(val);
                        break;
                    case DbType.UInt32:
                        res = UInt32.Parse(val);
                        break;
                    case DbType.UInt64:
                        res = UInt64.Parse(val);
                        break;
                    case DbType.StringFixedLength:
                        res = val[0];
                        break;
                    case DbType.DateTimeOffset:
                        res = DateTimeOffset.Parse(val);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
            return res;
        }
    }
}
