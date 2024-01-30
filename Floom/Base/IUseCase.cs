using Microsoft.AspNetCore.Mvc;

namespace Floom.Base;

public interface IUseCase<in T>
{
    Task<IActionResult> ValidateAsync(T model);
    Task<IActionResult> ExecuteAsync(T model);
}