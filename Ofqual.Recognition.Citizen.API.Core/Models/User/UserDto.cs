using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UserDto
{
    public Guid UserId { get; set; }
    public Guid B2CId { get; set; }
    public string EmailAddress { get; set; }
    public string DisplayName { get; set; }

}
