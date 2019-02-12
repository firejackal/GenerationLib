using System.Collections.Generic;

namespace GenerationLib
{
    namespace MapGenerator
    {
        public enum CardinalDirections { North, South, West, East }

        public enum FilterModes { Optional, On, Off }

        public enum RoomTypes { Room, Passage }

        public abstract class BaseFilterManager
        {
            public string      SubsetID    = ""; //Nothing/Empty is optional, else it must match the string.
            public FilterModes RoomType    = FilterModes.Off; //Includes how to check for room types.
            public FilterModes PassageType = FilterModes.Off; //Includes how to check for passage types.

            public BaseFilterManager() {}

            public BaseFilterManager(BaseFilterManager clone)
            {
                if(clone == null) return;
                this.SubsetID    = clone.SubsetID;
                this.RoomType    = clone.RoomType;
                this.PassageType = clone.PassageType;
            } //Constructor

            public virtual void SetAll(FilterModes mode, string subSetID = "")
            {
                this.SubsetID = (mode == FilterModes.On ? subSetID : "");
                this.RoomType = mode;
                this.PassageType = mode;
            } //SetAll

            public virtual void SetAll(FilterModes mode, FilterModes requiredMode, string subSetID = "")
            {
                this.SubsetID = (mode == FilterModes.On ? subSetID : "");
                if(this.RoomType == requiredMode) this.RoomType = mode;
                if(this.PassageType == requiredMode) this.PassageType = mode;
            } //SetAll

            public virtual void FillFromNeighbors(BaseFilterManager northNeighbor, BaseFilterManager southNeighbor, BaseFilterManager westNeighbor, BaseFilterManager eastNeighbor, string subSetID = "")
            {
                this.SetAll(FilterModes.Optional);
                this.SubsetID = subSetID;
            } //MakeFilterFromNeighbors

            public static BaseFilterManager CreateFromNeighbors(BaseFilterManager northNeighbor, BaseFilterManager southNeighbor, BaseFilterManager westNeighbor, BaseFilterManager eastNeighbor, string subSetID = "")
            {
                BaseFilterManager results = northNeighbor.Clone();
                results.FillFromNeighbors(northNeighbor, southNeighbor, westNeighbor, eastNeighbor, subSetID);
                return results;
            } //CreateFromNeighbors

            public abstract BaseFilterManager Clone();
            public abstract void SetConnectionOff(CardinalDirections direction, bool onlyIfOptional = false);
            public abstract void SetConnectionOn(CardinalDirections direction);
            public abstract BaseFilterManager MakeEmpty(); //would be shared but cannot make shared into a overridable
        } //BaseFilterManager class

        public class RoomsDataCollection : List<BaseRoomData>
        {
            private System.Random mRandom = new System.Random();

            public int FindIndex(string id)
            {
                if(base.Count > 0) {
                    for(int index = 0; index < base.Count; index++) {
                        if(string.Equals(base[index].ID, id, System.StringComparison.CurrentCultureIgnoreCase)) return index;
                    } //index
                }

                return -1;
            } //FindIndex

            public BaseRoomData this[string id]
            {
                get {
                    int index = this.FindIndex(id);
                    if(index < 0) return null;
                    return base[index];
                }
            } //Item

            public void EnumSubsetIDs(ref List<string> results)
            {
                if(results == null) results = new List<string>();
                
                if(base.Count > 0) {
                    foreach(BaseRoomData room in this) {
                        if(!results.Contains(room.SubSetID)) results.Add(room.SubSetID);
                    } //room
                }
            } //EnumSubsetIDs

            public void AddFromPath(string pathName, BaseRoomData baseRoom)
            {
                string[] foundFiles = System.IO.Directory.GetFiles(pathName, "*.rooms", System.IO.SearchOption.AllDirectories);
                if(foundFiles != null || foundFiles.Length == 0) return;

                foreach(string foundFile in foundFiles) {
                    this.LoadFromFile(foundFile, baseRoom);
                } //foundfile
            } //AddRoomsFromPath

            public bool LoadFromFile(string fileName, BaseRoomData baseRoom)
            {
                XINI cXINI = new XINI();
                if(!cXINI.LoadFromFile(fileName)) return false;

                XINI.EntryItem parentEntry = cXINI.Root.AppendChildEntry("Rooms", XINI.AppendModes.Read);
                AppendRoomsData(parentEntry, baseRoom, XINI.AppendModes.Read);

                return true;
            } //LoadFromFile

            public bool SaveToFile(string fileName, BaseRoomData baseRoom)
            {
                XINI cXINI = new XINI();
                cXINI.Name = "Rooms";
                cXINI.Version = "1";

                XINI.EntryItem parentEntry = cXINI.Root.AppendChildEntry("Rooms", XINI.AppendModes.Save);
                AppendRoomsData(parentEntry, baseRoom, XINI.AppendModes.Save);

                return cXINI.SaveToFile(fileName);
            } //SaveToFile

            public void AppendRoomsData(XINI.EntryItem parentEntry, BaseRoomData baseRoom, XINI.AppendModes appendMode)
            {
                string subSetID = "";
                if(appendMode == XINI.AppendModes.Save && base.Count > 0)
                    subSetID = base[0].SubSetID;
                parentEntry.AppendChildEntryValue("Subset ID", ref subSetID, appendMode);

                if(appendMode == XINI.AppendModes.Read) {
                    if(parentEntry.Children != null) {
                        foreach(XINI.EntryItem childEntry in parentEntry.Children) {
                            if(string.Equals(childEntry.Name, "room", System.StringComparison.CurrentCultureIgnoreCase)) {
                                BaseRoomData newData = baseRoom.Clone();
                                newData.AppendData(childEntry, appendMode, subSetID);
                                base.Add(newData);
                            }
                        } //childEntry
                    }
                } else if(appendMode == XINI.AppendModes.Save) {
                    if(base.Count > 0) {
                        for(int index = 0; index < base.Count; index++) {
                            XINI.EntryItem childEntry = parentEntry.AddChild("Room");
                            base[index].AppendData(childEntry, appendMode, subSetID);
                        } //index
                    }
                }
            } //AppendData

            public BaseFilterManager MakeEmptyFilter()
            {
                // Make a base filter from a filter in the rooms data.
                if(base.Count == 0) return null;
                return base[0].GetFilterRequirement().MakeEmpty();
            } //MakeEmptyFilter

#region "Filter Finding"
            public void EnumRooms(BaseFilterManager filter, ref List<BaseRoomData> results)
            {
                if(results == null) results = new List<BaseRoomData>();

                foreach(BaseRoomData room in this) {
                    if(room.MatchesWithFilter(filter)) results.Add(room);
                } //room
            } //EnumRooms

            public BaseRoomData FindRandomRoom(BaseFilterManager filter) { return FindRandomRoom(filter, this.mRandom); }

            public BaseRoomData FindRandomRoom(BaseFilterManager filter, int seed) { return FindRandomRoom(filter, new System.Random(seed)); }
            
            public BaseRoomData FindRandomRoom(BaseFilterManager filter, System.Random random)
            {
                // Find the rooms that is applied by this filter.
                List<BaseRoomData> foundRooms = new List<BaseRoomData>();
                this.EnumRooms(filter, ref foundRooms);
                if(foundRooms == null || foundRooms.Count == 0) return null;

                // Randomly select one room.
                return foundRooms[random.Next(0, foundRooms.Count)];
            } //FindRandomRoom
#endregion 'Filter Finding
        } //RoomsDataCollection

        public abstract class BaseRoomData
        {
            public string    ID       = "";
            public string    SubSetID = ""; // the general sub set ID of this room
            public RoomTypes Type     = RoomTypes.Room;

            public BaseRoomData() {}

            public BaseRoomData(string id, RoomTypes type, string subSetID = "")
            {
                this.ID       = id;
                this.Type     = type;
                this.SubSetID = subSetID;
            } //New

            public abstract bool HasConnection(CardinalDirections direction, BaseRoomData b);
            public abstract BaseFilterManager GetFilterRequirement();
            public abstract bool MatchesWithFilter(BaseFilterManager filter);
            public abstract bool IsSideOpened(CardinalDirections direction);
            public abstract BaseRoomData Clone();

            public virtual void AppendData(XINI.EntryItem parentEntry, XINI.AppendModes appendMode, string defaultSubSetID = "")
            {
                if(appendMode == XINI.AppendModes.Read) {
                    this.SubSetID = parentEntry.GetChildValue("Subset ID", defaultSubSetID);
                } else if(appendMode == XINI.AppendModes.Save) {
                    if(!this.SubSetID.Equals(defaultSubSetID, System.StringComparison.CurrentCultureIgnoreCase))
                        parentEntry.SetChildValue("Subset ID", this.SubSetID);
                }

                parentEntry.AppendChildEntryValue("ID", ref this.ID, appendMode);
                parentEntry.AppendChildEntryEnumValue<RoomTypes>("Type", ref this.Type, appendMode, RoomTypes.Room.ToString(), RoomTypes.Room.ToString());
            } //AppendData
        } //BaseRoomData

        public class DirectionalRoomData : BaseRoomData
        {
            public bool North;
            public bool South;
            public bool West;
            public bool East;

            public DirectionalRoomData() {}

            public DirectionalRoomData(string id, RoomTypes type, bool north, bool south, bool west, bool east, string subSetID = "")
                : base(id, type, subSetID)
            {
                this.North = north;
                this.South = south;
                this.West  = west;
                this.East  = east;
            } //Constructor

            public DirectionalRoomData(DirectionalRoomData clone)
            {
                if(clone == null) return;
                this.ID       = clone.ID;
                this.Type     = clone.Type;
                this.SubSetID = clone.SubSetID;
                this.North    = clone.North;
                this.South    = clone.South;
                this.West     = clone.West;
                this.East     = clone.East;
            } //Constructor

            public int Count() { return 4; }

            /// <value>A value from 0 to count-1</value>
            public bool this[CardinalDirections index]
            {
                get {
                    switch(index) {
                        case(CardinalDirections.North) : return this.North;
                        case(CardinalDirections.South) : return this.South;
                        case(CardinalDirections.East)  : return this.East;
                        case(CardinalDirections.West)  : return this.West;
                        default : return false;
                    }
                }
                set {
                    switch(index) {
                        case(CardinalDirections.North) : this.North = value; break;
                        case(CardinalDirections.South) : this.South = value; break;
                        case(CardinalDirections.East)  : this.East = value; break;
                        case(CardinalDirections.West)  : this.West = value; break;
                    }
                }
            } //Item

