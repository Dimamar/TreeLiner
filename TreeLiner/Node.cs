using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TreeLiner
{
    [DebuggerDisplay("ID:{Id} PId:{ParrentId} {Name}:{Val}")]
    public class Node : EntityTree
    {
        private static int _maxId = 0;
        private List<Node> _childs = new List<Node>();
        private Node _parrent;

        /// <summary>
        /// Метод получени нового GUID
        /// </summary>
        /// <returns></returns>
        public static int GetNewId()
        {
            return _maxId++;
        }

        /// <summary>
        /// Установка предыдущего максимального Id
        /// </summary>
        /// <returns></returns>
        public static void SetMaxId(int maxId)
        {
            _maxId = maxId;
        }

        public Node()
        {
            Id = GetNewId();
        }


        public Node(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Имя узла
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Родитель
        /// </summary>
        public Node Parrent
        {
            get { return _parrent; }
            set
            {
                _parrent = value;
                ParrentId = _parrent.Id;
            }
        }

        /// <summary>
        /// Идентификатор родителя 
        /// </summary>
        public long ParrentId { get; set; }

        /// <summary>
        /// Дети
        /// </summary>
        public List<Node> Childs
        {
            get { return _childs; }
            set { _childs = value; }
        }

        /// <summary>
        /// Значение
        /// </summary>
        public object Val { get; set; }

        /// <summary>
        /// Тип значения 
        /// </summary>
        public int TypeVal { get; set; }

        /// <summary>
        /// Ссылка на значение сложного типа
        /// </summary>
        /// <remarks>
        /// Существует, когда значение простого типа <see cref="Val"/> == null
        /// </remarks>
        public WeakReference RefValue { get; set; }

        public Node this[string key]
        {
            get
            {
                var res = Childs.FirstOrDefault(item => item.Name == key);
                return res;
            }
            set
            {
                var node = Childs.FirstOrDefault(item => item.Name == key);
                if (value == node)
                    return;
                if (node != null)
                    Childs.Remove(node);
                Childs.Add(value);
            }
        }

        public override bool Equals(object obj)
        {
            var rVal = obj as Node;
            if (rVal == null)
                return false;
            if (Id != rVal.Id)
                return false;
            if (Name != rVal.Name)
                return false;
            if (TypeVal != rVal.TypeVal)
                return false;
            if (Val != rVal.Val)
                return false;
            if (Val != rVal.Val)
                return false;
            //if (ParrentId != rVal.ParrentId)
            //    return false;
            if (Childs.Where((el, i) => rVal.Childs[i].Id!=el.Id).Any())
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
