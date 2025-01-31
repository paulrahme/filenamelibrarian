﻿using System.Collections.Generic;

namespace FileLibrarian
{
    public class CommandHandler_Status : CommandHandler
    {
        public override List<string> Commands => new() { "status" };
        public override string Description => "Lists status of files.";

        /// <summary> Executes the command (see base class comment for more details) </summary>
        public override CommandResults Execute(List<string> args, ref List<FileEntry> allFiles, List<CommandData> commandHistory, out string output)
        {
            var uniqueDirs = new List<string>();
            foreach (var file in allFiles)
            {
                string dir = file.FileInfo.Directory.FullName;
                if (!uniqueDirs.Contains(dir))
                    uniqueDirs.Add(dir);
            }

            output = $"Current list contains '{allFiles.Count}' files in '{uniqueDirs.Count}' directories.";
            return CommandResults.Success;
        }
    }
}
