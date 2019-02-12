using System.Drawing; //for pattern support

namespace GenerationLib.MapGenerator
{
    public class MazeMapGenerator : BaseGenerator
    {
        public enum UnconnectedTilesActions
        {
            /// <summary>Specifies to leave all remaining tiles alone.</summary>
            LeaveAlone,
            /// <summary>Specifies to connect all path sets as one.</summary>
            ConnectAll,
            /// <summary>Specifies that only the path with the most rooms is kept.</summary>
            RemoveSmallerSets
        } //UnconnectedTilesActions

        public OptionsDescription Options = new OptionsDescription();

        public MazeMapGenerator()
        {
            this.Options.SubsetID = "";
            this.Options.IncludeRooms = true;
            this.Options.IncludePassages = true;
            this.Options.UnconnectedTilesAction = UnconnectedTilesActions.ConnectAll;
        } //Constructor

        public override MapGenerator.MapData Generate(MapGenerator.RoomsDataCollection rooms, int mapWidth, int mapHeight)
        {
            // Make sure there is rooms and map data, if not then ... exit.
            if(rooms == null || rooms.Count == 0 || mapWidth <= 0 || mapHeight <= 0) return null;

            // Clear and resize the map.
            MapGenerator.MapData results = new MapGenerator.MapData(mapWidth, mapHeight);

            // Create the instance to the randomizer.
            MapHelpers.SetRandomSeed(this.Options.Seed);

            // (Step 1) Fill the map with random matching tiles.
            FillWithRandomTiles(results, rooms, this.Options.IgnoreBoundries, this.Options.SubsetID, this.Options.IncludeRooms, this.Options.IncludePassages, this.Options.Pattern);

            // (Step 2) Decide what to do with the unconnected tiles ...
            if(this.Options.UnconnectedTilesAction == UnconnectedTilesActions.ConnectAll)
                // ... Connect all unconnected tiles.
                MapHelpers.ConnectPathsets(results, rooms, this.Options.IgnoreBoundries, this.Options.SubsetID);
            else if(this.Options.UnconnectedTilesAction == UnconnectedTilesActions.RemoveSmallerSets)
                // ... Remove all unconnected tiles.
                MapHelpers.RemoveAllSmallPathSets(results);

            // Remove the map data.
            return results;
        } //Generate

        private static void FillWithRandomTiles(MapGenerator.MapData map, MapGenerator.RoomsDataCollection rooms, bool ignoreBoundries = false, string subSetID = "", bool allowRooms = true, bool allowPassages = true, Bitmap pattern = null)
        {
            for(int row = 1; row <= map.Height; row++) {
                for(int column = 1; column <= map.Width; column++) {
                    MapHelpers.PlotRandomRoom(map, rooms, column, row, ignoreBoundries, subSetID, allowRooms, allowPassages, pattern, false);
                } //column
            } //row
        } //FillWithRandomTiles

        public struct OptionsDescription
        {
            /// <summary>Specifies the seed to be used when using randomized numbers.</summary>
            public int Seed;
            /// <summary>Contains a bitmap that will determin a plotting pattern.</summary>
            public Bitmap Pattern;
            /// <summary>Specifies the subset to use when finding rooms to plot.</summary>
            public string SubsetID;
            /// <summary>Specifies to ignore the boundries as a block.</summary>
            public bool IgnoreBoundries;
            /// <summary>Specifies when finding rooms, if the room type should be included.</summary>
            public bool IncludeRooms;
            /// <summary>Specifies when finding rooms, if the passage type should be included.</summary>
            public bool IncludePassages;
            /// <summary>What to do with the path sets that are not connected</summary>
            public UnconnectedTilesActions UnconnectedTilesAction;

            public bool LoadFromFile(string fileName)
            {
                XINI cXINI = new XINI();
                if(!cXINI.LoadFromFile(fileName)) return false;

                XINI.EntryItem parentEntry = cXINI.Root.AppendChildEntry("Options", XINI.AppendModes.Read);
                this.AppendData(parentEntry, XINI.AppendModes.Read, fileName + ".pattern");

                return true;
            } //LoadFromFile

            public bool SaveToFile(string fileName)
            {
                XINI cXINI = new XINI();
                cXINI.Name = "Options";
                cXINI.Version = "1";

                XINI.EntryItem parentEntry = cXINI.Root.AppendChildEntry("Options", XINI.AppendModes.Save);
                this.AppendData(parentEntry, XINI.AppendModes.Save, fileName + ".pattern");

                return cXINI.SaveToFile(fileName);
            } //SaveToFile

            public void AppendData(XINI.EntryItem parentEntry, XINI.AppendModes appendMode, string patternFile)
            {
                parentEntry.AppendChildEntryValue("Seed",                         ref this.Seed, appendMode, 0, 0);
                parentEntry.AppendChildEntryValue("Subset ID",                    ref this.SubsetID, appendMode, "", "");
                parentEntry.AppendChildEntryValue("Ignore Boundries",             ref this.IgnoreBoundries, appendMode, false, false);
                parentEntry.AppendChildEntryValue("Include Rooms",                ref this.IncludeRooms, appendMode, true, true);
                parentEntry.AppendChildEntryValue("Include Passages",             ref this.IncludePassages, appendMode, true, true);
                parentEntry.AppendChildEntryEnumValue<UnconnectedTilesActions>("Unconnected Tiles Action", ref this.UnconnectedTilesAction, appendMode, UnconnectedTilesActions.ConnectAll.ToString());

                if(appendMode == XINI.AppendModes.Read) {
                    this.Pattern = null;
                    if(System.IO.File.Exists(patternFile)) this.Pattern = new Bitmap(patternFile);
                } else if(appendMode == XINI.AppendModes.Save) {
                    if(System.IO.File.Exists(patternFile)) System.IO.File.Delete(patternFile);
                    if(this.Pattern != null) this.Pattern.Save(patternFile);
                }
            } //AppendData
        } //OptionsDescription
    } //MazeMapGenerator
} // GenerationLib.MapGenerator namespace
