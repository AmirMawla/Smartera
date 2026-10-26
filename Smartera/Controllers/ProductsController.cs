using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.Common;
using Smartera.BLL.Dtos.ProductDto;
using Smartera.BLL.Interfaces;

namespace Smartera.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var result = await _productRepository.GetAllProductsAsync(request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _productRepository.GetProductByIdAsync(id, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("category/{categoryId:int}")]
        public async Task<IActionResult> GetByCategory(int categoryId, [FromQuery] RequestFilter request, CancellationToken cancellationToken)
        {
            var result = await _productRepository.GetProductsByCategoryAsync(categoryId, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("featured")]
        public async Task<IActionResult> GetFeatured([FromQuery] int count = 8, CancellationToken cancellationToken = default)
        {
            var result = await _productRepository.GetFeaturedProductsAsync(count, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("new-arrivals")]
        public async Task<IActionResult> GetNewArrivals([FromQuery] int count = 8, CancellationToken cancellationToken = default)
        {
            var result = await _productRepository.GetNewArrivalsAsync(count, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productRepository.CreateProductAsync(request, cancellationToken);
            return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productRepository.UpdateProductAsync(id, request, cancellationToken);
            return result.IsSuccess ? Ok(new { message = "Product successfully updated." }) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _productRepository.DeleteProductAsync(id, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> images, CancellationToken cancellationToken)
        {
            var result = await _productRepository.AddProductImagesAsync(id, images, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken cancellationToken)
        {
            var result = await _productRepository.RemoveProductImageAsync(id, imageId, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/images/{imageId:int}/primary")]
        public async Task<IActionResult> SetPrimaryImage(int id, int imageId, CancellationToken cancellationToken)
        {
            var result = await _productRepository.SetPrimaryImageAsync(id, imageId, cancellationToken);
            return result.IsSuccess ? Ok(new { message = "Primary image set successfully." }) : result.ToProblem();
        }
    }
}