            public override bool HasConnection(CardinalDirections direction, BaseRoomData b)
            {
                // make sure there is a valid target data.
                DirectionalRoomData target = (DirectionalRoomData)b;
                if(target == null) return false;
                // return if the source and the target has a connection.
                return (this[direction] && target[MapHelpers.OppositeCardianDirection(direction)]);
            } //HasConnection

            /// <summary>Uses a room's data as a reference point to setup this filter.</summary>
            public override BaseFilterManager GetFilterRequirement()
            {
                DirectionalRoomFilter results = new DirectionalRoomFilter();

                // Set the required sub set ID
                results.SubsetID = this.SubSetID;

                // Setup the room/passage types.
                results.RoomType = FilterModes.Off;
                results.PassageType = FilterModes.Off;
                if(this.Type == RoomTypes.Room)
                    // ... Room filter must be on.
                    results.RoomType = FilterModes.On;
                else if(this.Type == RoomTypes.Passage)
                    // ... Passage filter must be on.
                    results.PassageType = FilterModes.On;

                // Determine the direction values.
                for(CardinalDirections direction = 0; direction < (CardinalDirections)this.Count(); direction++) {
                    results[direction] = (this[direction] ? FilterModes.On : FilterModes.Off);
                } //direction

                // Return the new description.
                return results;
            } //GetFilterRequirement

            /// <summary>Checks to see if a room matches a filter's description.</summary>
            /// <param name="filter">The filter to use to check the room with.</param>
            /// <returns>Returns true if the room passes all checks, false if the room does not match.</returns>
            /// <remarks>Checks all failing statements first, if non found then returns successful.</remarks>
            public override bool MatchesWithFilter(BaseFilterManager filter)
            {
                // Check the room's sub ID to match the filter's.
                if(filter == null || !(filter is DirectionalRoomFilter) || (!string.IsNullOrEmpty(filter.SubsetID) && !string.Equals(this.SubSetID, filter.SubsetID, System.StringComparison.CurrentCultureIgnoreCase))) return false;
                // Convert the filter.
                DirectionalRoomFilter ourFilter = (DirectionalRoomFilter)filter;
                if(ourFilter == null) return false;

                // Check the room types.
                if(filter.RoomType == FilterModes.On && this.Type != RoomTypes.Room)
                    return false;
                else if(filter.RoomType == FilterModes.Off && this.Type == RoomTypes.Room)
                    return false;
                else if(filter.PassageType == FilterModes.On && this.Type != RoomTypes.Passage)
                    return false;
                else if(filter.PassageType == FilterModes.Off && this.Type == RoomTypes.Passage)
                    return false;

                // Go through each direction used for checking ...
                for(int direction = 0; direction < ourFilter.Count; direction++) {
                    // ... Get the information about this direction ...
                    bool roomHasDirection = this[(CardinalDirections)direction];
                    FilterModes filterDirectionType = ourFilter[(CardinalDirections)direction];

                    // ... Check to see if this direction is against our filter ...
                    if(filterDirectionType == FilterModes.On && !roomHasDirection)
                        return false;
                    else if(filterDirectionType == FilterModes.Off && roomHasDirection)
                        return false;
                } //direction

                // Return that all is successful.
                return true;
            } //CheckRoomWithFilter

            /// <summary>Returns if the side of the room is opened (can be walked off or has an entrance.)</summary>
            public override bool IsSideOpened(CardinalDirections direction) { return this[direction]; }

            public override void AppendData(XINI.EntryItem parentEntry, XINI.AppendModes appendMode, string defaultSubSetID = "")
            {
                base.AppendData(parentEntry, appendMode, defaultSubSetID);
                parentEntry.AppendChildEntryValue("North", ref this.North, appendMode);
                parentEntry.AppendChildEntryValue("South", ref this.South, appendMode);
                parentEntry.AppendChildEntryValue("West",  ref this.West, appendMode);
                parentEntry.AppendChildEntryValue("East",  ref this.East, appendMode);
            } //AppendData

            public override BaseRoomData Clone() { return new DirectionalRoomData(this); }
        } //DirectionalRoomData

        public class DirectionalRoomFilter : BaseFilterManager
        {
            FilterModes North = FilterModes.Off;
            FilterModes South = FilterModes.Off;
            FilterModes East  = FilterModes.Off;
            FilterModes West  = FilterModes.Off;

            public DirectionalRoomFilter() {}

            public DirectionalRoomFilter(DirectionalRoomFilter clone)
            {
                if(clone == null) return;
                base.SubsetID    = clone.SubsetID;
                base.RoomType    = clone.RoomType;
                base.PassageType = clone.PassageType;
                this.North       = clone.North;
                this.South       = clone.South;
                this.West        = clone.West;
                this.East        = clone.East;
            } //Constructor

            public int Count { get { return 4; } }

            /// <value>A value from 0 to count-1</value>
            public FilterModes this[CardinalDirections index]
            {
                get {
                    switch(index) {
                        case(CardinalDirections.North) : return this.North;
                        case(CardinalDirections.South) : return this.South;
                        case(CardinalDirections.East)  : return this.East;
                        case(CardinalDirections.West)  : return this.West;
                        default : return FilterModes.Off;
                    }
                }
                set {
                    switch(index) {
                        case(CardinalDirections.North) : this.North = value; break;
                        case(CardinalDirections.South) : this.South = value; break;
                        case(CardinalDirections.East)  : this.East = value; break;
                        case(CardinalDirections.West)  : this.West = value; break;
                    }
                }
            } //Item
            
            public override void SetAll(FilterModes mode, string subSetID = "")
            {
                base.SetAll(mode, subSetID);
                for(int index = 0; index < this.Count; index++) {
                    this[(CardinalDirections)index] = mode;
                } //index
            } //SetAll

            public override void SetAll(FilterModes mode, FilterModes requiredMode, string subSetID = "")
            {
                base.SetAll(mode, requiredMode, subSetID);
                for(int index = 0; index < this.Count; index++) {
                    if(this[(CardinalDirections)index] == requiredMode) this[(CardinalDirections)index] = mode;
                } //index
            } //SetAll

            public override void FillFromNeighbors(BaseFilterManager northNeighbor, BaseFilterManager southNeighbor, BaseFilterManager westNeighbor, BaseFilterManager eastNeighbor, string subSetID = "")
            {
                base.FillFromNeighbors(northNeighbor, southNeighbor, westNeighbor, eastNeighbor, subSetID);

                if(northNeighbor != null) {
                    DirectionalRoomFilter neighborFilter = (DirectionalRoomFilter)northNeighbor;
                    if(neighborFilter != null) this[CardinalDirections.North] = neighborFilter[CardinalDirections.South];
                }

                if(southNeighbor != null) {
                    DirectionalRoomFilter neighborFilter = (DirectionalRoomFilter)southNeighbor;
                    if(neighborFilter != null) this[CardinalDirections.South] = neighborFilter[CardinalDirections.North];
                }

                if(westNeighbor != null) {
                    DirectionalRoomFilter neighborFilter = (DirectionalRoomFilter)westNeighbor;
                    if(neighborFilter != null) this[CardinalDirections.West] = neighborFilter[CardinalDirections.East];
                }

                if(eastNeighbor != null) {
                    DirectionalRoomFilter neighborFilter = (DirectionalRoomFilter)eastNeighbor;
                    if(neighborFilter != null) this[CardinalDirections.East] = neighborFilter[CardinalDirections.West];
                }
            } //MakeFilterFromNeighbors

            public override BaseFilterManager Clone() { return new DirectionalRoomFilter(this); }

            public override void SetConnectionOff(CardinalDirections direction, bool onlyIfOptional = false)
            {
                if(!onlyIfOptional || (onlyIfOptional && this[direction] == FilterModes.Optional)) this[direction] = FilterModes.Off;
            } //SetConnectionOff

            public override void SetConnectionOn(CardinalDirections direction)
            {
                this[direction] = FilterModes.On;
            } //SetConnectionOn

            public override BaseFilterManager MakeEmpty()
            {
                DirectionalRoomFilter results = new DirectionalRoomFilter();
                results.SubsetID = "";
                results.RoomType = FilterModes.Off;
                results.PassageType = FilterModes.Off;
                results.North = FilterModes.Off;
                results.South = FilterModes.Off;
                results.East = FilterModes.Off;
                results.West = FilterModes.Off;
                return results;
            } //MakeEmpty
        } //DirectionalRoomFilter

        public class MapData
        {
            private int mWidth, mHeight; //The size of the scene.
            private TileManager[,] mTile; //The tile collection that contains data for each tile.

            public MapData(int width, int height) { this.Resize(width, height); }

            public int Width { get { return this.mWidth; } }

            public int Height { get { return this.mHeight; } }

            public TileManager Tile(int column, int row) { return this.mTile[column - 1, row - 1]; }

            public void Resize(int width, int height)
            {
                if(width > 0 && height > 0) {
                    this.mWidth = width;
                    this.mHeight = height;
                    this.mTile = new TileManager[this.mWidth, this.mHeight];

                    for(int row = 0; row < this.mHeight; row++) {
                        for(int column = 0; column < this.mWidth; column++) {
                            this.mTile[column, row] = new TileManager();
                        } //column
                    } //row
                } else {
                    this.mWidth = 0;
                    this.mHeight = 0;
                    this.mTile = null;
                }
            } //Resize

#region "Tile Helpers"
            /// <summary>Returns true if two tiles next to eachother is connected.</summary>
            public bool IsTilesConnected(int column, int row, CardinalDirections direction)
            {
                // Get this tile's type.
                MapData.TileManager thisTileData = this.mTile[column - 1, row - 1];
                if(thisTileData.Room == null) return false;

                // First get target the location.
                int targetTileX, targetTileY; MapHelpers.GetDirectionTile(column, row, direction, out targetTileX, out targetTileY, this.mWidth, this.mHeight);
                if(targetTileX == 0 && targetTileY == 0) return false;

                // Get the target tile type.
                MapData.TileManager targetTileData = this.mTile[targetTileX - 1, targetTileY - 1];
                if(targetTileData.Room == null) return false;

                // Now check if the directions match.
                return thisTileData.Room.HasConnection(direction, targetTileData.Room);
            } //IsTilesConnected

