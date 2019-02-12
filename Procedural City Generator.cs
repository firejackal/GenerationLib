using System.Collections.Generic;
using System.Drawing;

namespace GenerationLib
{
    public class ProceduralCityGenerator
    {
        private const float MaxCityCountMultiplier = 0.01F;
        // Flags:
        public  int   CitiesCount;
        public  float TileDowntownMinDensity = 0.9F;
        public  float TileOutsideMaxRange    = 0.8F;
        // Variables:
        public  CitiesCollection    Cities;
        public  CityTypesCollection CityTypes;
        private int mWidth, mHeight;
        private TileInformation[,] mTile;
        private System.Random mRandom = new System.Random();

        public ProceduralCityGenerator()
        {
            this.Cities    = new CitiesCollection();
            this.CityTypes = new CityTypesCollection();
            this.DefaultCityTypes();
        } //New

        public void DefaultCityTypes()
        {
            this.CityTypes.Clear();
            // Super Small
            this.CityTypes.Add(2, 4);
            // Small
            this.CityTypes.Add(4, 8);
            // Medium
            this.CityTypes.Add(8, 16);
            // Large
            this.CityTypes.Add(16, 24);
        } //DefaultCityTypes

        public bool Generate(int width, int height, int seed)
        {
            // Make sure all settings are correct, if not then ... fail.
            if(width < 1 || height < 1) return false;

            // Setup the random seed.
            this.mRandom = new System.Random(seed);

            // Setup the level size.
            this.Resize(width, height);

            // Generate the cities.
            this.GenerateCities();

            // Now go through every tile, determing the tile type.
            for(int row = 0; row < this.mHeight; row++) {
                for(int column = 0; column < this.mWidth; column++) {
                    this.DetermineTileType(column, row);
                } //column
            } //row

            return true;
        } //Generate

        private void GenerateCities()
        {
            // Clear all existing cities.
            this.Cities.Clear();

            // Determine how many cities to build on the map.
            int citiesCount = this.CitiesCount;
            if(citiesCount < 1) {
                int maxCities = System.Convert.ToInt32(System.Math.Round((this.mWidth * this.mHeight) * MaxCityCountMultiplier));
                if(maxCities < 1) maxCities = 1;
                citiesCount = this.mRandom.Next(1, maxCities + 1);
                if(citiesCount < 1) return; //don't allow anything to happen if this number doesn't exist.
            }

            // Go through each city we want to add.
            for(int index = 1; index <= citiesCount; index++) {
                // Randomly decide on the type of city.
                int cityType = this.mRandom.Next(0, this.CityTypes.Count);
                int citySize = this.mRandom.Next(this.CityTypes[cityType].MinimumSize, this.CityTypes[cityType].MaximumSize);

                // Determine the city's position.
                Point cityPos = this.ComputeCitySpawnLocation(citySize);

                // Add the city.
                this.Cities.Add(citySize, cityPos.X, cityPos.Y);
            } //index
        } //GenerateCities

        private Point ComputeCitySpawnLocation(int size)
        {
            // Enumerate all available locations for the specified size.
            List<Point> validAreas = new List<Point>(); this.EnumValidCitySpawnLocations(size, ref validAreas, true);
            if(validAreas == null || validAreas.Count == 0) return Point.Empty;
            return validAreas[this.mRandom.Next(0, validAreas.Count)];
        } //ComputeCitySpawnLocation

        private void EnumValidCitySpawnLocations(int size, ref List<Point> results, bool checkCities = true)
        {
            if(results == null) results = new List<Point>();

            for(int row = 0; row < this.mHeight; row++) {
                for(int column = 0; column < this.mWidth; column++) {
                    // Check to make sure this tile is out of the range of any other cities.
                    if(checkCities && this.Cities.Count > 0) {
                        for(int cityIndex = 0; cityIndex < this.Cities.Count; cityIndex++) {
                            int distance = System.Convert.ToInt32(System.Math.Floor(ComputeDistance(column, row, this.Cities[cityIndex].Left, this.Cities[cityIndex].Top)));
                            if(distance > System.Math.Floor(this.Cities[cityIndex].Size * 0.5) + System.Math.Floor(size * 0.5)) {
                                results.Add(new Point(column, row));
                            }
                        } //cityIndex
                    } else {
                        results.Add(new Point(column, row));
                    }
                } //column
            } //row

            if(results.Count == 0 && checkCities) {
                int lastFurtherDistance = 0, lastColumn = 0, lastRow = 0;
                for(int row = 0; row < this.mHeight; row++) {
                    for(int column = 0; column < this.mWidth; column++) {
                        // Check to make sure this tile is out of the range of any other cities.
                        if(this.Cities.Count > 0) {
                            for(int cityIndex = 0; cityIndex < this.Cities.Count; cityIndex++) {
                                int distance = System.Convert.ToInt32(System.Math.Floor(ComputeDistance(column, row, this.Cities[cityIndex].Left, this.Cities[cityIndex].Top)));
                                if(distance > lastFurtherDistance) {
                                    lastFurtherDistance = distance;
                                    lastColumn = column;
                                    lastRow = row;
                                }
                            } //cityIndex
                        }
                    } //column
                } //row

                if(lastFurtherDistance > 0) {
                    results.Add(new Point(lastColumn, lastRow));
                }
            }
        } //ComputeCitySpawnLocation

