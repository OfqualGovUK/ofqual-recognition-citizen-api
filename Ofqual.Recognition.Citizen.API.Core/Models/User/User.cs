using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class User : IDataMetadata
{
    public Guid UserId { get; set; }
    public Guid B2CId { get; set; }
    public string? EmailAddress { get; set; }
    public string? DisplayName { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

}

