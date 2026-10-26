using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smartera.BLL.Abstractions;
using Smartera.BLL.Dtos.CategoryDto;
using Smartera.BLL.Interfaces;

namespace Smartera.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _categoryRepository.GetAllCategoriesAsync(cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _categoryRepository.GetCategoryByIdAsync(id, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryRepository.CreateCategoryAsync(request, cancellationToken);
            return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCategoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _categoryRepository.UpdateCategoryAsync(id, request, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _categoryRepository.DeleteCategoryAsync(id, cancellationToken);
            return result.IsSuccess ? NoContent() : result.ToProblem();
        }
    }
}
