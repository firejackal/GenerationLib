using System;
using System.Collections.Generic;
using System.Drawing;

namespace GenerationLib
{
    public class CaveGenerator
    {
        public const int TilePermanantWall = 0;
        public const int TileWall = 1;
        public const int TileFloor = 2;

        private MapData mMap = new MapData();
        private Random mRandom;

        public CaveGenerator(int columns, int rows, int seed, float initial_open = 0.4F)
        {
            this.mRandom = new Random(seed);
            this.mMap.Resize(columns, rows);
            this.GenerateInitialMap(initial_open);
        } //New constructor

        public void GenerateMap(bool joinRooms)
        {
            for (int row = 2; row < this.mMap.Rows; row++) {
                for (int column = 2; column < this.mMap.Columns; column++) {
                    int wall_count = this.AdjWallCount(column, row);

                    if (this.mMap.GetTile(column, row) == TileFloor && wall_count > 5) {
                        this.mMap.SetTile(column, row, TileWall);
                    } else if (this.mMap.GetTile(column, row) != TileFloor && wall_count < 4) {
                        this.mMap.SetTile(column, row, TileFloor);
                    }
                } //column
            } //row

            if (joinRooms) this.JoinRooms();
        } //GenerateMap

        /// <summary>Make all border squares walls</summary>
        private void SetBorder()
        {
            for (int j = 1; j <= this.mMap.Rows; j++) {
                this.mMap.SetTile(1, j, TilePermanantWall);
                this.mMap.SetTile(this.mMap.Columns, j, TilePermanantWall);
            } //j

            for (int j = 1; j <= this.mMap.Columns; j++) {
                this.mMap.SetTile(j, 1, TilePermanantWall);
                this.mMap.SetTile(j, this.mMap.Rows, TilePermanantWall);
            } //j
        } //SetBorder

        private void GenerateInitialMap(float initial_open)
        {
            for (int row = 1; row <= this.mMap.Rows; row++) {
                for (int column = 1; column <= this.mMap.Columns; column++) {
                    this.mMap.SetTile(column, row, TileWall);
                } //column
            } //row

            int open_count = (int)((this.mMap.Rows * this.mMap.Columns) * initial_open);
            this.SetBorder();

            while (open_count > 0) {
                int rand_r = 1 + this.mRandom.Next(1, this.mMap.Rows - 1);
                int rand_c = 1 + this.mRandom.Next(1, this.mMap.Columns - 1);

                if (this.mMap.GetTile(rand_c, rand_r) == TileWall) {
                    this.mMap.SetTile(rand_c, rand_r, TileFloor);
                    open_count -= 1;
                }
            }
        } //GenerateInitialMap

        private int AdjWallCount(int startColumn, int startRow)
        {
            int count = 0;

            for (int row = -1; row <= 1; row++) {
                for (int column = -1; column <= 1; column++) {
                    if (this.mMap.GetTile(startColumn + column, startRow + row) != TileFloor && row != 0 && column != 0) {
                        count += 1;
                    }
                } //column
            } //row

            return count;
        } //AdjWallCount

        public MapData Map { get { return this.mMap; } }

        #region "Room Joining"
        private void JoinRooms()
        {
            // Step 1, Determine Rooms
            int[,] roomArray = this.JoinRooms_DetermineRoomsTable();

            // Step 2, Enumerate Rooms
            List<JoinRoomDesc> rooms = JoinRooms_EnumRooms(roomArray, this.mMap.Columns, this.mMap.Rows);

            // Step 3, Connect Rooms
            if (rooms.Count > 1) {
                do {
                    this.JoinRooms_ConnectRooms(rooms[0].Tiles, rooms[1].Tiles);
                    rooms[0].MergeTiles(rooms[1].Tiles);
                    rooms.RemoveAt(1);
                } while (rooms.Count > 1);
            }
        } //JoinRooms