            public bool MakeConnection(int column, int row, CardinalDirections direction, RoomsDataCollection rooms, bool ignoreEmptyTiles = false, bool ignoreBoundries = false, string subSetID = "", bool allowRooms = true, bool allowPassages = true, bool keepOriginalType = true)
            {
                if(column == 0 || row == 0) return false;

                // First check to make sure if there already isn't a connection, if there is return true.
                if(this.IsTilesConnected(column, row, direction)) return true;

                // Get this tile's type.
                MapData.TileManager thisTileData = this.mTile[column - 1, row - 1];
                if(thisTileData.Room == null && !ignoreEmptyTiles) return false;

                // First get target the location.
                int targetTileLocationX, targetTileLocationY;
                MapHelpers.GetDirectionTile(column, row, direction, out targetTileLocationX, out targetTileLocationY, this.mWidth, this.mHeight);
                if(targetTileLocationX == 0 && targetTileLocationY == 0) return false;

                // Get the target tile type.
                MapData.TileManager targetTileData = this.mTile[targetTileLocationX - 1, targetTileLocationY - 1];
                if(targetTileData.Room == null && !ignoreEmptyTiles) return false;

                // if(either of the room's are permament then ... return failure.
                if(thisTileData.Permanant || targetTileData.Permanant) return false;

                // Make the connection for this tile.
                if(thisTileData.Room != null)
                    this.MakeOneSideConnection(column, row, direction, rooms, ignoreBoundries, subSetID, allowRooms, allowPassages, keepOriginalType);

                // Make the connection for the target tile.
                if(targetTileData.Room != null)
                    this.MakeOneSideConnection(targetTileLocationX, targetTileLocationY, MapHelpers.OppositeCardianDirection(direction), rooms, ignoreBoundries, subSetID, allowRooms, allowPassages, keepOriginalType);

                return true;
            } //MakeConnection

            /// <summary>Makes the tile has a connection to a direction.</summary>
            public bool MakeOneSideConnection(int column, int row, CardinalDirections direction, RoomsDataCollection rooms, bool ignoreBoundries = false, string subSetID = "", bool allowRooms = true, bool allowPassages = true, bool keepOriginalType = true)
            {
                if(column == 0 || row == 0 || this.mTile[column - 1, row - 1].Permanant || this.mTile[column - 1, row - 1].Room == null) return false;

                BaseFilterManager targetFilter = MapHelpers.ComputeTileRequirementFilter(this, column, row, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID, allowRooms, allowPassages);
                if(keepOriginalType) {
                    BaseFilterManager originalFilter = this.mTile[column - 1, row - 1].Room.GetFilterRequirement();
                    targetFilter.RoomType = originalFilter.RoomType;
                    targetFilter.PassageType = originalFilter.PassageType;
                }
                targetFilter.SetConnectionOn(direction);

                if(targetFilter != null) MapHelpers.PlotRandomRoom(this, rooms, column, row, targetFilter);
                return true;
            } //MakeOneSideConnection

            /// <summary>Generate a path from a start tile to an end tile.</summary>
            public bool PlotPath(RoomsDataCollection rooms, int startColumn, int startRow, int endColumn, int endRow, bool ignoreBoundries = false, string subSetID = "")
            {
                // Use path finding to find the pathway to the exit, ignore all existing passages through.
                List<MapLocation> tileList = new List<MapLocation>();
                MapHelpers.CreateOutsidePathRoute(this, startColumn, startRow, endColumn, endRow, ref tileList, true);
                if(tileList == null || tileList.Count == 0) return false;
                // Plot the path using the tile list.
                return PlotPath(rooms, tileList, ignoreBoundries, subSetID);
            } //PlotPath

            /// <summary>Plot a path.</summary>
            public bool PlotPath(RoomsDataCollection rooms, List<MapLocation> tiles, bool ignoreBoundries = false, string subSetID = "")
            {
                if(tiles == null || tiles.Count == 0) return false;

                for(int index = 0; index < tiles.Count; index++) {
                    BaseFilterManager filter = MapHelpers.ComputeTileRequirementFilter(this, tiles[index].X, tiles[index].Y, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID);
                    filter.SetAll(FilterModes.Off, FilterModes.Optional, subSetID);
                    filter.PassageType = FilterModes.On;
                    filter.RoomType = FilterModes.Off;

                    // Turn on just for the directions needed.
                    if(index > 0) {
                        CardinalDirections targetDirection = MapHelpers.GetTileDirection(tiles[index].X, tiles[index].Y, tiles[index - 1].X, tiles[index - 1].Y);
                        filter.SetConnectionOn(targetDirection);
                    }
                    if(index < (tiles.Count - 1)) {
                        CardinalDirections targetDirection = MapHelpers.GetTileDirection(tiles[index].X, tiles[index].Y, tiles[index + 1].X, tiles[index + 1].Y);
                        filter.SetConnectionOn(targetDirection);
                    }

                    MapHelpers.PlotRandomRoom(this, rooms, tiles[index].X, tiles[index].Y, filter);
                } //index

                return true;
            } //PlotPath
#endregion //Tile Helpers

            public void ClearData()
            {
                if(this.mWidth <= 0 || this.mHeight <= 0) return;

                for(int row = 0; row < this.mHeight; row++) {
                    for(int column = 0; column < this.mWidth; column++) {
                        this.mTile[column, row].Clear();
                    } //column
                } //row
            } //ClearData

            public class TileManager
            {
                public bool         Permanant;
                public BaseRoomData Room;

                public void Clear()
                {
                    this.Permanant = false;
                    this.Room = null;
                } //Clear

                public bool IsRoom() { return (this.Room != null && this.Room.Type == MapGenerator.RoomTypes.Room); }

                public bool IsPassage() { return (this.Room != null && this.Room.Type == MapGenerator.RoomTypes.Passage); }
            } //TileManager
        } //MapData

        /// <summary>Provides functions for enumeration functions of the map.</summary>
        public static class Enumerations
        {
            public struct TileSideDescription
            {
                public int Column;
                public int Row;
                public int TargetColumn;
                public int TargetRow;
                public CardinalDirections Direction;

                public TileSideDescription(int column, int row, CardinalDirections direction, int targetColumn, int targetRow)
                {
                    this.Column       = column;
                    this.Row          = row;
                    this.Direction    = direction;
                    this.TargetColumn = targetColumn;
                    this.TargetRow    = targetRow;
                } //Constructor
            } //TileSideDescription

            /// <summary>Returns a list of rooms (tiles) locations which is of a certain map type.</summary>
            public static void EnumRoomsByType(MapData map, RoomTypes type, ref List<MapLocation> results)
            {
                EnumRoomsByType(map, type == RoomTypes.Room, type == RoomTypes.Passage, ref results);
            } //EnumRoomsByType

            /// <summary>Returns a list of rooms (tiles) locations which is of a certain map type.</summary>
            public static void EnumRoomsByType(MapData map, bool room, bool passage, ref List<MapLocation> results)
            {
                if(map == null) return;
                if(results == null) results = new List<MapLocation>();

                if(map.Width > 0 && map.Height > 0) {
                    for(int row = 1; row <= map.Height; row++) {
                        for(int column = 1; column <= map.Width; column++) {
                            if(map.Tile(column, row).Room != null) {
                                if((map.Tile(column, row).Room.Type == RoomTypes.Room && room) ||
                                    (map.Tile(column, row).Room.Type == RoomTypes.Passage && passage))
                                    results.Add(new MapLocation(column, row));
                            }
                        } //column
                    } //row
                }
            } //EnumRoomsByType

            public static void EnumRoomSetIndexes(MapData map, ref List<int> results)
            {
                int[,] roomSetTable;
                MapHelpers.DetermineRoomSetsTable(map, out roomSetTable);
                EnumRoomSetIndexes(roomSetTable, map.Width, map.Height, ref results);
            } //EnumRoomSetIndexes

            public static void EnumRoomSetIndexes(int[,] roomSetIndexTable, int width, int height, ref List<int> results)
            {
                if(results == null) results = new List<int>();

                for(int row = 0; row < height; row++) {
                    for(int column = 0; column < width; column++) {
                        if(roomSetIndexTable[column, row] != 0 && !results.Contains(roomSetIndexTable[column, row])) results.Add(roomSetIndexTable[column, row]);
                    } //column
                } //row
            } //EnumRoomSetIndexes

            public static void EnumRoomsByRoomSetIndex(int[,] roomSetIndexTable, int width, int height, int roomSetIndex, ref List<MapLocation> results)
            {
                if(results == null) results = new List<MapLocation>();

                for(int row = 0; row < height; row++) {
                    for(int column = 0; column < width; column++) {
                        if(roomSetIndexTable[column, row] == roomSetIndex) results.Add(new MapLocation(1 + column, 1 + row));
                    } //column
                } //row
            } //EnumRoomsByRoomSetIndex

            /// <summary>Returns all tiles in a path set that has a foreign neighbor tile next to it.</summary>
            public static void EnumPathSetWithForeignNeighbor(MapData map, int[,] pathSetIndexTable, int pathSetIndex, ref List<TileSideDescription> results)
            {
                if(results == null) results = new List<TileSideDescription>();

                // Go through each tile on the board ...
                for(int row = 1; row <= map.Height; row++) {
                    for(int column = 1; column <= map.Width; column++) {
                        // ... if(this tile is part of the path set we're searching for then ...
                        if(pathSetIndexTable[column - 1, row - 1] == pathSetIndex) {
                            // ... Go through each direction on this currnet tile.
                            for(CardinalDirections direction = 0; direction < (CardinalDirections)4; direction++) {
                                // ... Get the tile at this location ...
                                int directionTileX, directionTileY;
                                MapHelpers.GetDirectionTile(column, row, direction, out directionTileX, out directionTileY, map.Width, map.Height);
                                // ... Make sure the tile is valid ...
                                if(directionTileX != 0 && directionTileY != 0) {
                                    if(map.Tile(directionTileX, directionTileY).Room != null) {
                                        // ... if(this tile has a different path set index, then ...
                                        if(pathSetIndexTable[directionTileX - 1, directionTileY - 1] != pathSetIndex)
                                            // ... Add this tile to the collection since it has a neighbor.
                                            results.Add(new TileSideDescription(column, row, direction, directionTileX, directionTileY));
                                    }
                                }
                            } //direction
                        }
                    } //column
                } //row
            } //EnumPathSetWithForeignNeighbor

