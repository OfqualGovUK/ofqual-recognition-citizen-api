using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IDataMetadata
{
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}

