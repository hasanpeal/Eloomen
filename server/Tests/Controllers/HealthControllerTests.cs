using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using server.Controllers;
using server.Interfaces;
using server.Models;
using server.Tests.Helpers;

namespace server.Tests.Controllers;

public class HealthControllerTests : IDisposable
{
    private readonly ApplicationDBContext _dbContext;
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _s3ServiceMock = new Mock<IS3Service>();
        _controller = new HealthController(_dbContext, _s3ServiceMock.Object);
    }

    [Fact]
    public async Task Get_ReturnsHealthStatus()
    {
        // Arrange
        _s3ServiceMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var healthData = Assert.IsType<Dictionary<string, object>>(okResult.Value);
        Assert.Contains("database", healthData.Keys);
        Assert.Contains("s3", healthData.Keys);
    }

    [Fact]
    public async Task Get_WithDatabaseConnection_ReportsConnected()
    {
        // Arrange
        _s3ServiceMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var healthData = Assert.IsType<Dictionary<string, object>>(okResult.Value);
        var dbStatus = healthData["database"];
        Assert.NotNull(dbStatus);
    }

    [Fact]
    public async Task Get_WithS3Service_ReportsS3Status()
    {
        // Arrange
        _s3ServiceMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var healthData = Assert.IsType<Dictionary<string, object>>(okResult.Value);
        var s3Status = healthData["s3"];
        Assert.NotNull(s3Status);
    }

    [Fact]
    public async Task Get_WithoutS3Service_ReportsS3NotConfigured()
    {
        // Arrange
        var controllerWithoutS3 = new HealthController(_dbContext, null);

        // Act
        var result = await controllerWithoutS3.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var healthData = Assert.IsType<Dictionary<string, object>>(okResult.Value);
        Assert.Contains("s3", healthData.Keys);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

