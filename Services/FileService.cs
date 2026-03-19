using GraphicalFileHasher.Exceptions;
using GraphicalFileHasher.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GraphicalFileHasher.Services;

public class FileService
{
    public static LinkedList<FileHasher> AddFilesFromPath(string? text, bool? recursiveFlag)
    {
        var returnList = new LinkedList<FileHasher>();

        // let's check if the path is correct or not
        if (text is null || text == string.Empty)
        {
            throw new InvalidPathException(InvalidPathException.Cause.INVALID_PATH);
        }

        string originPath = text;

        // in case the path is a file we don't even check the recursive flag because it wouldn't be useful
        if (File.Exists(originPath))
        {
            returnList.AddLast(new FileHasher(originPath));
            return returnList;
        }

        // we have to use the explicit check because it may be null
        bool recursive = recursiveFlag == true;

        // let's check if the path is a directory
        if (Directory.Exists(originPath))
        {
            if (!recursive)
            {
                throw new InvalidPathException(InvalidPathException.Cause.NO_RECURSIVE_FLAG);
            }

            // we are in the most generic case, the one in which the user passes a whole directory in recursive
            // mode, so let's explore it recursively
            ExploreRecursively(returnList, originPath);
            return returnList;
        }


        throw new InvalidPathException(InvalidPathException.Cause.NOT_EXISTS);
    }

    /// <summary>
    /// Explores a path recursively adding the found files to the provided list
    /// </summary>
    /// <param name="fileHasherList">is a LinkedList which will contain the paths</param>
    /// <param name="currentPath"></param>
    private static void ExploreRecursively(LinkedList<FileHasher> fileHasherList, string currentPath)
    {
        if (File.Exists(currentPath))
        {
            fileHasherList.AddLast(new FileHasher(currentPath));
            return;
        }

        // let's launch the recursion at the files collocated at this level
        try
        {
            foreach (string file in Directory.GetFiles(currentPath))
                ExploreRecursively(fileHasherList, file);

            // let's launch a deeper recursion, if needed
            foreach (string file in Directory.GetDirectories(currentPath))
                ExploreRecursively(fileHasherList, file);
        } catch(UnauthorizedAccessException _) { }
    }
}
