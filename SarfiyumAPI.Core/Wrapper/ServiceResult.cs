using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Wrapper
{
    public class ServiceResult<T>
    {
        public T Data { get; set; } // Dönen asıl veri (Token, Ürün Listesi vs.)

        public List<string> Errors { get; set; } // Hatalar listesi

        [JsonIgnore] // Bu alanı JSON'a dahil etmeyip sadece kod içinde Status Code yönetmek için kullanabiliriz, ama Angular'da da görmek istersen kaldırma.
        public int StatusCode { get; set; }

        public bool IsSuccess { get; set; } // UI'da kolay kontrol: if(res.isSuccess)

        // Başarılı Cevaplar İçin Factory Methodlar (Clean Code)
        public static ServiceResult<T> Success(T data, int statusCode = 200)
        {
            return new ServiceResult<T> { Data = data, StatusCode = statusCode, IsSuccess = true, Errors = null };
        }

        // Verisiz Başarılı Cevap (Örn: Update işlemi)
        public static ServiceResult<T> Success(int statusCode = 200)
        {
            return new ServiceResult<T> { Data = default, StatusCode = statusCode, IsSuccess = true, Errors = null };
        }

        // Hatalı Cevaplar İçin
        public static ServiceResult<T> Fail(List<string> errors, int statusCode = 400)
        {
            return new ServiceResult<T> { Data = default, Errors = errors, StatusCode = statusCode, IsSuccess = false };
        }

        public static ServiceResult<T> Fail(string error, int statusCode = 400)
        {
            return new ServiceResult<T> { Data = default, Errors = new List<string> { error }, StatusCode = statusCode, IsSuccess = false };
        }
    }
}
