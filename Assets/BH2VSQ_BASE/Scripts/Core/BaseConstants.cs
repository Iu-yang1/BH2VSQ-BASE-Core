namespace BH2VSQ.Base
{
    public static class BaseConstants
    {
        public const int MaxPlayers = 80;
        public const int PlayerRowsPerPage = 5;
        public const int InvalidId = int.MinValue;
        public const float XpInterval = 30f;
        public const float RequestTimeout = 30f;
    }

    public enum BaseRank { Visitor = 0, Member = 1, Admin = 2 }
    public enum FloorState { Open = 0, Reserved = 1, Maintenance = 2 }
    public enum AccessResult { Allowed = 0, InvalidPlayer = 1, InvalidFloor = 2, InvalidArea = 3, InsufficientRank = 4, FloorReserved = 5, FloorMaintenance = 6 }
}
