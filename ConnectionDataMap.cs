using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransferFTP;
using CsvHelper.Configuration;

public class ConnectionDataMap : ClassMap<ConnectionData>
{
    public ConnectionDataMap()
    {
        Map(p => p.Description).Index(0);
        Map(p => p.Type).Index(1); // "SFTP" - "FTP"
        Map(p => p.Mode).Index(2); // "U"pload - "D"ownload
        Map(p => p.Url).Index(3);
        Map(p => p.Port).Index(4);
        Map(p => p.User).Index(5);
        Map(p => p.Password).Index(6);
        Map(p => p.RemoteDirectory).Index(7);
        Map(p => p.LocalDirectory).Index(8);
        Map(p => p.DeleteFile).Index(9);
        Map(p => p.MoveBackupFolder).Index(10);
    }
}
