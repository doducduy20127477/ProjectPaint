using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsLibrary
{
    public class DllLoader
    {
        public static List<Type> Types { get; set; }
        public static void execute()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(exePath);
            FileInfo[] fis = new DirectoryInfo(folder).GetFiles("*.dll");
            Types = new List<Type>();
            foreach (FileInfo fileInfo in fis)
            {
                var domain = AppDomain.CurrentDomain;
                //AssemblyName assName = AssemblyName.GetAssemblyName(fileInfo.FullName);
                Assembly assembly = Assembly.LoadFrom(fileInfo.FullName);
                Type[] types = assembly.GetTypes();

                Types.AddRange(types);
            }
        }
    }
}