            /// <summary>Searches through all rooms with a set index and returns back the tiles with doors to foreign sets.</summary>
            public static void EnumRoomSetDoors(MapData map, int[,] roomSetIndexTable, int roomSetIndex, ref List<TileSideDescription> results, bool returnDoorsConnected = true)
            {
                if(results == null) results = new List<TileSideDescription>();

                // First enumerate all the rooms for the room set.
                List<MapLocation> roomsForSet = new List<MapLocation>();
                EnumRoomsByRoomSetIndex(roomSetIndexTable, map.Width, map.Height, roomSetIndex, ref roomsForSet);
                // if(no rooms was found then ... exit this procedure with NOTHING :(.
                if(roomsForSet == null) return;

                // Now go through each room in this set ...
                foreach(MapLocation roomTile in roomsForSet) {
                    // ... Go through each direction ...
                    for(CardinalDirections direction = 0; direction < (CardinalDirections)4; direction++) {
                        // ... if(this direction is valid on this tile then ...
                        if(map.Tile(roomTile.X, roomTile.Y).Room.IsSideOpened(direction)) {
                            // ... Get the tile that is attached to this tile ...
                            int targetTileX, targetTileY;
                            MapHelpers.GetDirectionTile(roomTile.X, roomTile.Y, direction, out targetTileX, out targetTileY, map.Width, map.Height);
                            // ... if(the tile is valid then ...
                            if(!(targetTileX == 0 && targetTileY == 0)) {
                                // ... if(this tile isn't of the same set (not found in the valid rooms collection) then ...
                                if(!MapLocation.ContainsCoord(roomsForSet, targetTileX, targetTileY)) {
                                    // ... if(we don't want to return doors that already has a connection then ...
                                    if(!returnDoorsConnected) {
                                        // ... if(this door indeed does have a connection ... don't add it (just continue.)
                                        if(map.Tile(targetTileX, targetTileY).Room != null) continue;
                                    }

                                    // ... Add this tile location and direction to the output.
                                    results.Add(new TileSideDescription(roomTile.X, roomTile.Y, direction, targetTileX, targetTileY));
                                }
                            }
                        }
                    } //direction
                } //roomTile
            } //EnumRoomSetDoors

#region "Path Set Statistics"
            public class PathSetsStatistics : List<PathSetStatistics>
            {
                public PathSetStatistics Add(int pathSetIndex, int tileLocationX, int tileLocationY)
                {
                    int index = this.Find(pathSetIndex);

                    PathSetStatistics results;
                    if(index <= 0) {
                        results = new PathSetStatistics(pathSetIndex);
                        base.Add(results);
                    } else
                        results = base[index - 1];

                    results.AddTile(tileLocationX, tileLocationY);

                    return results;
                } //Add

                public int Find(int pathSetToFind)
                {
                    if(base.Count > 0) {
                        for(int index = 1; index < base.Count; index++) {
                            if(base[index - 1].PathSetIndex == pathSetToFind) return index;
                        } //index
                    }

                    return 0;
                } //FindIndex
            } //PathSetsStatistics

            /// <summary>Stores the path set index and the count of tiles using this path set.</summary>
            /// <remarks>Using a class because of assignment issues with structures in an array.</remarks>
            public class PathSetStatistics
            {
                public int PathSetIndex = 0;
                public List<MapLocation> Tiles = new List<MapLocation>();

                public PathSetStatistics(int pathSetIndex)
                {
                    this.PathSetIndex = pathSetIndex;
                    this.Tiles.Clear();
                } //New

                public bool AddTile(int locationX, int locationY)
                {
                    if(MapLocation.ContainsCoord(this.Tiles, locationX, locationY)) return false;
                    this.Tiles.Add(new MapLocation(locationX, locationY));
                    return true;
                } //AddTile
            } //PathSetStatistics

            public static PathSetsStatistics ComputePathSetsStats(MapData map, int[,] pathSetTable)
            {
                PathSetsStatistics results = new PathSetsStatistics();

                int pathIndex = 0;
                for(int row = 1; row <= map.Height; row++) {
                    for(int column = 1; column <= map.Width; column++) {
                        pathIndex = pathSetTable[column - 1, row - 1];
                        if(pathIndex > 0) {
                            // ... Add the tile to the location.
                            results.Add(pathIndex, column, row);
                        }
                    } //column
                } //row

                return results;
            } //ComputePathSetsStats

            public static PathSetsStatistics ComputePathSetsStats(MapData map)
            {
                int[,] pathSets; MapHelpers.DeterminePathSetsTable(map, out pathSets);
                return ComputePathSetsStats(map, pathSets);
            } //ComputePathSetsStats
#endregion //Path Set Statistics

            /// <summary>Enumerates all the tiles in a path set that has a door to an empty tile.</summary>
            /// <param name="map">The tile map.</param>
            /// <param name="tiles">The collection of tiles to check.</param>
            /// <returns>Returns a collection of opened tile locations.</returns>
            public static void EnumOpenedTiles(MapData map, List<MapLocation> tiles, ref List<TileSideDescription> results)
            {
                // if(there is no tiles then ... return failure.
                if(tiles == null || tiles.Count == 0) return;
                // Prepare the results variable.
                if(results == null) results = new List<TileSideDescription>();

                // Go through each tile in the path set ...
                foreach(MapLocation tileLocation in tiles) {
                    // ... if(this tile has a room then ...
                    BaseRoomData room = map.Tile(tileLocation.X, tileLocation.Y).Room;
                    if(room != null) {
                        // ... Go through each direction around the tile ...
                        for(CardinalDirections direction = 0; direction < (CardinalDirections)4; direction++) {
                            // ... Check to see if the current tile has this direction, if so then ...
                            if(room.IsSideOpened(direction)) {
                                // ... Get that direction's tile ...
                                int dirTileX, dirTileY;
                                MapHelpers.GetDirectionTile(tileLocation.X, tileLocation.Y, direction, out dirTileX, out dirTileY, map.Width, map.Height);
                                // ... if(that tile is valid then ...
                                if(dirTileX != 0 && dirTileY != 0) {
                                    // ... if(the tile does not have a room attached then ... add this tile to the results.
                                    if(map.Tile(dirTileX, dirTileY).Room == null) results.Add(new TileSideDescription(tileLocation.X, tileLocation.Y, direction, dirTileX, dirTileY));
                                }
                            }
                        } //direction
                    }
                } //tileLocation
            } //EnumOpenedTiles
        } //Enumerations

        /// <summary>Provides helper functions for various routines.</summary>
        public static class MapHelpers
        {
            public static void GetDirectionTile(int column, int row, CardinalDirections direction, out int x, out int y, int mapWidth = -1, int mapHeight = -1)
            {
                x = 0;
                y = 0;

                // First get target locations.
                if(direction == CardinalDirections.North) {
                    x = column;
                    y = (row - 1);
                } else if(direction == CardinalDirections.South) {
                    x = column;
                    y = (row + 1);
                } else if(direction == CardinalDirections.West) {
                    x = (column - 1);
                    y = row;
                } else if(direction == CardinalDirections.East) {
                    x = (column + 1);
                    y = row;
                } else
                    return;

                if(mapWidth > 0 && (x < 1 || x > mapWidth)) {x = 0; y = 0; return; }
                if(mapHeight > 0 && (y < 1 || y > mapHeight)) {x = 0; y = 0; return; }
            } //GetDirectionTile

            /// <summary>Returns the direction opposite from a direction.</summary>
            public static CardinalDirections OppositeCardianDirection(CardinalDirections direction)
            {
                if(direction == CardinalDirections.North)
                    return CardinalDirections.South;
                else if(direction == CardinalDirections.South)
                    return CardinalDirections.North;
                else if(direction == CardinalDirections.West)
                    return CardinalDirections.East;
                else //else if(direction == CardinalDirections.East)
                    return CardinalDirections.West;
            } //OppositeCardianDirection

            public static CardinalDirections GetTileDirection(int column, int row, int targetColumn, int targetRow)
            {
                if(targetColumn < column)
                    return CardinalDirections.West;
                else if(targetColumn > column)
                    return CardinalDirections.East;
                else {
                    if(targetRow < row)
                        return CardinalDirections.North;
                    else if(targetRow > row)
                        return CardinalDirections.South;
                    else
                        return CardinalDirections.North; //dunno what to do here, shouldn't happen through
                }
            } //GetTileDirection

#region "Path Set Table"
            public static void DeterminePathSetsTable(MapData map, out int[,] results)
            {
                results = null;
                // if(there is no tile data then ... exit this function.
                if(map.Width == 0 || map.Height == 0) return;

                // Prepare the results.
                results = new int[map.Width, map.Height];

                // Determine all the path set indexes ...
                int lastPathSetIndex = 0;
                for(int row = 1; row < map.Height; row++) {
                    for(int column = 1; column < map.Width; column++) {
                        UpdateTilePathSet(map, ref results, column, row, ref lastPathSetIndex);
                    } //column
                } //row
            } //DetermineTilePathSetsTable

