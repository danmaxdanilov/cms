using CMS.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace CMS.UnitTests
{
    public class CMSWebApiTest
    {
        [Fact]
        public void GetTestNumber_Success()
        {
            //Arrange
            var expectedResult = 777;

            //Act
            var CMSController = new CMSController();
            var actionResult = CMSController.GetTestNumber();

            //Assert
            Assert.Equal((actionResult as OkObjectResult).StatusCode, (int)System.Net.HttpStatusCode.OK);
            Assert.Equal(expectedResult, ((ObjectResult)actionResult).Value);
        }
    }
}
