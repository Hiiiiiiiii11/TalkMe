using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Services
{
    public interface IMediaUploadService
    {
        Task<string> UploadPostImageAsync(IFormFile file);

        // Upload video
        Task<string> UploadPostVideoAsync(IFormFile file);
    }
}
