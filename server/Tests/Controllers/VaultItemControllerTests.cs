using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using server.Controllers;
using server.Dtos.VaultItem;
using server.Interfaces;
using System.Security.Claims;

namespace server.Tests.Controllers;

public class VaultItemControllerTests
{
    private readonly Mock<IVaultItemService> _itemServiceMock;
    private readonly VaultItemController _controller;
    private readonly string _testUserId = Guid.NewGuid().ToString();

    public VaultItemControllerTests()
    {
        _itemServiceMock = new Mock<IVaultItemService>();
        _controller = new VaultItemController(_itemServiceMock.Object);
        
        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim("sub", _testUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetVaultItems_ReturnsItems()
    {
        // Arrange
        var items = new List<VaultItemResponseDTO>
        {
            new VaultItemResponseDTO { Id = 1, Title = "Item 1", VaultId = 1 }
        };
        _itemServiceMock.Setup(x => x.GetVaultItemsAsync(1, _testUserId))
            .ReturnsAsync(items);

        // Act
        var result = await _controller.GetVaultItems(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsType<List<VaultItemResponseDTO>>(okResult.Value);
        Assert.Single(returnedItems);
    }

    [Fact]
    public async Task GetItem_WithValidId_ReturnsItem()
    {
        // Arrange
        var item = new VaultItemResponseDTO { Id = 1, Title = "Test Item", VaultId = 1 };
        _itemServiceMock.Setup(x => x.GetItemByIdAsync(1, _testUserId))
            .ReturnsAsync(item);

        // Act
        var result = await _controller.GetItem(1, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItem = Assert.IsType<VaultItemResponseDTO>(okResult.Value);
        Assert.Equal(1, returnedItem.Id);
    }

    [Fact]
    public async Task GetItem_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.GetItemByIdAsync(999, _testUserId))
            .ReturnsAsync((VaultItemResponseDTO?)null);

        // Act
        var result = await _controller.GetItem(1, 999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Item not found or access denied", notFound.Value);
    }

    [Fact]
    public async Task CreateItem_WithValidData_CreatesItem()
    {
        // Arrange
        var dto = new CreateVaultItemDTO
        {
            VaultId = 1,
            ItemType = server.Models.ItemType.Note,
            Title = "New Item"
        };
        var item = new VaultItemResponseDTO { Id = 1, Title = dto.Title, VaultId = 1 };
        _itemServiceMock.Setup(x => x.CreateItemAsync(dto, _testUserId))
            .ReturnsAsync(item);

        _controller.ControllerContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        // Act
        var result = await _controller.CreateItem(1, dto);

        // Assert
        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedItem = Assert.IsType<VaultItemResponseDTO>(createdAt.Value);
        Assert.Equal(1, returnedItem.Id);
    }

    [Fact]
    public async Task UpdateItem_WithValidData_UpdatesItem()
    {
        // Arrange
        var dto = new UpdateVaultItemDTO { Title = "Updated Title" };
        var item = new VaultItemResponseDTO { Id = 1, Title = dto.Title, VaultId = 1 };
        _itemServiceMock.Setup(x => x.UpdateItemAsync(1, dto, _testUserId))
            .ReturnsAsync(item);

        _controller.ControllerContext.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        // Act
        var result = await _controller.UpdateItem(1, 1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItem = Assert.IsType<VaultItemResponseDTO>(okResult.Value);
        Assert.Equal("Updated Title", returnedItem.Title);
    }

    [Fact]
    public async Task DeleteItem_WithValidId_DeletesItem()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.DeleteItemAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteItem(1, 1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RestoreItem_WithValidId_RestoresItem()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.RestoreItemAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RestoreItem(1, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }
}