        private int[,] JoinRooms_DetermineRoomsTable()
        {
            int[,] results = new int[this.Map.Columns, this.Map.Rows];

            int lastRoomIndex = 0;
            for (int row = 1; row <= this.Map.Rows; row++)
            {
                for (int column = 1; column <= this.Map.Columns; column++)
                {
                    this.JoinRooms_UpdateTileRoom(ref results, column, row, ref lastRoomIndex);
                } //column
            } //row

            return results;
        } //JoinRooms_DetermineRoomsTable

        private void JoinRooms_UpdateTileRoom(ref int[,] roomTable, int column, int row, ref int lastRoomIndex)
        {
            if (this.mMap.GetTile(column, row) == TileFloor) {
                int newRoomIndex = 0;

                if (lastRoomIndex == 0) {
                    lastRoomIndex = 1;
                    newRoomIndex = lastRoomIndex;
                } else {
                    if (this.JoinRooms_IsTilesConnected(column, row, column, row - 1)) {
                        newRoomIndex = roomTable[column - 1, (row - 1) - 1];
                    }

                    if (this.JoinRooms_IsTilesConnected(column, row, column - 1, row)) {
                        if (newRoomIndex == 0) {
                            newRoomIndex = roomTable[(column - 1) - 1, row - 1];
                        } else {
                            if (roomTable[(column - 1) - 1, row - 1] != newRoomIndex) {
                                // change the target tile.
                                JoinRooms_ReplaceTableIndexValues(ref roomTable, this.mMap.Columns, 1, row, roomTable[(column - 1) - 1, row - 1], newRoomIndex);
                            }
                        }
                    }

                    // If no path was found then ...
                    if (newRoomIndex == 0) {
                        lastRoomIndex += 1;
                        newRoomIndex = lastRoomIndex;
                    }
                }

                roomTable[column - 1, row - 1] = newRoomIndex;
            } else {
                roomTable[column - 1, row - 1] = 0;
            }
        } //JoinRooms_UpdateTileRoom

        private bool JoinRooms_IsTilesConnected(int thisColumn, int thisRow, int targetColumn, int targetRow)
        {
            if (thisRow < 1 || thisRow > this.mMap.Rows || thisColumn < 1 || thisColumn > this.mMap.Columns) return false;
            if (targetRow < 1 || targetRow > this.mMap.Rows || targetColumn < 1 || targetColumn > this.mMap.Columns) return false;
            if (this.mMap.GetTile(targetColumn, targetRow) != TileFloor) return false;
            return true;
        } //JoinRooms_IsTilesConnected

        private static void JoinRooms_ReplaceTableIndexValues(ref int[,] table, int mapWidth, int startRow, int endRow, int oldIndex, int newIndex)
        {
            for (int row = (startRow - 1); row < endRow; row++) {
                for (int column = 0; column < mapWidth; column++) {
                    if (table[column, row] == oldIndex) table[column, row] = newIndex;
                } //column
            } //row
        } //JoinRooms_ReplaceTableIndexValues

        private static List<JoinRoomDesc> JoinRooms_EnumRooms(int[,] map, int columns, int rows)
        {
            List<JoinRoomDesc> results = new List<JoinRoomDesc>();

            int roomIndex, foundIndex;
            for (int row = 0; row < rows; row++) {
                for (int column = 0; column < columns; column++) {
                    roomIndex = map[column, row];
                    if (roomIndex != 0)
                    {
                        foundIndex = JoinRoomDesc.FindByRoomIndex(roomIndex, results);
                        if (foundIndex < 0)
                        {
                            results.Add(new JoinRoomDesc(roomIndex));
                            foundIndex = (results.Count - 1);
                        }

                        results[foundIndex].Tiles.Add(new Point(column, row));
                    }
                } //column
            } //row
            
            return results;
        } //JoinRooms_EnumRooms

