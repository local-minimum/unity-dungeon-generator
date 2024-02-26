using ProcDungeon;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace ProcDungeon
{
    public class VoronoiDistanceGrid
    {
        int[,] Distances;
        public int MaxDistance { get; private set; }

        public VoronoiDistanceGrid(DungeonLevelSetting settings, System.Func<Vector2Int, bool> IsIn)
        {
            Distances = new int[settings.gridSizeRows, settings.gridSizeColumns];

            var InGroup = new List<Vector2Int>();
            var OutGroup = new List<Vector2Int>();
            for (int row = 0; row < settings.gridSizeRows; row++)
            {
                for (int col = 0; col < settings.gridSizeColumns; col++)
                {
                    var coords = new Vector2Int(col, row);
                    
                    if (IsIn(coords))
                    {
                        InGroup.Add(coords);
                    }
                    else
                    {
                        OutGroup.Add(coords);
                    }
                }
            }

            int distance = 0;
            while (OutGroup.Count > 0)
            {
                distance++;
                var NextGroup = new List<Vector2Int>();

                for (int idx = 0, nOut = OutGroup.Count; idx < nOut; idx++)
                {
                    var coords = OutGroup[idx];
                    if (InGroup.Any(other => coords.ChebyshevDistance(other) == 1))
                    {
                        Distances[coords.y, coords.x] = distance;
                        NextGroup.Add(coords);
                    }
                }

                InGroup = NextGroup;
                OutGroup = OutGroup.Where(c => !InGroup.Contains(c)).ToList();
            }

            MaxDistance = distance;
        }

        public int this[Vector2Int point]
        {
            get => Distances[point.y, point.x];
        }

        public IEnumerable<Vector2Int> Coordinates(int distance)
        {
            for (int row = 0; row < Distances.GetLength(0); row++)
            {
                for (int col = 0; col < Distances.GetLength(1); col++)
                {
                    if (Distances[row, col] == distance)
                    {
                        yield return new Vector2Int(col, row);
                    }
                }
            }
        }

        const string DISTANCEENCODING = "0123456789ABCDEFGH*";

        public override string ToString()
        {
            string output = $"Distance Grid (Max distance {MaxDistance}):\n";
            int lastEncodingIndex = DISTANCEENCODING.Length - 1;
            for (int row = 0; row < Distances.GetLength(0); row++)
            {
                for (int col = 0;  col < Distances.GetLength(1); col++)
                {
                    var distance = Distances[row, col];
                    if (distance >= DISTANCEENCODING.Length)
                    {
                        output += DISTANCEENCODING[lastEncodingIndex];
                    } else
                    {
                        output += DISTANCEENCODING[distance];
                    }
                }

                output += "\n";
            }

            return output;
        }
    }
}