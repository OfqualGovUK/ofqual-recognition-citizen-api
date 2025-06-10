using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class AttachmentLink
{
    public Guid AttachmentLinkId { get; set; }
    public Guid AttachmentId { get; set; }
    public Guid LinkId { get; set; }
    public LinkType LinkTypeId { get; set; }
    public Guid ApplicationId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn { get; set; }
    public required string ModifiedByUpn { get; set; }
}