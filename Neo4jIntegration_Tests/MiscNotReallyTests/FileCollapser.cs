using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace plm_testing.MiscNotReallyTests
{
    [TestClass]
    class FileCollapser
    {

        static bool jobDone, maxLocked;
        [TestMethod]
        public void CollapseSQL()
        {
            DirectoryInfo root = new DirectoryInfo(@"Y:\Dump\SQL");

            using (StreamWriter output = new StreamWriter((new FileInfo(@"Y:\Dump\collapsed.sql")).Open(FileMode.Create)))
            {
                CollapseSQL(root, output);
                output.Flush();
            }
        }
        public void CollapseSQL(DirectoryInfo folder, StreamWriter output)
        {
            foreach (var di in folder.EnumerateDirectories())
            {
                CollapseSQL(di, output);
            }
            foreach (var fi in folder.EnumerateFiles())
            {
                CollapseSQL(fi, output);
            }
        }
        public void CollapseSQL(FileInfo file, StreamWriter output)
        {
            using (StreamReader src = new StreamReader(file.OpenRead()))
            {
                while (!src.EndOfStream)
                {
                    string line = src.ReadLine();
                    output.WriteLine(line);
                }
            }
        }
    }
}
