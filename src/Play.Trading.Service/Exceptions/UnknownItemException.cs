using System;

namespace Play.Trading.Service.Exceptions
{
    [Serializable]
    public class UnknownItemException : Exception
    {
        public Guid ItemID { get; }

        public UnknownItemException(Guid itemID)
            : base($"Unknow item '{itemID}'")
        {
            ItemID = itemID;
        }
    }
}
