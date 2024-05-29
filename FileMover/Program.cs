using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileMover
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                int command_index = Int32.Parse(args[0]);
                switch (command_index)
                {
                    case 1:
                        {
                            FolderToFile(args[1], ".pdf");
                        }
                        break;
                    case 2:
                        {
                            FileToFolder(args[1], ".pdf");
                        }
                        break;
                    case 3:
                        {
                            FolderToFile(args[1], ".jpg");
                        }
                        break;
                    case 4:
                        {
                            FileToFolder(args[1], ".jpg");
                        }
                        break;
                    case 5:
                        {
                            FolderToFile(args[1], ".txt");
                        }
                        break;
                    case 6:
                        {
                            FileToFolder(args[1], ".txt");
                        }
                        break;
                    case 7:
                        {
                            FileNameRemover(args[1]);
                        }
                        break;
                    case 8:
                        {
                            FolderDown(args[1]);
                        }
                        break;
                    case 9:
                        {
                            FolderUp(args[1]);
                        }
                        break;
                }
            }
        }

        static void FolderDown(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FolderDown(path, dir.Name, dir.FullName);
            }
        }
        static void FolderDown(string target_path, string prev_str, string current_path)
        {
            DirectoryInfo d = new DirectoryInfo(current_path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FolderDown(target_path, prev_str + "@" + dir.Name, dir.FullName);
            }
            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                string new_path = target_path + "\\" + prev_str;
                DirectoryInfo di = new DirectoryInfo(new_path);
                if (di.Exists == false)
                {
                    di.Create();
                }

                foreach (FileInfo file in files)
                {
                    string destinationFile = target_path + "\\" + prev_str + "\\" + file.Name;
                    System.IO.File.Move(file.FullName, destinationFile);
                }
            }
        }
        static void FolderUp(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                string[] folders = dir.Name.Split('@');
                if (folders.Length > 1)
                {
                    string new_path = path;
                    for (int i = 0; i < folders.Length; i++)
                    {
                        new_path += ("\\" + folders[i]);
                        DirectoryInfo di = new DirectoryInfo(new_path);
                        if (di.Exists == false)
                        {
                            di.Create();
                        }
                    }

                    FileInfo[] files = dir.GetFiles();
                    if (files.Length > 0)
                    {
                        foreach (FileInfo file in files)
                        {
                            System.IO.File.Move(file.FullName, new_path + "\\" + file.Name);
                        }
                    }
                }
                FolderUp(dir.FullName);
            }

        }
        static void FolderToFile(string path, string ext)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FolderToFile(path, dir.Name, dir.FullName, ext);
            }
        }

        static void FolderToFile(string target_path, string prev_str, string current_path, string ext)
        {
            DirectoryInfo d = new DirectoryInfo(current_path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FolderToFile(target_path, prev_str + "@" + dir.Name, dir.FullName, ext);
            }
            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (file.Extension.ToLower() == ext)
                    {
                        string destinationFile = target_path + "\\" + prev_str + "@" + file.Name;
                        System.IO.File.Move(file.FullName, destinationFile);
                    }
                }
            }
        }

        static void FileToFolder(string path, string ext)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FileToFolder(dir.FullName, ext);
            }

            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (file.Extension.ToLower() == ext)
                    {
                        string new_path = path;
                        string[] folders = file.Name.Split('@');
                        if (folders.Length > 1)
                        {
                            for (int i = 0; i < folders.Length - 1; i++)
                            {
                                new_path += ("\\" + folders[i]);
                                DirectoryInfo di = new DirectoryInfo(new_path);
                                if (di.Exists == false)
                                {
                                    di.Create();
                                }
                            }
                            new_path += ("\\" + folders[folders.Length - 1]);
                            System.IO.File.Move(file.FullName, new_path);
                        }
                    }
                }
            }
        }

        static void FileNameRemover(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FileNameRemover(dir.FullName);
            }

            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    int index = file.Name.LastIndexOf('@');
                    if(index > -1)
                    {
                        string new_file_name = file.Name.Remove(0, index + 1);
                        try
                        {
                            System.IO.File.Move(file.FullName, path + "\\" + new_file_name);
                        }
                        catch (System.IO.IOException e)
                        {
                            Console.Write(file.FullName + " : ");
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }

    }
}
