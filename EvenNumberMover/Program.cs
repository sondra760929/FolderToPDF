using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenNumberMover
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.Write(">> Even Number Mover Utility from DIGIBOOK 2021/08/26<<\n\n");

                int command_index = Int32.Parse(args[0]);
                switch (command_index)
                {
                    case 1:
                        {
                            MoveFiles(args[1]);
                        }
                        break;
                    case 2:
                        {
                            MoveFilesToParent(args[1]);
                        }
                        break;
                    case 3:
                        {
                            EraseBlankFolder(args[1]);
                        }
                        break;
                }
            }
        }

        static void MoveFiles(string path)
        {
            //  하위 폴더 생성
            DirectoryInfo d = new DirectoryInfo(path);
            if (d.Name == "000.even_number")
                return;

            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                string current_folder;
                current_folder = path + "\\000.even_number";
                DirectoryInfo di = new DirectoryInfo(current_folder);
                if (di.Exists == false)
                {
                    di.Create();
                }

                foreach (FileInfo file in files)
                {
                    string file_name = file.Name;
                    int index1 = file_name.LastIndexOf('@');
                    file_name = file_name.Substring(index1 + 1);
                    index1 = file_name.IndexOf('.');
                    file_name = file_name.Remove(index1);

                    int file_no;
                    if (int.TryParse(file_name, out file_no))
                    {
                        if (file_no % 2 == 0) //  짝수
                        {
                            System.IO.File.Move(file.FullName, current_folder + "\\" + file.Name);
                        }
                    }
                }
            }

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                MoveFiles(dir.FullName);
            }
        }
        static void MoveFilesToParent(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);
            if (d.Name.ToLower() == "000.even_number")
            {
                DirectoryInfo parent = d.Parent;

                FileInfo[] files = d.GetFiles();
                if (files.Length > 0)
                {
                    foreach (FileInfo file in files)
                    {
                        System.IO.File.Move(file.FullName, parent.FullName + "\\" + file.Name);
                    }
                }
            }

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                MoveFilesToParent(dir.FullName);
            }
        }
        static bool EraseBlankFolder(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);
            FileInfo[] files = d.GetFiles();
            DirectoryInfo[] dirs = d.GetDirectories();

            if(files.Length == 0 && dirs.Length == 0)
            {
                d.Delete();
                return true;
            }
            else
            {
                bool erase_child = true;
                while (erase_child && dirs.Length > 0)
                {
                    erase_child = false;
                    foreach (DirectoryInfo dir in dirs)
                    {
                        if(EraseBlankFolder(dir.FullName))
                        {
                            erase_child = true;
                        }
                    }
                    dirs = d.GetDirectories();
                }

                if (files.Length == 0 && dirs.Length == 0)
                {
                    d.Delete();
                    return true;
                }
            }
            return false;
        }
    }
}
