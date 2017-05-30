using System;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Threading;

namespace CSGO_DemoPars
{
    class Program
    {
        static void Main(string[] args)
        {
            // First, check wether the user needs assistance:
            if (args.Length == 0 || args[0] == "--help")
            {
                PrintHelp();
                return;
            }

            FileStream ostrm;
            StreamWriter writer;
            try
            {
                ostrm = new FileStream("./Test.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open Redirect.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            //Console.SetOut(writer);

            //placeholder variables for database inputs that are not in the demo
            DateTime matchTime = DateTime.Now;
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo root = new System.IO.DirectoryInfo(args[0]);

            try
            {
                files = root.GetFiles("*.dem");
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            int reservedLine = -1;
            Database database = new Database();
            // Parallel approach as reading different files are independent tasks
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 1 }, file =>
            {
                Parser parser = new Parser();
                parser.setDatabase(database);
                parser.parserFile(file, Interlocked.Increment(ref reservedLine));
            });
        }

        static void PrintHelp()
        {
            string fileName = Path.GetFileName((Assembly.GetExecutingAssembly().Location));
            Console.WriteLine("CS:GO Demo-Statistics-Generator");
            Console.WriteLine("http://github.com/moritzuehling/demostatistics-creator");
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Usage: {0} [--help] [--scoreboard] file1.dem [file2.dem ...]", fileName);
            Console.WriteLine("--help");
            Console.WriteLine("    Displays this help");
            Console.WriteLine("--frags");
            Console.WriteLine("    Displays only the frags happening in this demo in the format. Cannot be used with anything else. Only works with 1 file. ");
            Console.WriteLine("    Output-format (example): <Hyper><76561198014874496><CT> + <centralize><76561198085059888><CT> [M4A1][HS][Wall] <percy><76561197996475850><T>");
            Console.WriteLine("--scoreboard");
            Console.WriteLine("    Displays only the scoreboards on every round_end event. Cannot be used with anything else. Only works with 1 file. ");

            Console.WriteLine("file1.dem");
            Console.WriteLine("    Path to a demo to be parsed. The resulting file with have the same name, ");
            Console.WriteLine("    except that it'll end with \".dem.[map].csv\", where [map] is the map.");
            Console.WriteLine("    The resulting file will be a CSV-File containing some statistics generated");
            Console.WriteLine("    by this program, and can be viewed with (for example) LibreOffice");
            Console.WriteLine("[file2.dem ...]");
            Console.WriteLine("    You can specify more than one file at a time.");
        }
    }
}
