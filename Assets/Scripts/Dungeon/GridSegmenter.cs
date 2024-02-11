using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProcDungeon
{
    public class GridSegmenter
    {
        const int HARD_SPLIT_LIMIT = 6;

        private List<RectInt> _Splittables = new List<RectInt>();
        private List<RectInt> _Segments = new List<RectInt>();
        private Dictionary<RectInt, List<RectInt>> _Topology = new Dictionary<RectInt, List<RectInt>>();
        private DungeonLevelSetting _Settings;

        private int[,] Grid;
        private int splitabilityThreshold;

        public List<RectInt> Segments => _Segments;

        void ClearPrevious()
        {
            _Splittables.Clear();
            _Segments.Clear();
            _Topology.Clear();
        }

        public void Segment(ref DungeonLevelSetting settings)
        {
            _Settings = settings;
            splitabilityThreshold = settings.minSegmentLength * 2;
            ClearPrevious();

            var rect = new RectInt(0, 0, settings.gridSizeColumns, settings.gridSizeRows);
            _Splittables.Add(rect);

            while (_Splittables.Count > 0 && _Splittables.Count + _Segments.Count < settings.maxNumberOfSegments)
            {
                int idx = Random.Range(0, _Splittables.Count);
                rect = _Splittables[idx];
                Debug.Log($"Segmenter: Considering splitting {rect}");
                _Splittables.RemoveAt(idx);
                _Topology.Add(rect, new List<RectInt>());

                if (rect.height < HARD_SPLIT_LIMIT)
                {
                    AddWidthSplit(ref rect);
                }
                else if (rect.width < HARD_SPLIT_LIMIT)
                {
                    AddHeightSplit(ref rect);
                }
                else if (Random.Range(0, 1) == 0)
                {
                    AddWidthSplit(ref rect);
                }
                else
                {
                    AddHeightSplit(ref rect);
                }

                Debug.Log($"Segmenter: Final segments: {_Segments.Count}, Splitting: {_Splittables.Count}");
            }

            Debug.Log($"Segmenter: Adding {_Splittables.Count} too large segments because we hit max segments");

            _Segments.AddRange(_Splittables);
            _Splittables.Clear();

            GenerateGrid();
            LogGrid();

        }


        private void GenerateGrid()
        {
            Grid = new int[_Settings.gridSizeRows, _Settings.gridSizeColumns];
            int nSegments = _Segments.Count;

            for (int row = 0; row < _Settings.gridSizeRows; row++)
            {
                for (int col = 0; col < _Settings.gridSizeColumns; col++)
                {
                    var point = new Vector2Int(col, row);
                    bool foundSegment = false;

                    for (int idx = 0; idx < nSegments; idx++)
                    {
                        if (_Segments[idx].Contains(point))
                        {
                            if (Grid[row, col] != 0)
                            {
                                Debug.LogError($"Segmenter: Segment {idx} overlap with {Grid[row, col]} at {point}");
                            }
                            Grid[row, col] = idx + 1;
                            foundSegment = true;
                            break;
                        }
                    }

                    if (!foundSegment)
                    {
                        Debug.LogError($"Segmenter: Point {point} is in no segment");
                        Grid[row, col] = -1;
                    }
                }
            }

        }

        public void LogGrid()
        {
            if (Grid == null)
            {
                Debug.LogError("Segmenter: Grid is not initialized");
                return;
            }

            var output = "Segmenter: Grid:\n";

            int nCols = Grid.GetLength(1);
            for (int row = 0, nRows = Grid.GetLength(0); row < nRows; row++)
            {
                for (int col = 0; col < nCols; col++)
                {
                    output += Convert.ToChar(Grid[row, col] + 64);
                }
                output += "\n";
            }

            Debug.Log(output);
        }


        private int GetSplitSize(int length)
        {
            if (length > splitabilityThreshold)
            {
                Debug.Log($"Segmenter: Splitting {length} between {_Settings.minSegmentLength} and {length - _Settings.minSegmentLength}");
                return Random.Range(
                    _Settings.minSegmentLength,
                    length - _Settings.minSegmentLength
                );
            }
            else
            {
                Debug.Log($"Segmenter: Splitting {length} at {length / 2}");
                return length / 2;
            }

        }

        private void AddWidthSplit(ref RectInt rect)
        {
            Debug.Log($"Segmenter: Split {rect} by Width");
            foreach (RectInt r in SplitWidth(rect))
            {
                _Topology[rect].Add(r);

                if (Random.value < _Settings.underSplitProbability)
                {
                    Debug.Log($"Segmenter: Undersplitting {r} (Width-split)");
                    _Segments.Add(r);
                }
                else if (r.height >= splitabilityThreshold || r.width >= splitabilityThreshold || r.width > HARD_SPLIT_LIMIT && Random.value < _Settings.overSplitProbability)
                {
                    _Splittables.Add(r);
                }
                else
                {

                    _Segments.Add(r);
                }
            }
        }

        private void AddHeightSplit(ref RectInt rect)
        {
            Debug.Log($"Segmenter: Split {rect} by Height");
            foreach (RectInt r in SplitHeight(rect))
            {
                _Topology[rect].Add(r);

                if (Random.value < _Settings.underSplitProbability)
                {
                    Debug.Log($"Segmenter: Undersplitting {r} (Height-split)");
                    _Segments.Add(r);
                }
                else if (r.width >= splitabilityThreshold || r.height >= splitabilityThreshold || r.height > HARD_SPLIT_LIMIT && Random.value < _Settings.overSplitProbability)
                {
                    _Splittables.Add(r);
                }
                else
                {
                    _Segments.Add(r);
                }
            }
        }

        private RectInt RectFromMinMax(Vector2Int min, Vector2Int max)
        {
            return new RectInt(min, max - min);
        }

        private IEnumerable<RectInt> SplitWidth(RectInt rect)
        {
            int split = rect.min.x + GetSplitSize(rect.width);

            yield return RectFromMinMax(rect.min, new Vector2Int(split, rect.max.y));
            yield return RectFromMinMax(new Vector2Int(split, rect.min.y), rect.max);
        }

        private IEnumerable<RectInt> SplitHeight(RectInt rect)
        {
            int split = rect.min.y + GetSplitSize(rect.height);

            yield return RectFromMinMax(rect.min, new Vector2Int(rect.max.x, split));
            yield return RectFromMinMax(new Vector2Int(rect.min.x, split), rect.max);
        }
    }
}