            private static void UpdateTilePathSet(MapData map, ref int[,] index, int column, int row, ref int lastPathSetIndex)
            {
                int newPathSetIndex = 0;

                // if(this tile is not valid to check then return ...
                if(map.Tile(column, row).Room == null) {
                    index[column - 1, row - 1] = 0;
                    return;
                }

                if(lastPathSetIndex == 0) {
                    lastPathSetIndex = 1;
                    newPathSetIndex  = lastPathSetIndex;
                } else {
                    if(map.IsTilesConnected(column, row, CardinalDirections.North)) {
                        int targetTileX, targetTileY;
                        GetDirectionTile(column, row, CardinalDirections.North, out targetTileX, out targetTileY, map.Width, map.Height);
                        newPathSetIndex = index[targetTileX - 1, targetTileY - 1];
                    }

                    if(map.IsTilesConnected(column, row, CardinalDirections.West)) {
                        int targetTileX, targetTileY;
                        GetDirectionTile(column, row, CardinalDirections.West, out targetTileX, out targetTileY, map.Width, map.Height);
                        if(newPathSetIndex == 0)
                            newPathSetIndex = index[targetTileX - 1, targetTileY - 1];
                        else {
                            if(index[targetTileX - 1, targetTileY - 1] != newPathSetIndex) { 
                                // change the target tile.
                                int countReplaced;
                                ReplaceTableIndexValues(ref index, map.Width, index[targetTileX - 1, targetTileY - 1], newPathSetIndex, 1, row, out countReplaced);
                            }
                        }
                    }

                    // if(no path was found then ...
                    if(newPathSetIndex == 0) {
                        lastPathSetIndex += 1;
                        newPathSetIndex = lastPathSetIndex;
                    }
                }

                index[column - 1, row - 1] = newPathSetIndex;
            } //UpdateTilePathSet

            /// <summary>Removes all tiles that is not part of the biggest path set.</summary>
            public static void RemoveAllSmallPathSets(MapData map)
            {
                Enumerations.PathSetsStatistics totalSets = Enumerations.ComputePathSetsStats(map);
                if(totalSets == null) return;
                if(totalSets.Count <= 1) return;

                // First determine which set has the highest amount of tiles in it.
                int highPathSet = totalSets[0].PathSetIndex;
                int highPathCount = totalSets[0].Tiles.Count;
                foreach(Enumerations.PathSetStatistics pathSet in totalSets) {
                    if(pathSet.Tiles.Count > highPathCount) {
                        highPathSet = pathSet.PathSetIndex;
                        highPathCount = pathSet.Tiles.Count;
                    }
                } //pathSets

                // Finally remove all tiles that is not part of the highest path set ...
                foreach(Enumerations.PathSetStatistics pathSet in totalSets) {
                    if(pathSet.PathSetIndex != highPathSet) RemoveAllTiles(map, pathSet.Tiles);
                } //pathSets
            } //RemoveAllSmallPathSets

            /// <summary>Removes all tiles that is part of a collection.</summary>
            public static void RemoveAllTiles(MapGenerator.MapData map, List<MapLocation> tiles)
            {
                if(tiles == null || tiles.Count == 0) return;
                for(int index = 0; index < tiles.Count; index++) {
                    map.Tile(tiles[index].X, tiles[index].Y).Clear(); //clear the tile's data
                } //index
            } //RemoveAllTiles

            /// <summary>Removes all tiles that is part of a path set.</summary>
            public static void RemoveAllTilesBasedOnPathSet(MapGenerator.MapData map, int pathSetIndex)
            {
                int[,] pathSetsTable;
                DeterminePathSetsTable(map, out pathSetsTable);

                for(int row = 1; row <= map.Height; row++) {
                    for(int column = 1; column <= map.Width; column++) {
                        if(pathSetsTable[column - 1, row - 1] == pathSetIndex) {
                            pathSetsTable[column - 1, row - 1] = 0; //notify that this tile has been checked
                            map.Tile(column, row).Clear(); //clear the tile's data
                        }
                    } //column
                } //row
            } //RemoveAllTilesBasedOnPathSet
#endregion //Path Set Table

#region "Room Set Table"
            public static void DetermineRoomSetsTable(MapData map, out int[,] results)
            {
                results = null;
                // if(there is no tile data then ... exit this function.
                if(map.Width == 0 || map.Height == 0) return;

                // Prepare the results.
                results = new int[map.Width, map.Height];

                // Determine all the room set indexes ...
                int lastPathSetIndex = 0;
                for(int row = 1; row <= map.Height; row++) {
                    for(int column = 1; column <= map.Width; column++) {
                        UpdateTileRoomSet(map, ref results, column, row, ref lastPathSetIndex);
                    } //column
                } //row
            } //UpdateTilesRoomSet

            private static void UpdateTileRoomSet(MapData map, ref int[,] table, int column, int row, ref int lastPathSetIndex)
            {
                int newPathSetIndex = 0;

                // if(this tile is not valid to check then return ...
                if(map.Tile(column, row).Room == null) {
                    table[column - 1, row - 1] = 0;
                    return;
                }

                // Make sure this current tile is a room, if it is not then ... exit
                if(map.Tile(column, row).Room.Type != MapGenerator.RoomTypes.Room) return;

                if(lastPathSetIndex == 0) {
                    lastPathSetIndex = 1;
                    newPathSetIndex = lastPathSetIndex;
                } else {
                    if(map.IsTilesConnected(column, row, CardinalDirections.West)) {
                        int targetTileX, targetTileY;
                        GetDirectionTile(column, row, CardinalDirections.West, out targetTileX, out targetTileY, map.Width, map.Height);
                        MapData.TileManager targetData = map.Tile(targetTileX, targetTileY);
                        if(targetData.Room != null) {
                            if(targetData.Room.Type == MapGenerator.RoomTypes.Room) {
                                newPathSetIndex = table[targetTileX - 1, targetTileY - 1];
                            }
                        }
                    }

                    if(map.IsTilesConnected(column, row, CardinalDirections.North)) {
                        int targetTileX, targetTileY;
                        GetDirectionTile(column, row, CardinalDirections.North, out targetTileX, out targetTileY, map.Width, map.Height);
                        MapData.TileManager targetData = map.Tile(targetTileX, targetTileY);
                        if(targetData.Room != null) {
                            if(targetData.Room.Type == MapGenerator.RoomTypes.Room) {
                                if(newPathSetIndex == 0)
                                    newPathSetIndex = table[targetTileX - 1, targetTileY - 1];
                                else {
                                    if(table[targetTileX - 1, targetTileY - 1] != newPathSetIndex) {
                                        // change the target tile.
                                        int countReplaced;
                                        ReplaceTableIndexValues(ref table, map.Width, table[targetTileX - 1, targetTileY - 1], newPathSetIndex, 1, row, out countReplaced);
                                    }
                                }
                            }
                        }
                    }

                    // if(no path was found then ...
                    if(newPathSetIndex == 0) {
                        lastPathSetIndex += 1;
                        newPathSetIndex = lastPathSetIndex;
                    }
                }

                table[column - 1, row - 1] = newPathSetIndex;
            } //UpdateTileRoomSet
#endregion //Room Set Table

#region "Room Index Table"
            public static void DetermineRoomsTable(MapData map, out int[,] results)
            {
                // if(there is no tile data then ... exit this function.
                results = null;
                if(map.Width == 0 || map.Height == 0) return;

                // Prepare the results.
                results = new int[map.Width, map.Height];

                // Determine all the room indexes ...
                int lastPathSetIndex = 0;
                for(int row = 1; row <= map.Height; row++) {
                    for(int column = 1; column <= map.Width; column++) {
                        UpdateTileRoomIndex(map, ref results, column, row, ref lastPathSetIndex);
                    } //column
                } //row
            } //DetermineTilesRoomIndexTable

            private static void UpdateTileRoomIndex(MapData map, ref int[,] table, int column, int row, ref int lastPathSetIndex)
            {
                table[column - 1, row - 1] = 0;

                // if(this tile is not valid to check then return ...
                if(map.Tile(column, row).Room == null) return;
                if(map.Tile(column, row).Room.Type != MapGenerator.RoomTypes.Room) return;

                lastPathSetIndex += 1;
                table[column - 1, row - 1] = lastPathSetIndex;
            } //UpdateTileRoomIndex
#endregion //Room Index Table

            public static void ReplaceTableIndexValues(ref int[,] table, int mapWidth, int tilePathSet, int replacePathSet, int startRow, int endRow, out int outCountReplaced)
            {
                outCountReplaced = 0;

                for(int row = startRow; row <= endRow; row++) {
                    for(int column = 1; column <= mapWidth; column++) {
                        if(table[column - 1, row - 1] == tilePathSet) {
                            table[column - 1, row - 1] = replacePathSet;
                            outCountReplaced += 1;
                        }
                    } //column
                } //row
            } //ReplaceTableIndexValues

#region "Path Finding"
            public class PathFindingItemsList : List<PathFindingItemsList.ItemManager>
            {
                public ItemManager Add(ItemManager parent, int column, int row, int finalColumn, int finalRow)
                {
                    base.Add(new ItemManager(parent, column, row, finalColumn, finalRow));
                    return base[base.Count - 1];
                } //Add

                public int Find(int column, int row)
                {
                    if(base.Count > 0) {
                        for(int index = 1; index <= base.Count; index++) {
                            if(base[index - 1].Column == column && base[index - 1].Row == row) return index;
                        } //index
                    }

                    return 0;
                } //Find

                public int FindByLowestF() 
                {
                    int result = 0;
                    int lastF = 0;

                    if(base.Count > 0) {
                        for(int index = 1; index < base.Count; index++) {
                            if(result == 0 || base[index - 1].F < lastF) { result = index; lastF = base[index - 1].F; }
                        } //index
                    }

                    return result;
                } //FindByLowestF

                public class ItemManager
                {
                    public int Column = 0;
                    public int Row    = 0;
                    private int mG = 0;
                    private int mH = 0;
                    private int mF = 0;
                    private ItemManager mParent;

                    public ItemManager() {}

                    public ItemManager(ItemManager parent, int column, int row, int finalColumn, int finalRow)
                    {
                        this.mParent = parent;
                        this.Column = column;
                        this.Row = row;
                        if(parent == null)
                            this.mG = 1;
                        else
                            this.mG = (parent.G + 1);
                        this.ComputeH(finalColumn, finalRow);
                        this.ComputeF();
                    } //Constructor

                    public ItemManager(ItemManager clone)
                    {
                        if(clone == null) return;
                        this.Column = clone.Column;
                        this.Row = clone.Row;
                        this.mG = clone.mG;
                        this.mH = clone.mH;
                        this.mF = clone.mF;
                        this.mParent = clone.mParent;
                    } //Constructor

                    public int G
                    {
                        get { return this.mG; }
                        set {
                            this.mG = value;
                            this.ComputeF();
                        }
                    } //G

