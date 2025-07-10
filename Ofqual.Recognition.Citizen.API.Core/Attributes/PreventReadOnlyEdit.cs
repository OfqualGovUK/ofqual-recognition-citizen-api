using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ofqual.Recognition.Citizen.API.Core.Attributes;

/* 
 * This attribute is used to indicate to middleware that it should forbid changes to applications that have been submitted
 * 
 * Use this attribute whenever a client makes a request related to a specific application. This should only be used in Controller endpoints
 * 
 * You must specify as a manual attribute the queryParameter that corresponds to the ApplicationId
 */
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PreventReadOnlyEdit : Attribute
{
    public string QueryParam { get; set; }
    public PreventReadOnlyEdit(string queryParam)
    {
        QueryParam = queryParam;
    }
}
