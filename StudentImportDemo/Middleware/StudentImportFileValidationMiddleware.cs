namespace StudentImportDemo.Middleware
{
    public class StudentImportFileValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public StudentImportFileValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsStudentImportEndpoint(context))
            {
                await _next(context);
                return;
            }

            if (!context.Request.HasFormContentType)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Request must be multipart/form-data"
                });
                return;
            }

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "File is required"
                });
                return;
            }

            if (file.Length == 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "File cannot be empty"
                });
                return;
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Only .xlsx files are allowed"
                });
                return;
            }

            await _next(context);
        }

        private static bool IsStudentImportEndpoint(HttpContext context)
        {
            return context.Request.Path.Equals("/api/students/import", StringComparison.OrdinalIgnoreCase)
                   && HttpMethods.IsPost(context.Request.Method);
        }
    }
}
