using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Rxintel.AI.ControllerHelpers
{
    /// <summary>
    /// Shared multipart CSV preview flow for field-mapping controllers. Keeps HTTP behavior
    /// (400 with error string, 200 with preview DTO) identical to inline controller code.
    /// </summary>
    public static class CsvPreviewControllerAction
    {
        public static async Task<ActionResult<TPreviewDto>> ExecuteAsync<TPreviewDto>(
            IFormFile? file,
            Func<Stream?, long, Task<(TPreviewDto? Preview, string? Error)>> getPreviewAsync)
            where TPreviewDto : class
        {
            Stream? stream = null;
            long length = 0;
            if (file is { Length: > 0 } f)
            {
                stream = f.OpenReadStream();
                length = f.Length;
            }

            using (stream)
            {
                var (preview, error) = await getPreviewAsync(stream, length);
                if (error != null)
                    return new BadRequestObjectResult(error);
                return new OkObjectResult(preview!);
            }
        }
    }
}
