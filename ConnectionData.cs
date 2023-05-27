using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

namespace TransferFTP
{
    public class ConnectionData
    {
        [Index(0)]
        public string Description { get; set; }
        [Index(1)]
        public string Type { get; set; } // "SFTP" - "FTP"
        [Index(2)]
        public string Mode { get; set; } // "U"pload - "D"ownload
        [Index(3)]
        public string Url { get; set; }
        [Index(4)]
        public int Port { get; set; }
        [Index(5)]
        public string User { get; set; }
        [Index(6)]
        public string Password { get; set; }
        [Index(7)]
        public string RemoteDirectory { get; set; }
        [Index(8)]
        public string LocalDirectory { get; set; }
        [Index(9)]
        public bool DeleteFile { get; set; }
        [Index(10)]
        public bool MoveBackupFolder { get; set; }
    }
}
