using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Jc.MediaImporter.Helpers;

public class Files
{
    internal static string GetChecksum(string file)
    {
        using var stream = File.OpenRead(file);
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(stream);
        return BitConverter.ToString(bytes).Replace("-", "");
    }
    
    public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOpt)
    {   
        try
        {
            var dirFiles = Enumerable.Empty<string>();
            if(searchOpt == SearchOption.AllDirectories)
            {
                dirFiles = Directory.EnumerateDirectories(path)
                    .SelectMany(x => EnumerateFiles(x, searchPattern, searchOpt));
            }
            return dirFiles.Concat(Directory.EnumerateFiles(path, searchPattern));
        }
        catch(UnauthorizedAccessException ex)
        {
            return Enumerable.Empty<string>();
        }
    }
    
    internal static void TraverseTreeParallelForEach(string root, Action<string> fileAction)
    {
        //Count of files traversed and timer for diagnostic output
        int fileCount = 0;
        var sw = Stopwatch.StartNew();

        // Determine whether to parallelize file processing on each folder based on processor count.
        int procCount = Environment.ProcessorCount;

        // Data structure to hold names of subfolders to be examined for files.
        Stack<string> dirs = new Stack<string>();

        if (!Directory.Exists(root))
        {
            throw new ArgumentException(
                "The given root directory doesn't exist.", nameof(root));
        }
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs = { };
            string[] files = { };

            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            // Thrown if we do not have discovery permission on the directory.
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            // Thrown if another process has deleted the directory after we retrieved its name.
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            
            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            foreach (string str in subDirs)
                dirs.Push(str);

            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }

            // Execute in parallel if there are enough files in the directory.
            // Otherwise, execute sequentially.Files are opened and processed
            // synchronously but this could be modified to perform async I/O.
            try
            {
                if (files.Length < procCount)
                {
                    foreach (var file in files)
                    {
                        fileAction(file);
                        fileCount++;
                    }
                }
                else
                {
                    Parallel.ForEach(files, () => 0,
                        (file, loopState, localCount) =>
                        {
                            fileAction(file);
                            return (int)++localCount;
                        },
                        (c) =>
                        {
                            Interlocked.Add(ref fileCount, c);
                        });
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException)
                    {
                        // Here we just output a message and go on.
                        Console.WriteLine(ex.Message);
                        return true;
                    }
                    // Handle other exceptions here if necessary...

                    return false;
                });
            }
        }

        // For diagnostic purposes.
        Console.WriteLine("Processed {0} files in {1} milliseconds", fileCount, sw.ElapsedMilliseconds);
    }
    
    internal static void TraverseTreeParallelForEach(string root, Func<string, (bool SkipFiles, int FileCount)> folderAction, Action<string> fileAction, ref int fileCount, ref int dirCount)
    {
        //Count of files traversed and timer for diagnostic output
        var sw = Stopwatch.StartNew();

        // Determine whether to parallelize file processing on each folder based on processor count.
        int procCount = Environment.ProcessorCount;

        // Data structure to hold names of subfolders to be examined for files.
        Stack<string> dirs = new Stack<string>();

        if (!Directory.Exists(root))
        {
            throw new ArgumentException(
                "The given root directory doesn't exist.", nameof(root));
        }
        dirs.Push(root);
        dirCount++;

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs = { };
            string[] files = { };

            bool skipFiles = false;
            
            try
            {
                (skipFiles, var c) = folderAction(currentDir);
                if (skipFiles)
                {
                    fileCount += c;
                }

                subDirs = Directory.GetDirectories(currentDir);
            }
            // Thrown if we do not have discovery permission on the directory.
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            // Thrown if another process has deleted the directory after we retrieved its name.
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            
            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            foreach (string str in subDirs)
            {
                dirs.Push(str);
                dirCount++;                
            }

            if (!skipFiles)
            {
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                // Execute in parallel if there are enough files in the directory.
                // Otherwise, execute sequentially.Files are opened and processed
                // synchronously but this could be modified to perform async I/O.
                try
                {
                    if (files.Length < procCount)
                    {
                        foreach (var file in files)
                        {
                            fileAction(file);
                            fileCount++;
                        }
                    }
                    else
                    {
                        int localFileCount = 0;
                        Parallel.ForEach(files, () => 0,
                            (file, loopState, localCount) =>
                            {
                                fileAction(file);
                                return (int)++localCount;
                            },
                            (c) => { Interlocked.Add(ref localFileCount, c); });
                        fileCount += localFileCount;
                    }
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) =>
                    {
                        if (ex is UnauthorizedAccessException)
                        {
                            // Here we just output a message and go on.
                            Console.WriteLine(ex.Message);
                            return true;
                        }
                        // Handle other exceptions here if necessary...

                        return false;
                    });
                }
            }
        }

        // For diagnostic purposes.
        Console.WriteLine("Processed {0} files in {1} milliseconds", fileCount, sw.ElapsedMilliseconds);
    }
}