        private void DetermineTileType(int column, int row)
        {
            // By default the tile has nothing on it.
            this.mTile[column, row].AllOn = false;

            // Check to see if it's it is in any downtown area.
            int totalDistance = 0, totalDistanceCities = 0;
            if(this.Cities.Count > 0) {
                for(int cityIndex = 0; cityIndex < this.Cities.Count; cityIndex++) {
                    int distance = System.Convert.ToInt32(System.Math.Floor(ComputeDistance(column, row, this.Cities[cityIndex].Left, this.Cities[cityIndex].Top)));
                    // If this tile is within a city's downtown range then ...
                    if(distance <= System.Math.Floor(this.Cities[cityIndex].Size * 0.5)) {
                        this.mTile[column, row].RandomlyChoose(this.TileDowntownMinDensity, this.mRandom);
                        return;
                    } else {
                        totalDistanceCities += 1;
                        totalDistance += distance;
                    }
                } //cityIndex
            }

            // The closer the tile is to a downtown area, the more chance it will be full of buildings.
            // The further away the tile is to a downtown area, the more chance it will have no buildings.

            float averageDistanceFromCities = (totalDistance / totalDistanceCities);
            float distanceInfluence = averageDistanceFromCities / (System.Math.Min(this.mWidth, this.mHeight) * this.TileOutsideMaxRange);

            if(this.mRandom.NextDouble() > distanceInfluence) {
                this.mTile[column, row].RandomlyChoose(distanceInfluence, this.mRandom);
                return;
            }
        } //DetermineTileType

        public void Resize(int width, int height)
        {
            if(width < 1 || height < 1) {
                this.mWidth = 0;
                this.mHeight = 0;
                this.mTile = null;
            } else {
                this.mWidth = width;
                this.mHeight = height;
                this.mTile = new TileInformation[this.mWidth, this.mHeight];

                for(int row = 0; row < this.mHeight; row++) {
                    for(int column = 0; column < this.mWidth; column++) {
                        this.mTile[column, row] = new TileInformation();
                    } //column
                } //row
            }
        } //Resize

        /// <summary>Returns the distance between two points.</summary>
        private static float ComputeDistance(float x1, float y1, float x2, float y2)
        {
            return System.Convert.ToSingle(System.Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)));
        } //ComputeDistance

        public int Width {  get { return this.mWidth; } }

        public int Height { get { return this.mHeight; } }

        public TileInformation Tile(int column, int row) { return this.mTile[column - 1, row - 1]; }

        public class CitiesCollection : List<CityInformation>
        {
            public CityInformation Add(int size, int left, int top)
            {
                base.Add(new CityInformation(size, left, top));
                return base[base.Count - 1];
            } //Add
        } //CitiesCollection

        public class CityInformation
        {
            public int Size;
            public int Left;
            public int Top;

            public CityInformation(int size, int left, int top)
            {
                this.Size = size;
                this.Left = left;
                this.Top = top;
            } //New
        } //CityInformation

        public class CityTypesCollection : List<CityTypeInformation>
        {
            public CityTypeInformation Add(int minimumSize, int maximumSize)
            {
                base.Add(new CityTypeInformation(minimumSize, maximumSize));
                return base[base.Count - 1];
            } //Add
        } //CityTypesCollection

        public class CityTypeInformation
        {
            public int MinimumSize;
            public int MaximumSize;

            public CityTypeInformation(int minimumSize, int maximumSize)
            {
                this.MinimumSize = minimumSize;
                this.MaximumSize = maximumSize;
            } //New
        } //CityTypeInformation

        public class TileInformation
        {
            public bool[,] SubSection = new bool[2, 2];

            public int SubColumns { get { return (this.SubSection.GetUpperBound(0) + 1); } }

            public int SubRows { get { return (this.SubSection.GetUpperBound(1) + 1); } }

            public bool AllOn
            {
                get {
                    int columns = this.SubColumns;
                    int rows    = this.SubRows;

                    for(int row = 0; row < rows; row++) {
                        for(int column = 0; column < columns; column++) {
                            if(!this.SubSection[column, row]) return false;
                        } //column     
                    } //row           
                    return true;
                }
                set {
                    int columns = this.SubColumns;
                    int rows    = this.SubRows;

                    for(int row = 0; row < rows; row++) {
                        for(int column = 0; column < columns; column++) {
                            this.SubSection[column, row] = value;
                        } //column
                    } //row
                }
            } //AllOn

            public void RandomlyChoose(float onChance, System.Random random)
            {
                float invOnChance = (1.0F - onChance);
                int columns = this.SubColumns;
                int rows    = this.SubRows;
                if(columns < 1 || rows < 1) return;

                for(int row = 0; row < rows; row++) {
                    for(int column = 0; column < columns; column++) {
                        this.SubSection[column, row] = (random.NextDouble() > invOnChance);
                    } //column
                } //row
            } //RandomlyChoose
        } //TileInformation
    } //ProceduralCityGenerator
} //GenerationLib namespace
