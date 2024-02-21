namespace ProcDungeon
{
    [System.Serializable]
    public struct DungeonLevelSetting
    {
        #region Grid
        public float tileSize;
        public int gridSizeColumns;
        public int gridSizeRows;
        #endregion

        #region Segmentation
        public float overSplitProbability;
        public float underSplitProbability;
        public int maxNumberOfSegments;
        public int minSegmentLength;
        #endregion

        #region Rooms
        public int roomPartMinOverlap;
        public int maxRoomArea;
        public int minRooms;
        public int maxRooms;
        public int maxSegmentsPerRoom;
        public float multiSegmentRoomProbability;
        #endregion

        #region Hallways
        public int exitCandidateTolerance;
        #endregion

        public DungeonLevelSetting(int gridSize)
        {
            tileSize = 1.0f;
            gridSizeColumns = gridSize;
            gridSizeRows = gridSize;

            overSplitProbability = 0.05f;
            underSplitProbability = 0.025f;
            maxNumberOfSegments = gridSize * gridSize / 10;
            minSegmentLength = 4;

            roomPartMinOverlap = 2;
            maxRoomArea = 200;
            this.minRooms = gridSize / 3;
            this.maxRooms = gridSize / 2;
            maxSegmentsPerRoom = 5;
            multiSegmentRoomProbability = 0.4f;

            exitCandidateTolerance = 2;
        }

        public DungeonLevelSetting(int gridColumns, int gridRows, int minRooms, int maxRooms)
        {
            tileSize = 1.0f;
            gridSizeColumns = gridColumns;
            gridSizeRows = gridRows;

            overSplitProbability = 0.05f;
            underSplitProbability = 0.025f;
            maxNumberOfSegments = gridRows * gridColumns / 10;
            minSegmentLength = 4;

            roomPartMinOverlap = 2;
            maxRoomArea = 200;
            this.minRooms = minRooms;
            this.maxRooms = maxRooms;
            maxSegmentsPerRoom = 5;
            multiSegmentRoomProbability = 0.4f;

            exitCandidateTolerance = 2;
        }
    }
}