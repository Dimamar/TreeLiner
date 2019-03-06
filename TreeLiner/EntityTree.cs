namespace TreeLiner
{
    public abstract class EntityTree
    {
        private bool _isNew = true;

        public bool IsNew
        {
            get { return _isNew; }
            set { _isNew = value; }
        }

        /// <summary>
        /// Идентификатор
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Идентификатор проверки к которой относится сущьность
        /// </summary>
        public long RepairId { get; set; }
    }
}