        private void JoinRooms_ConnectRooms(List<Point> roomsA, List<Point> roomsB)
        {            // Step 1, Find Closest Tiles
            int closestRoomA = 0, closestRoomB  = 0;
            float closestDistance = 0.0f, currentDistance = 0.0f;
            for(int roomIndexA = 0; roomIndexA < roomsA.Count; roomIndexA++) {
                for (int roomIndexB = 0; roomIndexB < roomsB.Count; roomIndexB++) {
                    currentDistance = ComputeDistance(roomsA[roomIndexA].X, roomsA[roomIndexA].Y, roomsB[roomIndexB].X, roomsB[roomIndexB].Y);

                    if((closestRoomA == 0 && closestRoomB == 0) || currentDistance < closestDistance) { 
                        closestDistance = currentDistance;
                        closestRoomA = roomIndexA;
                        closestRoomB = roomIndexB;
                    }
                } //roomIndexB
            } //roomIndexA

            // Step 1, Create Path Between Tiles
            this.JoinRooms_CreatePath(roomsA[closestRoomA].X, roomsA[closestRoomA].Y, roomsB[closestRoomB].X, roomsB[closestRoomB].Y);
        } //JoinRooms_ConnectRooms

        private void JoinRooms_CreatePath(int startX, int startY, int endX, int endY)
        {
            int currentX = startX;
            int currentY = startY;
            int offsetX = 0, offsetY = 0;
            float distA = 0.0f, distB = 0.0f;

            do
            {
                // Set tile to FLOOR if set to WALL
                if (this.mMap.GetTile(1 + currentX, 1 + currentY) == TileWall) this.mMap.SetTile(1 + currentX, 1 + currentY, TileFloor);
                // If at end then stop.
                if (currentX == endX && currentY == endY) break;

                // Determine offsets
                offsetX = 0;
                if (endX < currentX) {
                    offsetX -= 1;
                } else if (endX > currentX) {
                    offsetX = 1;
                }
                offsetY = 0;
                if (endY < currentY) {
                    offsetY -= 1;
                } else if (endY > currentY) {
                    offsetY = 1;
                }

                // Determine next tile
                if (offsetX != 0 && offsetY != 0) {
                    // Determine closest
                    distA = ComputeDistance(currentX, currentY, currentX + offsetX, currentY);
                    distB = ComputeDistance(currentX, currentY, currentX, currentY + offsetY);
                    if (distA <= distB) {
                        currentX += offsetX;
                    } else {
                        currentY += offsetY;
                    }
                } else if (offsetX != 0 && offsetY == 0) {
                    currentX += offsetX;
                } else if (offsetX == 0 && offsetY != 0) {
                    currentY += offsetY;
                }
            } while (true);
        } //JoinRooms_CreatePath

        private struct JoinRoomDesc
        {
            public int RoomIndex;
            public List<Point> Tiles;

            public JoinRoomDesc(int roomIndex)
            {
                this.RoomIndex = roomIndex;
                this.Tiles = new List<Point>();
            } //New constructor

            public void MergeTiles(List<Point> a)
            {
                if (a != null && a.Count > 0) {
                    for (int index = 0; index < a.Count; index++) {
                        this.Tiles.Add(a[index]);
                    } //index
                }
            } //MergeTiles

            public static int FindByRoomIndex(int roomIndex, List<JoinRoomDesc> items)
            {
                if (items != null && items.Count > 0) {
                    for (int index = 0; index < items.Count; index++) {
                        if (items[index].RoomIndex == roomIndex) return index;
                    } //index
                }

                return -1;
            } //FindByRoomIndex
        } //JoinRoomDesc

        private static float ComputeDistance(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        } //ComputeDistance
        #endregion 'Room Joining

        public class MapData
        {
            private int mRows, mColumns;
            private int[,] mMap;

            public void Resize(int columns, int rows)
            {
                if (columns <= 0 || rows <= 0) {
                    this.mColumns = 0;
                    this.mRows = 0;
                    this.mMap = null;
                } else {
                    this.mColumns = columns;
                    this.mRows = rows;
                    this.mMap = new int[columns, rows];
                }
            } //Resize

            public int Columns { get { return this.mColumns; } }

            public int Rows { get { return this.mRows; } }

            public int GetTile(int column, int row)
            {
                return this.mMap[column - 1, row - 1];
            } //Tile

            public void SetTile(int column, int row, int value)
            {
                this.mMap[column - 1, row - 1] = value;
            } //Tile
        } //MapData class
    } // CaveGenerator class
} //GenerationLib namespace
