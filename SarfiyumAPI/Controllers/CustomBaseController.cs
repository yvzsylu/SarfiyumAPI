using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomBaseController : ControllerBase
    {
        // Bu metot, servisten gelen ServiceResult nesnesini alır
        // ve içindeki StatusCode'a göre uygun HTTP cevabını üretir.
        [NonAction] // Swagger endpoint sanmasın diye
        public IActionResult CreateActionResult<T>(ServiceResult<T> result)
        {
            if (result.StatusCode == 204) // NoContent (Başarılı ama veri yok)
            {
                return new ObjectResult(null)
                {
                    StatusCode = result.StatusCode
                };
            }

            return new ObjectResult(result)
            {
                StatusCode = result.StatusCode
            };
        }
    }



}
