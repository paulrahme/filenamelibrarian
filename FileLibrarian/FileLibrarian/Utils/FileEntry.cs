﻿using System.Collections.Generic;
using System.IO;

namespace FileLibrarian
{
    public class FileEntry
    {
        public class CompareResult
        {
            public long SizeCountDiff;
            public int LineCountDiff;
            public int? DifferentLineStart, DifferentLineEnd;

            public string GetSummary()
            {
                if ((SizeCountDiff == 0) && (DifferentLineStart == null) && (DifferentLineEnd == null))
                    return "MATCH - Files identical.";

                if ((DifferentLineStart == null) && (DifferentLineEnd == null))
                    return $"CONTENT MATCH - file sizes differ. Size diff = {SizeCountDiff}, Line diff = {LineCountDiff}";

                return $"DIFFERENT - First different line from start = {DifferentLineStart + 1}, Last different line from end = {DifferentLineEnd + 1}";
            }
        }

        public enum SizeTypes { None, Bytes, Kilo, Mega, Giga };

        public FileInfo FileInfo { get; }
        public int LastContentMatchCount { get; private set; }
        public Dictionary<FileEntry, CompareResult> LastCompareResult { get; } = new Dictionary<FileEntry, CompareResult>();
        public int LastSortPosition { get; set; }

        long _size;
        List<string> _tags = new();
        string[] _content;

        /// <summary> Constructor </summary>
        public FileEntry(string fileName)
        {
            FileInfo = new FileInfo(fileName);
        }

        #region File size

        /// <summary> Returns the file's size by specified type (bytes/KB/MB/etc) </summary>
        public float GetSizeByType(SizeTypes sizeType)
        {
            if (_size == 0)
                _size = FileInfo.Length;

            return sizeType switch
            {
                SizeTypes.Kilo => _size /= 1024,
                SizeTypes.Mega => _size /= 1024 * 1024,
                SizeTypes.Giga => _size /= 1024 * 1024 * 1024,
                _ => _size
            };
        }

        #endregion // File size

        #region Tagging

        public bool HasTag(string tag) => _tags.Contains(tag);

        public void AddTag(string tag)
        {
            if (!HasTag(tag))
                _tags.Add(tag);
        }

        #endregion // Tagging

        #region Comparing content

        /// <summary> Returns the file's content as a string array, caching as necessary </summary>
        string[] CheckGetContent()
        {
            if (_content == null)
            {
                if (!File.Exists(FileInfo.FullName))
                    throw new FileNotFoundException($"Could not open file '{FileInfo.FullName}'");

                _content = File.ReadAllLines(FileInfo.FullName);
            }

            return _content;
        }

        /// <summary> Compares this file's content with another one </summary>
        /// <param name="ignoreEmptyLines"> True (default) to ignore/skip blank lines </param>
        /// <returns> A text summary of match / differences found </returns>
        public string CompareWith(FileEntry otherFileEntry, bool ignoreEmptyLines = true)
        {
            // Ensure both files have read their content
            CheckGetContent();
            string[] otherContent = otherFileEntry.CheckGetContent();

            var compareResult = new CompareResult
            {
                SizeCountDiff = FileInfo.Length - otherFileEntry.FileInfo.Length,
                LineCountDiff = _content.Length - otherContent.Length
            };

            compareResult.DifferentLineStart = CheckMatchingLines(_content, otherContent, fromStart: true, ignoreEmptyLines);

            if (compareResult.DifferentLineStart == null) // Matched all the way through
                compareResult.DifferentLineEnd = null;
            else // Also compare from the end working back
                compareResult.DifferentLineEnd = CheckMatchingLines(_content, otherContent, fromStart: false, ignoreEmptyLines);

            LastCompareResult[otherFileEntry] = compareResult;

            return compareResult.GetSummary();
        }

        static int? CheckMatchingLines(string[] thisContent, string[] otherContent, bool fromStart, bool ignoreEmptyLines)
        {
            int thisLineIdx, thisEndIdx, otherLineIdx, otherEndIdx, direction;
            if (fromStart)
            {
                direction = 1;
                thisLineIdx = otherLineIdx = 0;
                thisEndIdx = thisContent.Length - 1;
                otherEndIdx = otherContent.Length - 1;
            }
            else
            {
                direction = -1;
                thisLineIdx = thisContent.Length - 1;
                otherLineIdx = otherContent.Length - 1;
                thisEndIdx = otherEndIdx = 0;
            }

            bool matching = true;
            while (matching && (thisLineIdx != thisEndIdx) && (otherLineIdx != otherEndIdx))
            {
                if (ignoreEmptyLines)
                {
                    while (string.IsNullOrEmpty(thisContent[thisLineIdx]) && (thisLineIdx != thisEndIdx))
                        thisLineIdx += direction;

                    while (string.IsNullOrEmpty(otherContent[otherLineIdx]) && (otherLineIdx != otherEndIdx))
                        otherLineIdx += direction;
                }

                // If neither file reached the end yet, compare the lines
                if ((thisLineIdx != thisEndIdx) && (otherLineIdx != otherEndIdx) && (thisContent[thisLineIdx] != otherContent[otherLineIdx]))
                    matching = false;

                // If still matching, advance both the lines
                if (matching)
                {
                    if (thisLineIdx != thisEndIdx)
                        thisLineIdx += direction;

                    if (otherLineIdx != otherEndIdx)
                        otherLineIdx += direction;
                }
            }

            return (matching ? null : thisLineIdx);
        }

        #endregion // Comparing content

        #region Filtering

        public bool DoesFilenameContain(string substring, bool ignoreCase = false)
        {
            return FileInfo.Name.Contains(substring, ignoreCase ? System.StringComparison.CurrentCultureIgnoreCase : System.StringComparison.CurrentCulture);
        }

        public bool DoesFileContentContain(string substring, bool ignoreCase = false)
        {
            CheckGetContent();

            var culture = ignoreCase ? System.StringComparison.CurrentCultureIgnoreCase : System.StringComparison.CurrentCulture;
            int count = _content.Length;
            for (int i = 0; i < count; ++i)
            {
                if (_content[i].Contains(substring, culture))
                    return true;
            }

            return false;
        }

        #endregion // Filtering

        #region Save/Load serialization

        [System.Serializable]
        public class SaveData
        {
            public string FileName = null;
            public List<string> Tags = new();
            public string[] Content;
        }

        /// <summary> Creates & returns SaveData for this FileEntry </summary>
        public SaveData CreateSaveData()
        {
            return new SaveData
            {
                FileName = FileInfo.FullName,
                Tags = _tags,
                Content = _content
            };
        }

        /// <summary> Creates & returns a FileEntry from SaveData </summary>
        public static FileEntry CreateFromSaveData(SaveData saveData)
        {
            return new FileEntry(saveData.FileName)
            {
                _tags = saveData.Tags,
                _content = saveData.Content
            };
        }

        #endregion // Save/Load serialization
    }
}