                    public int H { get { return this.mH; } }

                    public int F { get { return this.mF; } }

                    public ItemManager Parent { get { return this.mParent; } }

                    private void ComputeH(int finalColumn, int finalRow)
                    {
                        //this.mH = (System.Math.Abs(this.Column - finalColumn) + Math.Abs(this.Row - finalRow));
                        this.mH = System.Convert.ToInt32(System.Math.Sqrt((this.Column - finalColumn) * (this.Column - finalColumn) + (this.Row - finalRow) * (this.Row - finalRow)));
                    } //ComputeH

                    private void ComputeF()
                    {
                        this.mF = (this.mG + this.mH);
                    } //ComputeF
                } //ItemManager
            } //PathFindingItemsList

            /// <summary>Creates a search from outside until a path can be created to the target cell. if(the target cell is a room, it will look for a opened entrance.</summary>
            /// <remarks>There is a big bug in here right now, if the path cannot be reach...
            ///          * It will hang.
            ///          * if(it does end, since that last tile is not the end tile, everything fails.</remarks>
            public static void CreateOutsidePathRoute(MapData map, int startColumn, int startRow, int endColumn, int endRow, ref List<MapLocation> results, bool ignorePassages = false)
            {
                // Make sure the start location is valid, if not return failure.
                if(!CreateOutsidePathRoute_IsTileValid(map, startColumn, startRow, ignorePassages)) return;

                // Prepare the open and close list.
                // OPENED LIST - Tiles that hasn't been search around yet.
                // CLOSED LIST - Tiles that has already been searched.
                PathFindingItemsList openedList = new PathFindingItemsList();
                PathFindingItemsList closedList = new PathFindingItemsList();
                // Add the start time to the opened list.
                openedList.Add(null, startColumn, startRow, endColumn, endRow);

                do {
                    // ... Find the item with the lowest score ...
                    int currentItemIndex = openedList.FindByLowestF();
                    // ... if(no items was found (empty opened list) then ... exit the loop.
                    if(currentItemIndex == 0) break;
                    // ... Get the found item and remove it from the opened list.
                    PathFindingItemsList.ItemManager currentItem = new PathFindingItemsList.ItemManager(openedList[currentItemIndex - 1]);
                    openedList.RemoveAt(currentItemIndex - 1);

                    // ... Add the item to the closed list ...
                    closedList.Add(currentItem);
                    // ... if(this tile is out destination tile then ... exit the loop.
                    if(currentItem.Column == endColumn && currentItem.Row == endRow) break;

                    // ... Go through each direction around the tile ...
                    for(CardinalDirections direction = 0; direction < (CardinalDirections)4; direction++) {
                        // ... Get the tile that is at this direction ...
                        int directionTileX, directionTileY;
                        GetDirectionTile(currentItem.Column, currentItem.Row, direction, out directionTileX, out directionTileY, map.Width, map.Height);
                        // ... Check to see if this tile is valid ...
                        if(CreateOutsidePathRoute_IsTileValid(map, directionTileX, directionTileY, ignorePassages)) {
                            // ... Make sure the tile isn't already in the closed list ...
                            if(closedList.Find(directionTileX, directionTileY) == 0) {
                                int dirTileIndex = openedList.Find(directionTileX, directionTileY);
                                // ... if(the tile is in the opened list then ...
                                if(dirTileIndex > 0) {
                                    // ... if(this cell haas a lesser G cost then the current item's G then ...
                                    if(openedList[dirTileIndex - 1].G < currentItem.G)
                                        openedList[dirTileIndex - 1].G = (currentItem.G + 1);
                                } else { //... if(the tile is not in the opened list then ...
                                    // ... Add the tile to the opened list.
                                    openedList.Add(currentItem, directionTileX, directionTileY, endColumn, endRow);
                                }
                            }
                        }
                    } //direction
                } while(true);

                // if(there are no items in the closed then ... return with failure.
                if(closedList.Count == 0) return;

                if(results == null) results = new List<MapLocation>();
                int nextIndex = closedList.Find(endColumn, endRow);
                if(nextIndex == 0) {
                    int smallestItem = 0;
                    float smallestDistance = 0.0F;
                    // Let's find a tile close to the end tile that is in the closed list.
                    for(int index = 1; index <= closedList.Count; index++) {
                        float theDistance = ComputeDistance(closedList[index - 1].Column, closedList[index - 1].Row, endColumn, endRow);
                        if(smallestItem == 0 || theDistance < smallestDistance) {
                            smallestItem = index;
                            smallestDistance = theDistance;
                        }
                    } //index
                    if(smallestItem > 0) nextIndex = smallestItem;
                }
                if(nextIndex > 0) {
                    PathFindingItemsList.ItemManager nextItem = closedList[nextIndex - 1];
                    do {
                        // Add this point.
                        results.Insert(0, new MapLocation(nextItem.Column, nextItem.Row));

                        if(nextItem.Parent == null)
                            break;
                        else
                            nextItem = nextItem.Parent;
                    } while(true);
                }
            } //CreateOutsidePathRoute

            private static bool CreateOutsidePathRoute_IsTileValid(MapData map, int column, int row, bool ignorePassages = false)
            {
                if(column == 0 || row == 0) return false;

                if(map.Tile(column, row).Room == null)
                    return true;
                else if(ignorePassages && map.Tile(column, row).Room.Type == MapGenerator.RoomTypes.Passage)
                    return true;

                return false;
            } //CreateOutsidePathRoute_IsTileValid
#endregion //Path Finding

#region "Filter Helpers"
            /// <summary>Computes what filter a tile should use.</summary>
            public static BaseFilterManager ComputeTileRequirementFilter(MapData map, int column, int row, BaseFilterManager baseFilter, bool ignoreBoundries = false, string subSetID = "", bool allowRooms = true, bool allowPassages = true)
            {
                // WEST
                BaseFilterManager westFilter = baseFilter.MakeEmpty();
                if(column <= 1) {
                    if(ignoreBoundries) { westFilter.SetAll(FilterModes.Optional); westFilter.SubsetID = subSetID; }
                } else
                    westFilter = ComputeTileActiveFilter(map, column - 1, row, baseFilter, subSetID);

                // NORTH
                BaseFilterManager northFilter = baseFilter.MakeEmpty();
                if(row == 1) {
                    if(ignoreBoundries) { northFilter.SetAll(FilterModes.Optional); northFilter.SubsetID = subSetID; }
                } else
                    northFilter = ComputeTileActiveFilter(map, column, row - 1, baseFilter, subSetID);

                // EAST
                BaseFilterManager eastFilter = baseFilter.MakeEmpty();
                if(column == map.Width) {
                    if(ignoreBoundries) { eastFilter.SetAll(FilterModes.Optional); eastFilter.SubsetID = subSetID; }
                } else
                    eastFilter = ComputeTileActiveFilter(map, column + 1, row, baseFilter, subSetID);

                // SOUTH
                BaseFilterManager southFilter = baseFilter.MakeEmpty();
                if(row == map.Height) {
                    if(ignoreBoundries) { southFilter.SetAll(FilterModes.Optional); southFilter.SubsetID = subSetID; }
                } else
                    southFilter = ComputeTileActiveFilter(map, column, row + 1, baseFilter, subSetID);

                BaseFilterManager results = baseFilter.MakeEmpty();
                results.FillFromNeighbors(northFilter, southFilter, westFilter, eastFilter, subSetID);
                if(!allowRooms)    results.RoomType = FilterModes.Off;
                if(!allowPassages) results.PassageType = FilterModes.Off;
                return results;
            } //ComputeTileRequirementFilter

            /// <summary>Returns a tile's active tile filter flags.</summary>
            public static BaseFilterManager ComputeTileActiveFilter(MapData map, int column, int row, BaseFilterManager filterType, string subSetID = "")
            {
                BaseRoomData roomData = map.Tile(column, row).Room;
                if(roomData == null) {
                    BaseFilterManager results = filterType.Clone();
                    results.SetAll(FilterModes.Optional);
                    results.SubsetID = subSetID;
                    return results;
                } else
                    return roomData.GetFilterRequirement();
            } //ComputeTileActiveFilter
#endregion //Filter Helpers

#region "Tile Helpers"
            private static System.Random randomPlotRoom     = new System.Random();
            private static System.Random randomFindPos      = new System.Random();
            private static System.Random randomConnectTiles = new System.Random();

            public struct TileConnectionReference
            {
                public int Column;
                public int Row;
                public int RefColumn;
                public int RefRow;

                public TileConnectionReference(int column, int row, int refColumn = 0, int refRow = 0)
                {
                    this.Column    = column;
                    this.Row       = row;
                    this.RefColumn = refColumn;
                    this.RefRow    = refRow;
                } //Constructor
            } //TileConnectionReference

            public static void SetRandomSeed(int seed)
            {
                randomPlotRoom     = new System.Random(seed);
                randomFindPos      = new System.Random(seed);
                randomConnectTiles = new System.Random(seed);
            } //SetRandomSeed

            //TODO: move bitmap functions to an optional outside Partial class.
            /// <summary>Randomly places a random count of rooms around the level.</summary>
            public static void PlotRandomRooms(MapData map, RoomsDataCollection rooms, int count, bool ignoreBoundries = false, string subSetID = "", bool allowRooms = true, bool allowPassages = true, System.Drawing.Bitmap pattern = null, bool ignoreCurrentTiles = true)
            {
                // Now go through each room we want to add ...
                for(int roomIndex = 1; roomIndex <= count; roomIndex++) {
                    // ... Randomly choose a position ...
                    int tilePosX, tilePosY;
                    ComputeRandomRoomPosition(map, out tilePosX, out tilePosY, pattern);
                    // ... Plot the random room.
                    PlotRandomRoom(map, rooms, tilePosX, tilePosY, ignoreBoundries, subSetID, allowRooms, allowPassages, pattern, ignoreCurrentTiles);
                } //roomIndex
            } //PlotRandomRooms

            public static void PlotRandomRoom(MapData map, RoomsDataCollection rooms, int column, int row, bool ignoreBoundries = false, string subSetID = "", bool allowRooms = true, bool allowPassages = true, System.Drawing.Bitmap pattern = null, bool ignoreCurrentTiles = true)
            {
                // Determine what type of room we need to use here.
                BaseFilterManager filter = ComputeTileRequirementFilter(map, column, row, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID, allowRooms, allowPassages);
                // Find a random room we can use.
                PlotRandomRoom(map, rooms, column, row, filter, pattern, ignoreCurrentTiles);
            } //PlotRandomRoom

