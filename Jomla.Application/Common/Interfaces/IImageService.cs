using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Jomla.Application.Common.Interfaces;

public interface IImageService
{
    Task<string> UploadImageAsync(IFormFile file,CancellationToken cancellationToken = default);
}

