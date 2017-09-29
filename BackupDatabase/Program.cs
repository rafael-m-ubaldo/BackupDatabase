using System;
using System.IO;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BackupDatabase
{
    class Program
    {
        static string Server = string.Empty;
        static string Database = string.Empty;
        static string Username = string.Empty;
        static string Password = string.Empty;
        static string BackupFile = string.Empty;
        static string OutputFile = string.Empty;
        static string DestinationFile = string.Empty;
        static DateTime backupTime;

        static void Main(string[] args)
        {
            if (!ReadOptions(args))
            {
                return;
            }

            backupTime = DateTime.Now;

            DoBackup();
            SetFileDate(OutputFile, backupTime);
            if (DestinationFile != string.Empty)
            {
                DestinationFile = StampFile(DestinationFile);
                CopyBackup();
            }
        }

        static bool ReadOptions(string[] args)
        {
            bool result = true;
            try
            {
                int idx = 0;
                while (idx < args.Length)
                {
                    string arg = args[idx++];
                    if (arg.Equals("-s", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Server = args[idx++];
                    }
                    else if (arg.Equals("-d", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Database = args[idx++];
                    }
                    else if (arg.Equals("-u", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Username = args[idx++];
                    }
                    else if (arg.Equals("-p", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Password = args[idx++];
                    }
                    else if (arg.Equals("-b", StringComparison.CurrentCultureIgnoreCase))
                    {
                        BackupFile = args[idx++];
                    }
                    else if (arg.Equals("-o", StringComparison.CurrentCultureIgnoreCase))
                    {
                        OutputFile = args[idx++];
                    }
                    else if (arg.Equals("-f", StringComparison.CurrentCultureIgnoreCase))
                    {
                        DestinationFile = args[idx++];
                    }
                    else
                    {
                        result = false;
                        throw new ArgumentException(string.Format("Unknown option: {0}", arg));
                    }
                }
                if (Server == string.Empty)
                {
                    Console.WriteLine("No Server.");
                    result = false;
                }
                if (Database == string.Empty)
                {
                    Console.WriteLine("No Database.");
                    result = false;
                }
                if (Username == string.Empty)
                {
                    Console.WriteLine("No Username.");
                    result = false;
                }
                if (Password == string.Empty)
                {
                    Console.WriteLine("No Password.");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error reading command-line options: {0}", ex.Message));
                result = false;
            }
            return result;
        }

        static string ConnectionString
        {
            get
            {
                SqlConnectionStringBuilder connstr = new SqlConnectionStringBuilder();
                // Build connection string
                connstr["Data Source"] = Server;
                connstr["Persist Security Info"] = "True";
                connstr["Initial Catalog"] = Database;
                connstr["User ID"] = Username;
                connstr["Password"] = Password;
                connstr["Connect Timeout"] = 120;   // 2 minute connection timeout
                return connstr.ConnectionString;
            }
        }
		
        static void DoBackup()
        {
            StringBuilder sql = new StringBuilder();
            sql.Append("BACKUP DATABASE ").Append(Database)
                .Append(" TO DISK = ").Append(Quoted(BackupFile))
                .Append(" WITH INIT,SKIP");
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(sql.ToString(), conn);
                cmd.CommandTimeout = 900;   // 15 minute command timeout 60*15 = 900
                try
                {
                    conn.Open();
                    IAsyncResult result = cmd.BeginExecuteNonQuery();
                    Console.WriteLine("Start backup");
                    int count = 0;
                    while(!result.IsCompleted)
                    {
                        System.Threading.Thread.Sleep(250);
                        if ((++count % 60) == 0)
                        {
                            if ((count % 240) == 0)
                            {
                                Console.Write(string.Format("{0}", (count/240)));
                            }
                            else
                            {
                                Console.Write(".");
                            }
                        }
                    }
                    Console.WriteLine("");
                    Console.WriteLine("Done");
                    cmd.EndExecuteNonQuery(result);
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        static string StampFile(string  Filename)
        {
            string dir = Path.GetDirectoryName(Filename);
            string filename = Path.GetFileNameWithoutExtension(Filename);
            string ext = Path.GetExtension(Filename);
            filename = string.Format("{0}_{1}{2}", filename, DateString(backupTime), ext);
            return Path.Combine(dir, filename);
        }

        static string DateString(DateTime dt, bool IncludeTime = false)
        {
            if (IncludeTime)
            {
                return string.Format("{0:D4}{1:D2}{2:D2}_{3:D2}{4:D2}{5:D2}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
            }
            else
            {
                return string.Format("{0:D4}{1:D2}{2:D2}", dt.Year, dt.Month, dt.Day);
            }
        }

        static string Quoted(string text)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("'").Append(text.Replace("'", "''")).Append("'");
            return sb.ToString();
        }

        static void SetFileDate(string filename, DateTime filedate)
        {
            if (File.Exists(filename))
            {
                File.SetCreationTime(filename, filedate);
                File.SetLastWriteTime(filename, filedate);
                File.SetLastAccessTime(filename, filedate);
            }
        }

        static void CopyBackup()
        {
            try
            {
                if (File.Exists(DestinationFile))
                {
                    File.Delete(DestinationFile);
                }
                Console.WriteLine("Copying file.");
                File.Copy(OutputFile, DestinationFile);
                SetFileDate(OutputFile, backupTime);
                SetFileDate(DestinationFile, backupTime);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }
    }
}
