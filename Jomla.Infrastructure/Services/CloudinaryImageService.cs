using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Jomla.Application.Common.Interfaces;
using Jomla.Application.Common.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

//MOW -- upload image service :: CloudinaryImageService (sound effects with drums) 

namespace Jomla.Infrastructure.Services;

public sealed class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(IOptions<CloudinarySettings> settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Value.CloudName))
            throw new Exception("CloudName missing");

        if (string.IsNullOrWhiteSpace(settings.Value.ApiKey))
            throw new Exception("ApiKey missing");

        if (string.IsNullOrWhiteSpace(settings.Value.ApiSecret))
            throw new Exception("ApiSecret missing");

        var account = new Account(
            settings.Value.CloudName,
            settings.Value.ApiKey,
            settings.Value.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync( IFormFile file,CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
            throw new Exception("Image is empty.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(
                file.FileName,
                stream),
            Format="jpg"
        };

        var result = await _cloudinary
            .UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
        {
            throw new Exception(
                $"Cloudinary Error: {result.Error.Message}");
        }

        if (result.SecureUrl == null)
        {
            throw new Exception(
                $"Upload failed. Status: {result.StatusCode}");
        }

        return result.SecureUrl.ToString();
    }

}

