using Download.NetCore.Service;
using System.Collections.Generic;

namespace Wemew.Program.ZipInfo
{

    public class ZipDescEntity
    {
        public string Source { get; set; }
        public string ImagePath { get; set; }
        public string Descript { get; set; }
        public string TypeName { get; set; }
        public  FileInformat jsonFromat { get; set; }
    }

    public class ZipEntity: ZipDescEntity
    {
        public bool FindVideo { get; set; }
    }
}
