namespace TriviaBackend.Exceptions
{
    public class ExceptionHandler(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception)
            {

            }
        }
    }
}
