using Microsoft.AspNetCore.Http;
using Moq;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class UserInformationServiceTests
{
    private readonly Mock<HttpContextAccessor> _mockHttpContextAccessor;

    public UserInformationServiceTests()
    {
        _mockHttpContextAccessor = new Mock<HttpContextAccessor>();
    }
}