            public static void PlotRandomRoom(MapData map, RoomsDataCollection rooms, int column, int row, BaseFilterManager filter, System.Drawing.Bitmap pattern = null, bool ignoreCurrentTiles = true)
            {
                // if(this location is not valid to place then ... exit this function.
                if(!IsPatternAreaValid(column, row, map.Width, map.Height, pattern)) return;

                // if(we need to check what's here first then ...
                if(!ignoreCurrentTiles) {
                    // ... if(there is something already here then ... don't let this function continue.
                    if(map.Tile(column, row).Room != null) return;
                }

                // Find a random room we can use.
                map.Tile(column, row).Room = rooms.FindRandomRoom(filter, randomPlotRoom);
            } //PlotRandomRoom

            /// <summary>Returns a valid position that is not next to any other rooms.</summary>
            public static void ComputeRandomRoomPosition(MapData map, out int outX, out int outY, System.Drawing.Bitmap pattern = null)
            {
                outX = 0;
                outY = 0;

                // This method might be slower, but will prevent unneccessary bugs.
                List<MapLocation> goodMap = new List<MapLocation>();
                for(int y = 1; y <= map.Height; y++) {
                    for(int x = 1; x <= map.Width; x++) {
                        // ... if(this area exists in the pattern and is not a room then ... add the point to the good list.
                        if(IsPatternAreaValid(x, y, map.Width, map.Height, pattern) && !map.Tile(x, y).IsRoom()) goodMap.Add(new MapLocation(x, y));
                    } //x
                } //y

                // No good point was found
                if(goodMap.Count > 0) {
                    // ... Return a random location.
                    int randPos = randomFindPos.Next(0, goodMap.Count);
                    outX = goodMap[randPos].X;
                    outY = goodMap[randPos].Y;
                }
            } //ComputeRandomRoomPosition

            /// <summary>Checks to see if a tile can be placed on a certain area.</summary>
            public static bool IsPatternAreaValid(int column, int row, int mapWidth, int mapHeight, System.Drawing.Bitmap pattern)
            {
                // if(no pattern image has been specifed then ...
                if(pattern == null)
                    // ... Return that this is valid.
                    return true;
                else {
                    // ... Determine the the pattern image ...
                    int imageX = System.Convert.ToInt32((column / mapWidth) * pattern.Width);
                    int imageY = System.Convert.ToInt32((row / mapHeight) * pattern.Height);
                    // ... Get the color places on this spot.
                    System.Drawing.Color imageColor = pattern.GetPixel(imageX - 1, imageY - 1);
                    // ... if(this color is black then (valid area) ...
                    return (imageColor.R == 0 && imageColor.G == 0 && imageColor.B == 0);
                }
            } //IsPatternAreaValid

            /// <summary>Computes all the path sets and connects them.</summary>
            /// TODO: cannot connect example when rooms are next to eachother for some reason, example is bottom-right corner of 135205828
            public static void ConnectPathsets(MapData map, RoomsDataCollection rooms, bool ignoreBoundries = false, string subSetID = "")
            {
                // Now we need to determine how the seperate room sets.
                Enumerations.PathSetsStatistics pathSets = Enumerations.ComputePathSetsStats(map);
                // if(there is only one path or less set available then ... exit.
                if(pathSets.Count <= 1) return;

                // Start a loop to connect all the pathsets (except for the last one.)
                for(int pathSetIndex = 0; pathSetIndex < pathSets.Count - 1; pathSetIndex++) {
                    ConnectPathsets(map, rooms, pathSets[pathSetIndex], pathSets[pathSetIndex + 1], ignoreBoundries, subSetID);
                } //pathSetIndex

                //TODO: until it is figured out why some tiles are not connected then remove the following line:
                RemoveAllSmallPathSets(map);

                //do {
                //    // Now we need to determine how the seperate room sets.
                //    Enumerations.PathSetsStatistics pathSets = Enumerations.ComputePathSetsStats(map);
                //    // if(there is only one path or less set available then ... exit.
                //    if(pathSets.Count <= 1) break;
                //
                //    // Connect the two first path sets, and then reloop.
                //    if(!ConnectPathsets(map, rooms, pathSets[0], pathSets[1], ignoreBoundries, subSetID))
                //        RemoveAllTiles(map, pathSets[1].Tiles);
                //} while(true);
            } //ConnectPathsets

            /// <summary>Connects two path sets together.</summary>
            public static bool ConnectPathsets(MapData map, RoomsDataCollection rooms, Enumerations.PathSetStatistics pathSetA, Enumerations.PathSetStatistics pathSetB, bool ignoreBoundries = false, string subSetID = "")
            {
                // Get the tile lists we're going to match.
                List<TileConnectionReference> tilesA = new List<TileConnectionReference>();
                List<TileConnectionReference> tilesB = new List<TileConnectionReference>();
                DetermineTilesToConnectTo(map, pathSetA.Tiles, ref tilesA);
                DetermineTilesToConnectTo(map, pathSetB.Tiles, ref tilesB);

                // Find the tiles of these two sets which are closer.
                List<MapLocation> foundTiles = new List<MapLocation>();
                GetCloserTiles(tilesA, tilesB, ref foundTiles);
                int foundIndex = 1;
                // if(there was multiple tiles found then ...
                if(foundTiles != null) {
                    if(foundTiles.Count > 1) {
                        // ... Randomly select one from the list.
                        foundIndex = randomConnectTiles.Next(0, foundTiles.Count + 1);
                        if(foundIndex < 1) foundIndex = 1;
                        if(foundIndex > foundTiles.Count) foundIndex = foundTiles.Count;
                    }

                    // Determine the points.
                    int tileAIndex = foundTiles[foundIndex - 1].X;
                    TileConnectionReference tileALocation = tilesA[tileAIndex - 1];
                    int tileBIndex = foundTiles[foundIndex - 1].Y;
                    TileConnectionReference tileBLocation = tilesB[tileBIndex - 1];

                    if(HasPath(map, tileALocation.Column, tileALocation.Row, tileBLocation.Column, tileBLocation.Row)) {
                        // Create a connection for each destinations.
                        map.MakeOneSideConnection(tileALocation.RefColumn, tileALocation.RefRow, GetTileDirection(tileALocation.RefColumn, tileALocation.RefRow, tileALocation.Column, tileALocation.Row), rooms, ignoreBoundries, subSetID, true, true);
                        map.MakeOneSideConnection(tileBLocation.RefColumn, tileBLocation.RefRow, GetTileDirection(tileBLocation.RefColumn, tileBLocation.RefRow, tileBLocation.Column, tileBLocation.Row), rooms, ignoreBoundries, subSetID, true, true);

                        // Now let's connect these two points together.
                        return PlotPath(map, rooms, tileALocation, tileBLocation, ignoreBoundries, subSetID);
                    }
                }

                return false;
            } //ConnectPathsets

            private static void DetermineTilesToConnectTo(MapData map, List<MapLocation> pathSetTiles, ref List<TileConnectionReference> results)
            {
                // Prepare the results list.
                if(results == null) results = new List<TileConnectionReference>();

                // Get which of the tiles are opened in the path set.
                List<Enumerations.TileSideDescription> openedTiles = new List<Enumerations.TileSideDescription>();
                Enumerations.EnumOpenedTiles(map, pathSetTiles, ref openedTiles);

                // if(there are no opened tiles then ...
                if(openedTiles == null) {
                    // Go through each tile in the path set ...
                    foreach(MapLocation tileLocation in pathSetTiles) {
                        // ... if(there is valid data here then (should always be valid) ...
                        if(map.Tile(tileLocation.X, tileLocation.Y).Room != null) {
                            // ... if(this tile's room type is a room then ...
                            if(map.Tile(tileLocation.X, tileLocation.Y).Room.Type == RoomTypes.Room) {
                                // ... Go through each direction around the tile ...
                                for(CardinalDirections direction = 0; direction < (CardinalDirections)4; direction++) {
                                    if(!map.Tile(tileLocation.X, tileLocation.Y).Permanant || (map.Tile(tileLocation.X, tileLocation.Y).Permanant && map.Tile(tileLocation.X, tileLocation.Y).Room.IsSideOpened(direction))) {
                                        // ... Get the tile associated with the direction from the tile ...
                                        int dirTileX, dirTileY;
                                        GetDirectionTile(tileLocation.X, tileLocation.Y, direction, out dirTileX, out dirTileY, map.Width, map.Height);
                                        // ... if(this tile is valid then ...
                                        if(dirTileX != 0 && dirTileY != 0) {
                                            // ... if(this directional tile is empty then ...
                                            if(map.Tile(dirTileX, dirTileY).Room == null)
                                                // ... Add this directional tile to the out list.
                                                results.Add(new TileConnectionReference(dirTileX, dirTileY, tileLocation.X, tileLocation.Y));
                                            else {
                                                // We will allow passage through another passage.
                                                if(map.Tile(dirTileX, dirTileY).Room.Type == MapGenerator.RoomTypes.Passage)
                                                    results.Add(new TileConnectionReference(dirTileX, dirTileY, tileLocation.X, tileLocation.Y));
                                            }
                                        }
                                    }
                                } //direction
                            } else //... if(this tile is not a room then ...
                                // ... Add this tile location to the out list.
                                results.Add(new TileConnectionReference(tileLocation.X, tileLocation.Y)); //gets skipped on CheckReference
                        }
                    } //tileLocation
                } else { //Let's add each open tile to the out list ...
                    // ... Go through each opened tile in the opened list ...
                    foreach(Enumerations.TileSideDescription openedTile in openedTiles) {
                        // ... Add the opened tile to the out list.
                        results.Add(new TileConnectionReference(openedTile.TargetColumn, openedTile.TargetRow, openedTile.Column, openedTile.Row));
                    } //openedTile
                }
            } //DetermineTilesToConnectTo

