namespace GenerationLib.MapGenerator
{
    public class CaveMapGenerator : BaseGenerator
    {
        public OptionsDescription Options = new OptionsDescription();

        public CaveMapGenerator()
        {
            this.Options.SubsetID = "";
            this.Options.IncludeRooms = true;
            this.Options.RandomRoomsPercentage = 0.1F;
        } //Constructor

        public override MapGenerator.MapData Generate(MapGenerator.RoomsDataCollection rooms, int mapWidth, int mapHeight)
        {
            // Make sure there are rooms and there is map data, if not then ... return failure.
            if(rooms == null || rooms.Count == 0 || mapWidth <= 0 || mapHeight <= 0) return null;

            // Resize the map to what we need.
            MapGenerator.MapData results = new MapGenerator.MapData(mapWidth, mapHeight);

            // Create the instance to the randomizer.
            MapHelpers.SetRandomSeed(this.Options.Seed);

            // (Step 1) Place the initial rooms to start out with.
            MapHelpers.PlotRandomRooms(results, rooms, MapHelpers.ComputeRandomRoomsCount(mapWidth, mapHeight, this.Options.RandomRoomsPercentage), this.Options.IgnoreBoundries, this.Options.SubsetID, this.Options.IncludeRooms, !this.Options.IncludeRooms, this.Options.Pattern, true);
            
            // (Step 2) Create the passages to connect each room together.
			MapHelpers.ConnectPathsets(results, rooms, this.Options.IgnoreBoundries, this.Options.SubsetID);

            // (Step 3) if(there is any left over path sets, remove them.
            //MapHelpers.RemoveAllSmallPathSets(results)

            // Remove the map data.
            return results;
        } //Generate

        public struct OptionsDescription
        {
            /// <summary>Specifies the seed to be used when using randomized numbers.</summary>
            public int Seed;
            /// <summary>Contains a bitmap that will determin a plotting pattern.</summary>
            public System.Drawing.Bitmap Pattern;
            /// <summary>Specifies the subset to use when finding rooms to plot.</summary>
            public string SubsetID;
            /// <summary>Specifies to ignore the boundries as a block.</summary>
            public bool IgnoreBoundries;
            /// <summary>Specifies when finding rooms, if the room type should be included.</summary>
            public bool IncludeRooms;
            /// <summary>Specifies the amount of random rooms to plot.</summary>
            public float RandomRoomsPercentage;

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
                parentEntry.AppendChildEntryValue("Seed",             ref this.Seed, appendMode, 0, 0);
                parentEntry.AppendChildEntryValue("Subset ID",        ref this.SubsetID, appendMode, "", "");
                parentEntry.AppendChildEntryValue("Ignore Boundries", ref this.IgnoreBoundries, appendMode, false, false);
                parentEntry.AppendChildEntryValue("Include Rooms",    ref this.IncludeRooms, appendMode, true, true);
                parentEntry.AppendChildEntryValue("Random Rooms Percentage", ref this.RandomRoomsPercentage, appendMode, 0.1F);

                if(appendMode == XINI.AppendModes.Read) {
                    this.Pattern = null;
                    if(System.IO.File.Exists(patternFile)) this.Pattern = new System.Drawing.Bitmap(patternFile);
                } else if(appendMode == XINI.AppendModes.Save) {
                    if(System.IO.File.Exists(patternFile)) System.IO.File.Delete(patternFile);
                    if(this.Pattern != null) this.Pattern.Save(patternFile);
                }
            } //AppendData
        } //OptionsDescription
    } //CaveMapGenerator
} // GenerationLib.MapGenerator namespace