            /// <summary>Compares two sets of tiles and returns which set of tiles are closer.</summary>
            /// <returns>Returns an array of points where X = tilesA item index and Y = tilesB item index.</returns>
            private static void GetCloserTiles(List<TileConnectionReference> tilesA, List<TileConnectionReference> tilesB, ref List<MapLocation> results)
            {
                if(tilesA == null || tilesA.Count == 0 || tilesB == null || tilesB.Count == 0) return;
                // Prepare the results variable.
                if(results == null) results = new List<MapLocation>();

                // First compute the minimum distance between two points.
                float minDistance = 0.0F;
                foreach(TileConnectionReference tileA in tilesA) {
                    foreach(TileConnectionReference tileB in tilesB) {
                        float theDistance = ComputeDistance(tileA.Column, tileA.Row, tileB.Column, tileB.Row);
                        if(minDistance == 0.0F || theDistance < minDistance) minDistance = theDistance;
                    } //tileB
                } //tileA

                // Now go through each tile again, returning those with the minimum distance (since there may be multiples)
                for(int tileAIndex = 1; tileAIndex <= tilesA.Count; tileAIndex++) {
                    for(int tileBIndex = 1; tileBIndex <= tilesB.Count; tileBIndex++) {
                        float theDistance = ComputeDistance(tilesA[tileAIndex - 1].Column, tilesA[tileAIndex - 1].Row, tilesB[tileBIndex - 1].Column, tilesB[tileBIndex - 1].Row);
                        if(theDistance <= minDistance)
                            results.Add(new MapLocation(tileAIndex, tileBIndex));
                    } //tileBIndex
                } //tileAIndex
            } //GetCloserTiles

            public static bool HasPath(MapData map, int startColumn, int startRow, int endColumn, int endRow)
            {
                List<MapLocation> tileList = new List<MapLocation>();
                CreateOutsidePathRoute(map, startColumn, startRow, endColumn, endRow, ref tileList, true);
                return (tileList != null && tileList.Count > 0);
            } //HasPath

            /// <summary>Generate a path from a start tile to an end tile.</summary>
            public static bool PlotPath(MapData map, RoomsDataCollection rooms, TileConnectionReference start, TileConnectionReference end, bool ignoreBoundries = false, string subSetID = "")
            {
                // if(the tiles are next to each other then ...
                float dist = ComputeDistance(start.Column, start.Row, end.Column, end.Row);
                if(dist == 1.0F) {
                    // ... Just connect them ...
                    CardinalDirections direction = GetTileDirection(start.Column, start.Row, end.Column, end.Row);
                    if(map.Tile(start.Column, start.Row).Room != null && map.Tile(end.Column, end.Row).Room != null)
                        map.MakeConnection(start.Column, start.Row, direction, rooms, false, ignoreBoundries, subSetID, true, true, true);
                    else {
                        if(map.Tile(start.Column, start.Row).Room == null) {
                            BaseFilterManager filter = MapHelpers.ComputeTileRequirementFilter(map, start.Column, start.Row, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID);
                            filter.SetAll(FilterModes.Off, FilterModes.Optional, subSetID);
                            filter.PassageType = FilterModes.On;
                            filter.RoomType = FilterModes.Off;
                            filter.SetConnectionOn(direction);
                            MapHelpers.PlotRandomRoom(map, rooms, start.Column, start.Row, filter);
                        } else
                            map.MakeOneSideConnection(start.Column, start.Row, direction, rooms, ignoreBoundries, subSetID, true, true, true);
                        direction = GetTileDirection(end.Column, end.Row, start.Column, start.Row);
                        if(map.Tile(end.Column, end.Row).Room == null) {
                            BaseFilterManager filter = MapHelpers.ComputeTileRequirementFilter(map, end.Column, end.Row, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID);
                            filter.SetAll(FilterModes.Off, FilterModes.Optional, subSetID);
                            filter.PassageType = FilterModes.On;
                            filter.RoomType = FilterModes.Off;
                            filter.SetConnectionOn(direction);
                            MapHelpers.PlotRandomRoom(map, rooms, end.Column, end.Row, filter);
                        } else
                            map.MakeOneSideConnection(end.Column, end.Row, direction, rooms, ignoreBoundries, subSetID, true, true, true);
                    }
                } else if(dist == 0.0F) {
                    BaseFilterManager filter = MapHelpers.ComputeTileRequirementFilter(map, start.Column, start.Row, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID);
                    filter.SetAll(FilterModes.Off, FilterModes.Optional, subSetID);
                    filter.PassageType = FilterModes.On;
                    filter.RoomType = FilterModes.Off;
                    MapHelpers.PlotRandomRoom(map, rooms, start.Column, start.Row, filter);
                } else {
                    // ... Else create a path between them.
                    // Use path finding to find the pathway to the exit, ignore all existing passages through.
                    List<MapLocation> tileList = new List<MapLocation>();
                    CreateOutsidePathRoute(map, start.Column, start.Row, end.Column, end.Row, ref tileList, true);
                    if(tileList == null || tileList.Count == 0) return false;

                    // Check to see if the last tile ends on the ending tile ...
                    if(!(tileList[tileList.Count - 1].X == end.Column && tileList[tileList.Count - 1].Y == end.Row)) {
                        if(ComputeDistance(end.RefColumn, end.RefRow, tileList[tileList.Count - 1].X, tileList[tileList.Count - 1].Y) <= 1.0F) {
                            if(map.Tile(end.RefColumn, end.RefRow).Room == null) {
                                BaseFilterManager filter = MapHelpers.ComputeTileRequirementFilter(map, end.RefColumn, end.RefRow, rooms.MakeEmptyFilter(), ignoreBoundries, subSetID);
                                filter.SetAll(FilterModes.Off, FilterModes.Optional, subSetID);
                                filter.PassageType = FilterModes.On;
                                filter.RoomType = FilterModes.Off;
                                MapHelpers.PlotRandomRoom(map, rooms, end.RefColumn, end.RefRow, filter);
                            } else
                                map.MakeOneSideConnection(end.RefColumn, end.RefRow, GetTileDirection(end.RefColumn, end.RefRow, tileList[tileList.Count - 1].X, tileList[tileList.Count - 1].Y), rooms, ignoreBoundries, subSetID, true, true, true);
                        }
                    }

                    // Plot the path using the tile list.
                    return map.PlotPath(rooms, tileList, ignoreBoundries, subSetID);
                }

                return false;
            } //PlotPath
#endregion //Tile Helpers

#region "Math"
            public static int ComputeRandomRoomsCount(int mapWidth, int mapHeight, float percentage = 0.1F)
            {
                return System.Convert.ToInt32(System.Math.Max(2, System.Math.Round((mapWidth * mapHeight) * percentage)));
            } //ComputeRandomRoomsCount

            private static float ComputeDistance(float X1, float Y1, float X2, float Y2) 
            {
                return System.Convert.ToSingle(System.Math.Sqrt((X1 - X2) * (X1 - X2) + (Y1 - Y2) * (Y1 - Y2)));
            } //ComputeDistance
#endregion //Math
        } //MapHelpers

        public abstract class BaseGenerator
        {
            public abstract MapData Generate(RoomsDataCollection rooms, int mapWidth, int mapHeight);
        } //BaseGenerator

        public struct MapLocation
        {
            public int X, Y;

            public MapLocation(int x, int y)
            {
                this.X = x;
                this.Y = y;
            } //Constructor

            public static bool ContainsCoord(List<MapLocation> items, int x, int y)
            {
                if(items != null && items.Count > 0) {
                    for(int index = 0; index < items.Count; index++) {
                        if(items[index].X == x && items[index].Y == y) return true;
                    }
                }
                
                return false;
            }
        } //MapLocation
    } //MapGenerator namespace

    public static class NoiseGeneration
    {
        private static System.Random mRandom = new System.Random();

        public static System.Drawing.Bitmap GeneratePattern(int width, int height, int seed, int averageRadius)
        {
            System.Drawing.Bitmap results = new System.Drawing.Bitmap(width, height);

            // First generate some random noise on the bitmap.
            RandomNoise(results, mRandom);
            // Now average the area.
            Average(results, averageRadius);

            // Do some cut-offs
            for(int y = 1; y <= results.Height; y++) {
                for(int x = 1; x <= results.Width; x++) {
                    if(results.GetPixel(x - 1, y - 1).R < 127)
                        results.SetPixel(x - 1, y - 1, System.Drawing.Color.White);
                    else
                        results.SetPixel(x - 1, y - 1, System.Drawing.Color.Black);
                } //x
            } //y

            return results;
        } //GeneratePattern

        private static void RandomNoise(System.Drawing.Bitmap pattern, System.Random random)
        {
            float randValue = 0.0F;
            for(int y = 1; y <= pattern.Height; y++) {
                for(int x = 1; x <= pattern.Width; x++) {
                    randValue = System.Convert.ToSingle(random.NextDouble());
                    pattern.SetPixel(x - 1, y - 1, System.Drawing.Color.FromArgb(System.Convert.ToInt32(randValue * 255), 0, 0));
                } //x
            } //y
        } //RandomNoise

        private static void Average(System.Drawing.Bitmap pattern, int radius)
        {
            for(int y  = 1; y <= pattern.Height; y++) {
                for(int x = 1; x <= pattern.Width; x++) {
                    AveragePoint(pattern, x, y, radius);
                } //x
            } //y
        } //Average

        private static void AveragePoint(System.Drawing.Bitmap pattern, int x, int y, int radius)
        {
            float sum = 0.0f, average = 0.0f, value = 0.0f;

            for(int yOffset = (y - radius); yOffset <= (y + radius); yOffset++) {
                for(int xOffset = (x - radius); xOffset <= (x + radius); xOffset++) {
                    if(xOffset > 0 && xOffset <= pattern.Width && yOffset > 0 && yOffset <= pattern.Height) {
                        value = (pattern.GetPixel(xOffset - 1, yOffset - 1).R / 255);
                        sum += value;
                        average += 1;
                    }
                } //xOffset
            } //yOffset

            int newValue = System.Convert.ToInt32((sum / average) * 255);
            pattern.SetPixel(x - 1, y - 1, System.Drawing.Color.FromArgb(newValue, 0, 0));
        } //GenerateNoisePattern_Average
    } //NoiseGeneration
} //GenerationLib namespace
